using ReactiveUI;

namespace OpenSSHA_GUI.ViewModels;

public class ConfirmDialogViewModel : ViewModelBase
{
    public ConfirmDialogViewModel() : this(StringsAndTexts.ConfirmDialogDefaultQuestionText, StringsAndTexts.ConfirmDialogDefaultOkText, StringsAndTexts.ConfirmDialogDefaultNoText)
    {
    }

    public ConfirmDialogViewModel(string question, string okText, string notOkText)
    {
        DialogResult = ReactiveCommand.Create<string, ConfirmDialogViewModel>(
            d =>
            {
                Consent = bool.Parse(d);
                return this;
            });
        Question = question;
        OkText = okText;
        NotOkText = notOkText;
        Consent = false;
    }

    public string Question { get; }
    public string OkText { get; }
    public string NotOkText { get; }

    public bool Consent { get; private set; }
    public ReactiveCommand<string, ConfirmDialogViewModel> DialogResult { get; }
}