using Hexa.NET.ImGui;
using Markdig.Syntax;
using Rysy.Gui;
using Rysy.Gui.Windows;
using Rysy.Helpers;
using Rysy.Scenes;
using Rysy.Signals;
using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace Rysy.Components;

internal sealed class UpdateChecker : SceneComponent {
    private readonly IRysyLogger _logger;
    private readonly string _repoUrl;
    private Task? _updateTask;

    public UpdateChecker(IRysyLogger logger, string repoUrl) {
        _logger = logger;
        _repoUrl = repoUrl;
    }

    public override void OnAdded() {
        base.OnAdded();
        
        _updateTask ??= Task.Run(CheckForUpdates);
    }

    private async Task CheckForUpdates() {
        _logger.Info($"Looking for Rysy updates from repo '{_repoUrl}'...");
        
        GitHubRelease? latestRelease;
        try {
            latestRelease = await GitHubApi.GetLatestReleaseAsync(_repoUrl);
        } catch (Exception ex) {
            _logger.Error(ex, "Failed to get latest Rysy release from GitHub.");
            return;
        }
        
        _logger.Info($"Fetched latest Rysy release from repo '{_repoUrl}'...");
        
        if (latestRelease is null) {
            _logger.Error("Failed to get latest Rysy release from GitHub, either there are no releases or the repo could not be found.");
            return;
        }

        var latestVersionStr = latestRelease.Version ?? "";
        if (!Version.TryParse(latestVersionStr, out var latestVersion)) {
            _logger.Error($"Failed to get latest Rysy release from GitHub, tag name {latestVersionStr} could not get parsed.");
            return;
        }

        if (RysyEngine.Version >= latestVersion) {
            _logger.Info("Rysy version is up-to-date.");
            return;
        }
        
        _logger.Info($"Rysy version is out-to-date, current: '{RysyEngine.Version}', latest is: '{latestVersion}'.");
        
        string? osPostfix = GetReleaseOsSpecificPrefix();
        if (osPostfix is null) {
            _logger.Info($"Cannot automatically download updates for this OS.");
            return;
        }

        var asset = latestRelease.Assets.FirstOrDefault(u =>
            u.Name.EndsWith(osPostfix, StringComparison.OrdinalIgnoreCase));
        
        if (asset is null) 
        {
            _logger.Error($"Failed to find release asset for this OS.");
            return;
        }
        
        Scene?.AddWindow(new UpdateNotificationWindow(latestRelease, asset));
    }
    
    private static string? GetReleaseOsSpecificPrefix()
    {
        var osPostfix =
            OperatingSystem.IsWindows() ? "-windows.zip" :
            OperatingSystem.IsLinux() ? "-linux.zip" :
            OperatingSystem.IsMacOS() ? RuntimeInformation.ProcessArchitecture is Architecture.Arm64
                ? "-osx-arm64.zip"
                : "-osx-x64.zip"
            : null;
        return osPostfix;
    }

    private class UpdateNotificationWindow : Window {
        private readonly GitHubRelease _latestRelease;
        private readonly GitHubAsset _asset;
        private readonly MarkdownObject? _description;

        public UpdateNotificationWindow(GitHubRelease latestRelease, GitHubAsset asset) : base("rysy.windows.updatechecker", new GuiSize(80, 16).CalculateWindowSize(true)) {
            _latestRelease = latestRelease;
            _asset = asset;
            if (_latestRelease.Description is {} description)
                _description = Markdig.Markdown.Parse(description, ImGuiMarkdown.MarkdownPipeline);
        }

        public override bool PersistBetweenScenes => true;

        public override bool HasBottomBar => true;

        public override void RenderBottomBar() {
            base.RenderBottomBar();

            if (ImGuiManager.TranslatedButton("rysy.windows.updatechecker.update")) {
                this.Emit(new RunAtEndOfThisFrame(() => PopupNotificationWindow.RunInBackground("rysy.windows.updatechecker.fail", StartInstallingUpdate)));
                RemoveSelf();
            }
            
            ImGui.SameLine();
            
            if (ImGuiManager.TranslatedButton("rysy.windows.updatechecker.remindMeLater")) {
                RemoveSelf();
            }
        }

        protected override void Render() {
            base.Render();
            
            ImGuiManager.TranslatedTextWrapped("rysy.windows.updatechecker.desc", _latestRelease.Version ?? "");
            if (_description is not null)
                ImGuiMarkdown.RenderMarkdown(_description);
        }

        private async Task StartInstallingUpdate() {
            var dir = AppContext.BaseDirectory;
            var updaterExecutableFilename = OperatingSystem.IsWindows() ? "Rysy.Updater.exe" : "Rysy.Updater";
            var updaterExecutableFile = Path.Combine(dir, updaterExecutableFilename);
            var zipFilePath = Path.Combine(dir, "rysy-update.zip");
            
            // Start a block here to ensure all streams get closed before we start the updater.
            {
                await GitHubApi.DownloadAssetAsync(_asset, zipFilePath);
            
                var zipStream = File.Open(zipFilePath, FileMode.Open);
                await using var zip = await ZipArchive.CreateAsync(zipStream, ZipArchiveMode.Read, false, null);

                if (zip.GetEntry(updaterExecutableFilename) is { } updaterEntry) {
                    await using var updaterStream = await updaterEntry.OpenAsync();
                    await using var updaterFileStream = File.Open(updaterExecutableFile, FileMode.Create);

                    await updaterStream.CopyToAsync(updaterFileStream);
                    await updaterFileStream.FlushAsync();
                }
            
                if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS()) {
                    try {
                        File.SetUnixFileMode(updaterExecutableFile,
                            UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
                    } catch (Exception ex) {
                        throw new Exception($"Failed add execute permission to updater executable file '{updaterExecutableFilename.Censor()}': {ex}");
                    }
                }  
            }
            
            Process.Start(new ProcessStartInfo
            {
                FileName = updaterExecutableFile,
                UseShellExecute = true,
                ArgumentList = { zipFilePath }
            });
            RysyState.Game.Exit();
        }
    }
}
