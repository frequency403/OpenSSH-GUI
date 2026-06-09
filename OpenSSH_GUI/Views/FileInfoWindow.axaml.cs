using JetBrains.Annotations;
using OpenSSH_GUI.Core.Lib.Keys;
using OpenSSH_GUI.Core.Resources.Wrapper;
using OpenSSH_GUI.ViewModels;

namespace OpenSSH_GUI.Views;

[UsedImplicitly]
public partial class FileInfoWindow : WindowBase<FileInfoWindowViewModel, SshKeyFileSource>
{
    public FileInfoWindow() { InitializeComponent(); }
}