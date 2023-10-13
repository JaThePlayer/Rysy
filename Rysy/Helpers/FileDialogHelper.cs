using NativeFileDialogSharp;
using Rysy.Extensions;
using Rysy.Scenes;
using System.Diagnostics.CodeAnalysis;

namespace Rysy.Helpers;

public static class FileDialogHelper {
    public static string GetDefaultPath() {
        if (RysyEngine.Scene is EditorScene editor && editor.Map?.Filepath?.Directory() is { } dir) {
            return dir;
        }

        return Profile.Instance.ModsDirectory;
    }

    public static bool TrySave(string filterList, [NotNullWhen(true)] out string? chosenFile, string? defaultPath = null) {
        var res = Dialog.FileSave(filterList, (defaultPath ?? GetDefaultPath()).CorrectSlashes());

        return HandleResult(res, filterList, out chosenFile, "FileDialogHelper.TrySave");
    }

    public static bool TryOpen(string filterList, [NotNullWhen(true)] out string? chosenFile, string? defaultPath = null) {
        var res = Dialog.FileOpen(filterList, (defaultPath ?? GetDefaultPath()).CorrectSlashes());

        return HandleResult(res, filterList, out chosenFile, "FileDialogHelper.TryOpen");
    }

    private static bool HandleResult(DialogResult res, string filterList, [NotNullWhen(true)] out string? chosenFile, string logTag) {
        if (res.IsOk) {
            chosenFile = AddExtIfNeeded(filterList, res.Path);
            return true;
        }

        if (res.IsError) {
            Logger.Write(logTag, LogLevel.Error, $"Failed to pick path: {res.ErrorMessage}");
        }

        chosenFile = null;
        return false;
    }

    private static string AddExtIfNeeded(string filterList, string chosenFile) {
        var extString = $".{filterList}";
        if (!chosenFile.EndsWith(extString, StringComparison.Ordinal))
            chosenFile += extString;

        return chosenFile;
    }
}
