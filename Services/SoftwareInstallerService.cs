using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using produKtiviti.Models;

namespace produKtiviti.Services
{
    public class SoftwareInstallerService
    {
        private static readonly TimeSpan AvailabilityCheckTimeout = TimeSpan.FromSeconds(8);
        private static readonly TimeSpan InstallTimeout = TimeSpan.FromMinutes(20);

        // Fresh machines sometimes only have the per-user app execution alias on disk
        // (not yet resolved via PATH), so fall back to its well-known location.
        private static string WingetFallbackPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft", "WindowsApps", "winget.exe");

        public static async Task<bool> IsWingetAvailableAsync()
        {
            var process = StartWinget("--version");
            if (process == null) return false;

            try
            {
                using var cts = new CancellationTokenSource(AvailabilityCheckTimeout);
                try
                {
                    await process.WaitForExitAsync(cts.Token);
                    return process.ExitCode == 0;
                }
                catch (OperationCanceledException)
                {
                    // winget's app-execution-alias stub can hang or pop a Store prompt
                    // when App Installer isn't actually present yet.
                    KillTree(process);
                    return false;
                }
            }
            catch
            {
                return false;
            }
            finally
            {
                process.Dispose();
            }
        }

        public async Task<bool> InstallAsync(SoftwareItem item, Action<string>? log = null, CancellationToken cancellationToken = default)
        {
            item.Status = InstallStatus.Installing;
            log?.Invoke($"Installing {item.Name} ({item.WingetId})...");

            var args = $"install --id {item.WingetId} -e --silent --accept-package-agreements --accept-source-agreements";
            var process = StartWinget(args);
            if (process == null)
            {
                item.Status = InstallStatus.Failed;
                log?.Invoke($"{item.Name}: could not find winget.");
                return false;
            }

            try
            {
                var stdOutTask = process.StandardOutput.ReadToEndAsync();
                var stdErrTask = process.StandardError.ReadToEndAsync();

                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(InstallTimeout);

                try
                {
                    await process.WaitForExitAsync(timeoutCts.Token);
                }
                catch (OperationCanceledException)
                {
                    KillTree(process);
                    bool userCancelled = cancellationToken.IsCancellationRequested;
                    item.Status = InstallStatus.Failed;
                    log?.Invoke(userCancelled
                        ? $"{item.Name}: cancelled."
                        : $"{item.Name}: timed out after {InstallTimeout.TotalMinutes:0} minutes.");
                    return false;
                }

                var stdOut = await stdOutTask;
                var stdErr = await stdErrTask;

                bool alreadyHandled =
                    stdOut.Contains("already installed", StringComparison.OrdinalIgnoreCase) ||
                    stdOut.Contains("No available upgrade", StringComparison.OrdinalIgnoreCase);
                bool success = process.ExitCode == 0 || alreadyHandled;

                item.Status = success ? InstallStatus.Success : InstallStatus.Failed;

                if (!string.IsNullOrWhiteSpace(stdOut)) log?.Invoke(stdOut.Trim());
                if (!string.IsNullOrWhiteSpace(stdErr)) log?.Invoke(stdErr.Trim());
                log?.Invoke(success ? $"{item.Name}: done." : $"{item.Name}: failed (exit code {process.ExitCode}).");

                return success;
            }
            catch (Exception ex)
            {
                item.Status = InstallStatus.Failed;
                log?.Invoke($"{item.Name}: error - {ex.Message}");
                return false;
            }
            finally
            {
                process.Dispose();
            }
        }

        // Tries the "winget" PATH alias first, then the known app-execution-alias
        // location directly, since a just-provisioned account's PATH may lag behind.
        private static Process? StartWinget(string arguments)
        {
            foreach (var exe in new[] { "winget", WingetFallbackPath })
            {
                try
                {
                    var psi = new ProcessStartInfo(exe, arguments)
                    {
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };
                    var process = new Process { StartInfo = psi };
                    process.Start();
                    return process;
                }
                catch (Win32Exception)
                {
                    // Not found via this candidate — try the next one.
                }
                catch
                {
                    return null;
                }
            }
            return null;
        }

        private static void KillTree(Process process)
        {
            try { process.Kill(entireProcessTree: true); } catch { }
        }
    }
}
