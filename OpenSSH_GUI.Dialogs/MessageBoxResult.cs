namespace OpenSSH_GUI.Dialogs;

/// <summary>
/// Represents the button that the user clicked to close a <see cref="MessageBoxDialog"/>.
/// </summary>
public enum MessageBoxResult
{
    /// <summary>No result; dialog was closed without interaction (e.g. window close button).</summary>
    None,

    /// <summary>The user clicked OK.</summary>
    Ok,

    /// <summary>The user clicked Cancel.</summary>
    Cancel,

    /// <summary>The user clicked Yes.</summary>
    Yes,

    /// <summary>The user clicked No.</summary>
    No
}
