using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Material.Icons;

namespace OpenSSH_GUI.Resources.Controls;

public partial class Sorter : ToggleButton
{

    public Sorter()
    {
        InitializeComponent();
        this.GetObservable(SortDirectionProperty).Subscribe(x => SortDirectionIcon = EvaluateSortIconKind(x));
    }
    
    public static readonly StyledProperty<bool?> SortDirectionProperty =
        AvaloniaProperty.Register<Sorter, bool?>(nameof(SortDirection), defaultBindingMode: BindingMode.TwoWay, defaultValue: null);

    public static readonly StyledProperty<string> HeaderProperty =
        AvaloniaProperty.Register<Sorter, string>(nameof(Header), defaultBindingMode: BindingMode.TwoWay, defaultValue: string.Empty);

    public static readonly StyledProperty<MaterialIconKind> HeaderIconProperty =
        AvaloniaProperty.Register<Sorter, MaterialIconKind>(nameof(HeaderIcon), defaultValue: MaterialIconKind.About);

    public static readonly DirectProperty<Sorter, MaterialIconKind> SortDirectionIconProperty =
        AvaloniaProperty.RegisterDirect<Sorter, MaterialIconKind>(nameof(SortDirectionIcon), s => s.SortDirectionIcon, (s, v) => s.SortDirectionIcon = v);

    public static readonly DirectProperty<Sorter, MaterialIconKind> NeutralIconProperty =
        AvaloniaProperty.RegisterDirect<Sorter, MaterialIconKind>(nameof(NeutralIcon), s => s.NeutralIcon, (s, v) => s.NeutralIcon = v);

    public static readonly DirectProperty<Sorter, MaterialIconKind> UpIconProperty =
        AvaloniaProperty.RegisterDirect<Sorter, MaterialIconKind>(nameof(UpIcon), s => s.UpIcon, (s, v) => s.UpIcon = v);

    public static readonly DirectProperty<Sorter, MaterialIconKind> DownIconProperty =
        AvaloniaProperty.RegisterDirect<Sorter, MaterialIconKind>(nameof(DownIcon), s => s.DownIcon, (s, v) => s.DownIcon = v);

    public string Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public MaterialIconKind HeaderIcon
    {
        get => GetValue(HeaderIconProperty);
        set => SetValue(HeaderIconProperty, value);
    }

    public bool? SortDirection
    {
        get => GetValue(SortDirectionProperty);
        set
        {
            SortDirectionIcon = EvaluateSortIconKind(value);
            SetValue(SortDirectionProperty, value);
        }
    }

    public MaterialIconKind SortDirectionIcon
    {
        get;
        private set => SetAndRaise(SortDirectionIconProperty, ref field, value);
    }

    public MaterialIconKind NeutralIcon
    {
        get;
        set => SetAndRaise(NeutralIconProperty, ref field, value);
    } = MaterialIconKind.CircleOutline;

    public MaterialIconKind UpIcon
    {
        get;
        set => SetAndRaise(UpIconProperty, ref field, value);
    } = MaterialIconKind.ChevronUpCircleOutline;

    public MaterialIconKind DownIcon
    {
        get;
        set => SetAndRaise(DownIconProperty, ref field, value);
    } = MaterialIconKind.ChevronDownCircleOutline;

    private MaterialIconKind EvaluateSortIconKind(bool? value) =>
        value switch
        {
            null => NeutralIcon,
            true => DownIcon,
            false => UpIcon
        };
}