using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using Avalonia;
using Avalonia.Controls;
using Material.Icons;
using ReactiveUI;

namespace OpenSSH_GUI.Resources.Controls;

public partial class SubmitButtons : UserControl
{
    private readonly CompositeDisposable disposables = new();
    
    public static readonly DirectProperty<SubmitButtons, ReactiveCommand<bool, Unit>> BooleanSubmitProperty =
        AvaloniaProperty.RegisterDirect<SubmitButtons, ReactiveCommand<bool, Unit>>(nameof(BooleanSubmit),
            c => c.BooleanSubmit,
            (c, v) => c.BooleanSubmit = v);

    public static readonly DirectProperty<SubmitButtons, bool> AbortButtonEnabledProperty =
         AvaloniaProperty.RegisterDirect<SubmitButtons, bool>(nameof(AbortButtonEnabled), 
             c => c.AbortButtonEnabled, (c, v) => c.AbortButtonEnabled = v);

    public static readonly DirectProperty<SubmitButtons, bool> SubmitButtonEnabledProperty =
         AvaloniaProperty.RegisterDirect<SubmitButtons, bool>(nameof(SubmitButtonEnabled), 
             c => c.SubmitButtonEnabled, (c, v) => c.SubmitButtonEnabled = v);

    public static readonly DirectProperty<SubmitButtons, string> AbortButtonTooltipProperty =
         AvaloniaProperty.RegisterDirect<SubmitButtons, string>(nameof(AbortButtonTooltip), 
             c => c.AbortButtonTooltip, (c, v) => c.AbortButtonTooltip = v);

    public static readonly DirectProperty<SubmitButtons, string> SubmitButtonTooltipProperty =
         AvaloniaProperty.RegisterDirect<SubmitButtons, string>(nameof(SubmitButtonTooltip), 
             c => c.SubmitButtonTooltip, (c, v) => c.SubmitButtonTooltip = v);

    public static readonly DirectProperty<SubmitButtons, MaterialIconKind> AbortButtonIconKindProperty =
         AvaloniaProperty.RegisterDirect<SubmitButtons, MaterialIconKind>(nameof(AbortButtonIconKind),
             c  => c.AbortButtonIconKind, (c, v) => c.AbortButtonIconKind = v);

    public static readonly DirectProperty<SubmitButtons, MaterialIconKind> SubmitButtonIconKindProperty =
         AvaloniaProperty.RegisterDirect<SubmitButtons, MaterialIconKind>(nameof(SubmitButtonIconKind),
            c => c.SubmitButtonIconKind, (c, v) => c.SubmitButtonIconKind = v);

    public static readonly DirectProperty<SubmitButtons, Control?> AbortButtonContentProperty =
        AvaloniaProperty.RegisterDirect<SubmitButtons, Control?>(nameof(AbortButtonContent),
            c => c.AbortButtonContent, (c, v) => c.AbortButtonContent = v);

    public static readonly DirectProperty<SubmitButtons, bool> AbortButtonContentEnabledProperty =
        AvaloniaProperty.RegisterDirect<SubmitButtons, bool>(nameof(AbortButtonContentEnabled),
            c => c.AbortButtonContentEnabled, (c, v) => c.AbortButtonContentEnabled = v);

    public bool AbortButtonContentEnabled
    {
        get;
        set => SetAndRaise(AbortButtonContentEnabledProperty, ref field, value);
    } = false;

    public Control? AbortButtonContent
    {
        get;
        set => SetAndRaise(AbortButtonContentProperty, ref field, value);
    } = null;
    
    public static readonly DirectProperty<SubmitButtons, Control?> SubmitButtonContentProperty =
        AvaloniaProperty.RegisterDirect<SubmitButtons, Control?>(nameof(SubmitButtonContent),
            c => c.SubmitButtonContent, (c, v) => c.SubmitButtonContent = v);
    
    public static readonly DirectProperty<SubmitButtons, bool> SubmitButtonContentEnabledProperty =
        AvaloniaProperty.RegisterDirect<SubmitButtons, bool>(nameof(SubmitButtonContentEnabled),
            c => c.SubmitButtonContentEnabled, (c, v) => c.SubmitButtonContentEnabled = v);

    public bool SubmitButtonContentEnabled
    {
        get;
        set => SetAndRaise(SubmitButtonContentEnabledProperty, ref field, value);
    } = false;

    public Control? SubmitButtonContent
    {
        get;
        set => SetAndRaise(SubmitButtonContentProperty, ref field, value);
    } = null;


    public ReactiveCommand<bool, Unit> BooleanSubmit
    {
        get;
        set => SetAndRaise(BooleanSubmitProperty, ref field, value);
    } = ReactiveCommand.Create<bool, Unit>(_ => new Unit());


    public bool AbortButtonEnabled
    {
        get;
        set => SetAndRaise(AbortButtonEnabledProperty, ref field, value);
    } = true;


    public bool SubmitButtonEnabled
    {
        get;
        set => SetAndRaise(SubmitButtonEnabledProperty, ref field, value);
    } = true;


    public string AbortButtonTooltip
    {
        get;
        set => SetAndRaise(AbortButtonTooltipProperty, ref field, value);
    } = StringsAndTexts.CancelAndClose;


    public string SubmitButtonTooltip
    {
        get;
        set => SetAndRaise(SubmitButtonTooltipProperty, ref field, value);
    } = StringsAndTexts.SaveAndClose;


    public MaterialIconKind AbortButtonIconKind
    {
        get;
        set => SetAndRaise(AbortButtonIconKindProperty, ref field, value);
    } = MaterialIconKind.CancelOutline;


    public MaterialIconKind SubmitButtonIconKind
    {
        get;
        set => SetAndRaise(SubmitButtonIconKindProperty, ref field, value);
    } = MaterialIconKind.CheckOutline;

    public SubmitButtons()
    {
        this.WhenAnyValue(x => x.AbortButtonContent)
            .Subscribe(x => AbortButtonContentEnabled = x is not null)
            .DisposeWith(disposables);
        
        this.WhenAnyValue(x => x.SubmitButtonContent)
            .Subscribe(x => SubmitButtonContentEnabled = x is not null)
            .DisposeWith(disposables);
        
        InitializeComponent();
    }
    
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        disposables.Dispose();
    }
}