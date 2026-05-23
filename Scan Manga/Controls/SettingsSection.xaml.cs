namespace Scan_Manga.Controls;

public partial class SettingsSection : ContentView
{
	public static readonly BindableProperty TitleProperty = BindableProperty.Create(
		nameof(Title), typeof(string), typeof(SettingsSection), default(string));

	public string Title
	{
		get => (string)GetValue(TitleProperty);
		set => SetValue(TitleProperty, value);
	}

	public SettingsSection()
	{
		InitializeComponent();
	}
}
