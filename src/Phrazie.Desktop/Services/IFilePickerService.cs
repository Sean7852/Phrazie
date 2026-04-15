namespace Phrazie.Desktop.Services;

public interface IFilePickerService
{
    /// <summary>Opens a system image picker. Returns the local file path, or null if cancelled.</summary>
    Task<string?> PickImageAsync();

    /// <summary>Opens a system video picker allowing multi-select. Returns selected local file paths.</summary>
    Task<IReadOnlyList<string>> PickVideoFilesAsync();
}
