using MauiIcons.MaterialSymbols.Rounded;
using Scan_Manga.Helpers;
using System.Windows.Input;

namespace Scan_Manga.Controls;

public partial class IconButton : ContentView
{
	public event EventHandler? Clicked;

	public static readonly BindableProperty IconProperty =
		BindableProperty.Create(nameof(Icon), typeof(MaterialSymbolsRoundedIcons), typeof(IconButton));

	public static readonly BindableProperty TextProperty =
		BindableProperty.Create(nameof(Text), typeof(string), typeof(IconButton));

	public static readonly BindableProperty RotateIconOnClickProperty =
		BindableProperty.Create(nameof(RotateIconOnClick), typeof(bool), typeof(IconButton));

	public static readonly BindableProperty IconColorProperty =
		BindableProperty.Create(nameof(IconColor), typeof(Color), typeof(IconButton));

	public static readonly BindableProperty TextColorProperty =
		BindableProperty.Create(nameof(TextColor), typeof(Color), typeof(IconButton), Colors.Black);

	public static readonly BindableProperty TextTransformProperty =
		BindableProperty.Create(nameof(TextTransform), typeof(TextTransform), typeof(IconButton), TextTransform.Uppercase);

	public static readonly BindableProperty IconSizeProperty =
		BindableProperty.Create(nameof(IconSize), typeof(double), typeof(IconButton), 30d);

	public static readonly BindableProperty FontSizeProperty =
		BindableProperty.Create(nameof(FontSize), typeof(double), typeof(IconButton), 10d);

	public static readonly BindableProperty FontAttributesProperty =
		BindableProperty.Create(nameof(FontAttributes), typeof(FontAttributes), typeof(IconButton), FontAttributes.Bold);

	public static readonly BindableProperty SpacingProperty =
		BindableProperty.Create(nameof(Spacing), typeof(double), typeof(IconButton), 4d);

	public static readonly BindableProperty CommandProperty =
		BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(IconButton), null);

	public static readonly BindableProperty CommandParameterProperty =
		BindableProperty.Create(nameof(CommandParameter), typeof(object), typeof(IconButton), null);

	public IconButton()
	{
		InitializeComponent();

		if (Application.Current != null)
		{
			Application.Current.Resources.TryGetValue("Primary", out var primary);
			Application.Current.Resources.TryGetValue("PrimaryDark", out var primaryDark);
			Application.Current.Resources.TryGetValue("White", out var white);
			Application.Current.Resources.TryGetValue("OffBlack", out var offBlack);

			this.SetAppThemeColor(IconColorProperty, primary as Color, primaryDark as Color);
			this.SetAppThemeColor(TextColorProperty, offBlack as Color, white as Color);
		}
	}

	public MaterialSymbolsRoundedIcons Icon
	{
		get => (MaterialSymbolsRoundedIcons)GetValue(IconProperty);
		set => SetValue(IconProperty, value);
	}

	public string Text
	{
		get => (string)GetValue(TextProperty);
		set => SetValue(TextProperty, value);
	}

	public bool RotateIconOnClick
	{
		get => (bool)GetValue(RotateIconOnClickProperty);
		set => SetValue(RotateIconOnClickProperty, value);
	}

	public Color IconColor
	{
		get => (Color)GetValue(IconColorProperty);
		set => SetValue(IconColorProperty, value);
	}

	public Color TextColor
	{
		get => (Color)GetValue(TextColorProperty);
		set => SetValue(TextColorProperty, value);
	}

	public TextTransform TextTransform
	{
		get => (TextTransform)GetValue(TextTransformProperty);
		set => SetValue(TextTransformProperty, value);
	}

	public double IconSize
	{
		get => (double)GetValue(IconSizeProperty);
		set => SetValue(IconSizeProperty, value);
	}

	public double FontSize
	{
		get => (double)GetValue(FontSizeProperty);
		set => SetValue(FontSizeProperty, value);
	}

	public FontAttributes FontAttributes
	{
		get => (FontAttributes)GetValue(FontAttributesProperty);
		set => SetValue(FontAttributesProperty, value);
	}

	public double Spacing
	{
		get => (double)GetValue(SpacingProperty);
		set => SetValue(SpacingProperty, value);
	}

	public ICommand? Command
	{
		get => (ICommand?)GetValue(CommandProperty);
		set => SetValue(CommandProperty, value);
	}

	public object? CommandParameter
	{
		get => GetValue(CommandParameterProperty);
		set => SetValue(CommandParameterProperty, value);
	}

	async void OnButtonTapped(object? sender, TappedEventArgs e)
	{
		if (sender is VerticalStackLayout tappedBtn)
		{
			Task<bool>? rotationTask = null;

			if (RotateIconOnClick)
			{
				var btnLabel = tappedBtn.Children.OfType<Label>().FirstOrDefault();
				if (btnLabel != null)
				{
					btnLabel.Rotation = 0;
					rotationTask = btnLabel.RotateToAsync(360, 500);
				}
			}

			await tappedBtn.ScaleToSafe(.7, 100);
			await tappedBtn.ScaleToSafe(1, 100);

			if (rotationTask is not null)
			{
				await rotationTask;
			}

			Clicked?.Invoke(this, e);

			var cmd = Command;
			if (cmd != null && cmd.CanExecute(CommandParameter))
			{
				cmd.Execute(CommandParameter);
			}
		}
	}
}