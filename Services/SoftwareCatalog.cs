using System.Collections.Generic;
using produKtiviti.Models;

namespace produKtiviti.Services
{
    public static class SoftwareCatalog
    {
        public static List<SoftwareItem> GetAll() => new()
        {
            new SoftwareItem { Name = "Firefox", Description = "Web browser", WingetId = "Mozilla.Firefox" },
            new SoftwareItem { Name = "Chrome", Description = "Web browser", WingetId = "Google.Chrome" },
            new SoftwareItem { Name = "Epic Games Launcher", Description = "Game launcher", WingetId = "EpicGames.EpicGamesLauncher" },
            new SoftwareItem { Name = "Steam", Description = "Game launcher", WingetId = "Valve.Steam" },
            new SoftwareItem { Name = "EA App", Description = "Game launcher (EA)", WingetId = "ElectronicArts.EADesktop" },
            new SoftwareItem { Name = "Jagex Launcher", Description = "Game launcher (RuneScape / OSRS)", WingetId = "Jagex.Runescape" },
            new SoftwareItem { Name = "Discord", Description = "Chat and voice", WingetId = "Discord.Discord" },
            new SoftwareItem { Name = "VS Code", Description = "Code editor", WingetId = "Microsoft.VisualStudioCode" },
            new SoftwareItem { Name = "IntelliJ IDEA", Description = "Java IDE (Community)", WingetId = "JetBrains.IntelliJIDEA.Community" },
            new SoftwareItem { Name = "PyCharm", Description = "Python IDE (Community)", WingetId = "JetBrains.PyCharm.Community" },
            new SoftwareItem { Name = "Claude Code", Description = "Anthropic's CLI coding agent", WingetId = "Anthropic.ClaudeCode" },
            new SoftwareItem { Name = "Git for Windows", Description = "Version control", WingetId = "Git.Git" },
        };
    }
}
