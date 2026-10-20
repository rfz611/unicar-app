namespace UniCar.Mobile.Controls;

/// <summary>Campo de formulário no estilo Uber: rótulo, caixa cinza que fica com borda preta no foco e mensagem de erro.</summary>
public partial class FormField : ContentView
{
    private static readonly Color DangerColor = Color.FromArgb("#E11900");

    private static readonly Color BrandColor = Color.FromArgb("#052355");

    private bool _focused;
    private bool _revealed;

    public FormField()
    {
        InitializeComponent();
    }

    public event EventHandler<TextChangedEventArgs>? TextChanged;
    public event EventHandler? SearchRequested;
    public void UnfocusInput()=>Input.Unfocus();

    public static readonly BindableProperty TitleProperty = BindableProperty.Create(
        nameof(Title), typeof(string), typeof(FormField), string.Empty,
        propertyChanged: (b, _, n) => ((FormField)b).TitleLabel.Text = (string?)n);

    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(FormField), string.Empty,
        BindingMode.TwoWay,
        propertyChanged: (b, _, n) =>
        {
            var field = (FormField)b;
            var value = (string?)n ?? string.Empty;
            if (field.Input.Text != value) field.Input.Text = value;
        });

    public static readonly BindableProperty PlaceholderProperty = BindableProperty.Create(
        nameof(Placeholder), typeof(string), typeof(FormField), string.Empty,
        propertyChanged: (b, _, n) => ((FormField)b).Input.Placeholder = (string?)n);

    public static readonly BindableProperty InputKeyboardProperty = BindableProperty.Create(
        nameof(InputKeyboard), typeof(Keyboard), typeof(FormField), Keyboard.Default,
        propertyChanged: (b, _, n) => ((FormField)b).Input.Keyboard = (Keyboard)n);

    public static readonly BindableProperty IsPasswordProperty = BindableProperty.Create(
        nameof(IsPassword), typeof(bool), typeof(FormField), false,
        propertyChanged: (b, _, n) =>
        {
            var field = (FormField)b;
            var isPassword = (bool)n;
            field.ToggleLabel.IsVisible = isPassword;
            field.Input.IsPassword = isPassword && !field._revealed;
        });

    public static readonly BindableProperty MaxLengthProperty = BindableProperty.Create(
        nameof(MaxLength), typeof(int), typeof(FormField), int.MaxValue,
        propertyChanged: (b, _, n) => ((FormField)b).Input.MaxLength = (int)n);

    public static readonly BindableProperty ErrorTextProperty = BindableProperty.Create(
        nameof(ErrorText), typeof(string), typeof(FormField), string.Empty,
        propertyChanged: (b, _, n) =>
        {
            var field = (FormField)b;
            var message = (string?)n ?? string.Empty;
            field.ErrorLabel.Text = message;
            field.ErrorLabel.IsVisible = message.Length > 0;
            field.RefreshBorder();
        });

    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public string Placeholder { get => (string)GetValue(PlaceholderProperty); set => SetValue(PlaceholderProperty, value); }
    public Keyboard InputKeyboard { get => (Keyboard)GetValue(InputKeyboardProperty); set => SetValue(InputKeyboardProperty, value); }
    public bool IsPassword { get => (bool)GetValue(IsPasswordProperty); set => SetValue(IsPasswordProperty, value); }
    public int MaxLength { get => (int)GetValue(MaxLengthProperty); set => SetValue(MaxLengthProperty, value); }
    public string ErrorText { get => (string)GetValue(ErrorTextProperty); set => SetValue(ErrorTextProperty, value); }

    public bool HasError => !string.IsNullOrEmpty(ErrorText);

    private void OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        Text = e.NewTextValue ?? string.Empty;

        // Voltou a digitar: some com o erro deste campo.
        if (HasError) ErrorText = string.Empty;

        TextChanged?.Invoke(this, e);
    }

    private void OnFocused(object? sender, FocusEventArgs e)
    {
        _focused = true;
        SearchRequested?.Invoke(this,EventArgs.Empty);
        RefreshBorder();
    }

    private void OnUnfocused(object? sender, FocusEventArgs e)
    {
        _focused = false;
        RefreshBorder();
    }

    private void OnToggleTapped(object? sender, TappedEventArgs e)
    {
        _revealed = !_revealed;
        Input.IsPassword = !_revealed;
        ToggleLabel.Text = _revealed ? "Ocultar" : "Mostrar";
    }

    private void RefreshBorder()
    {
        Frame.Stroke = HasError
            ? DangerColor
            : _focused ? BrandColor : Colors.Transparent;
    }
}
