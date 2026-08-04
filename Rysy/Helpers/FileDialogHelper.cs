using NativeFileDialogSharp;
using Rysy.Extensions;
using Rysy.Scenes;
using System.Diagnostics.CodeAnalysis;

namespace Rysy.Helpers;

public static class FileDialogHelper {
    public static string GetDefaultPath() {
        if (EditorState.Current?.Map?.Filepath?.Directory() is { } dir) {
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

    public static bool TryOpenDir([NotNullWhen(true)] out string? chosenDir, string? defaultPath = null) {
        var res = Dialog.FolderPicker((defaultPath ?? GetDefaultPath()).CorrectSlashes());

        return HandleResult(res, extension: null, out chosenDir, "FileDialogHelper.TryOpenDir");
    }

    private static bool HandleResult(DialogResult res, string? extension, [NotNullWhen(true)] out string? chosen, string logTag) {
        if (res.IsOk) {
            chosen = AddExtIfNeeded(extension, res.Path);
            return true;
        }

        if (res.IsError) {
            Logger.Write(logTag, LogLevel.Error, $"Failed to pick path: {res.ErrorMessage}");
        }

        chosen = null;
        return false;
    }

    private static string AddExtIfNeeded(string? extension, string chosen) {
        if (extension is null)
            return chosen;

        var extString = $".{extension}";
        if (!chosen.EndsWith(extString, StringComparison.Ordinal))
            chosen += extString;

        return chosen;
    }
}
