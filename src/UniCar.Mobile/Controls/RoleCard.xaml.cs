namespace UniCar.Mobile.Controls;

/// <summary>Cartão selecionável (estilo Uber) usado na escolha entre passageiro, motorista ou ambos.</summary>
public partial class RoleCard : ContentView
{
    private static readonly Color BrandColor = Color.FromArgb("#052355");

    public RoleCard()
    {
        InitializeComponent();
    }

    public event EventHandler? Tapped;

    public static readonly BindableProperty IconProperty = BindableProperty.Create(
        nameof(Icon), typeof(string), typeof(RoleCard), string.Empty,
        propertyChanged: (b, _, n) => ((RoleCard)b).IconLabel.Text = (string?)n);

    public static readonly BindableProperty TitleProperty = BindableProperty.Create(
        nameof(Title), typeof(string), typeof(RoleCard), string.Empty,
        propertyChanged: (b, _, n) => ((RoleCard)b).TitleLabel.Text = (string?)n);

    public static readonly BindableProperty SubtitleProperty = BindableProperty.Create(
        nameof(Subtitle), typeof(string), typeof(RoleCard), string.Empty,
        propertyChanged: (b, _, n) => ((RoleCard)b).SubtitleLabel.Text = (string?)n);

    public static readonly BindableProperty IsSelectedProperty = BindableProperty.Create(
        nameof(IsSelected), typeof(bool), typeof(RoleCard), false,
        propertyChanged: (b, _, n) => ((RoleCard)b).ApplySelection((bool)n));

    public string Icon { get => (string)GetValue(IconProperty); set => SetValue(IconProperty, value); }
    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string Subtitle { get => (string)GetValue(SubtitleProperty); set => SetValue(SubtitleProperty, value); }
    public bool IsSelected { get => (bool)GetValue(IsSelectedProperty); set => SetValue(IsSelectedProperty, value); }

    /// <summary>Valor enviado à API (PASSENGER, DRIVER ou BOTH).</summary>
    public string Value { get; set; } = string.Empty;

    private void ApplySelection(bool selected)
    {
        Card.Stroke = selected ? BrandColor : Color.FromArgb("#DCE3EF");
        Card.BackgroundColor = selected ? Color.FromArgb("#EDF2FA") : Colors.White;
        Radio.Stroke = selected ? BrandColor : Color.FromArgb("#BDBDBD");
        Dot.IsVisible = selected;
    }

    private void OnTapped(object? sender, TappedEventArgs e) => Tapped?.Invoke(this, EventArgs.Empty);
}
