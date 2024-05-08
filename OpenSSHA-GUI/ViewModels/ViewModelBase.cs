using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace OpenSSHA_GUI.ViewModels;

public class ViewModelBase(ILogger logger) : ReactiveObject
{
    protected ILogger _logger = logger;
}