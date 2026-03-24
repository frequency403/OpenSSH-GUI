using JetBrains.Annotations;
using OpenSSH_GUI.Core.Lib.Keys;
using OpenSSH_GUI.Core.MVVM;
using ReactiveUI.SourceGenerators;

namespace OpenSSH_GUI.ViewModels;
[UsedImplicitly]
public partial class FileInfoWindowViewModel : ViewModelBase<FileInfoWindowViewModel, FileInfoViewModelInitializer>
{
    [Reactive]
    private SshKeyFile _keyFile;
    
    public override ValueTask InitializeAsync(FileInfoViewModelInitializer parameters, CancellationToken cancellationToken = default)
    {
        KeyFile = parameters.Key;
        
        return base.InitializeAsync(parameters, cancellationToken);
    }
}

public class FileInfoViewModelInitializer : IInitializerParameters<FileInfoWindowViewModel>
{
    public required SshKeyFile Key { get; init; }
}