using System.Windows;

namespace MouseMacro.Services;

public sealed class ThemeService
{
    private const string DarkThemeUri = "Themes/DarkTheme.xaml";
    private const string LightThemeUri = "Themes/LightTheme.xaml";

    public void ApplyTheme(string? themeName)
    {
        var uri = new Uri(
            string.Equals(themeName, "Light", StringComparison.OrdinalIgnoreCase) ? LightThemeUri : DarkThemeUri,
            UriKind.Relative);

        var theme = new ResourceDictionary { Source = uri };
        var merged = Application.Current.Resources.MergedDictionaries;

        for (int i = merged.Count - 1; i >= 0; i--)
        {
            var source = merged[i].Source;
            if (source is not null
                && (source.OriginalString.Contains("DarkTheme.xaml", StringComparison.OrdinalIgnoreCase)
                    || source.OriginalString.Contains("LightTheme.xaml", StringComparison.OrdinalIgnoreCase)))
            {
                merged.RemoveAt(i);
            }
        }

        merged.Insert(0, theme);
    }
}
