using Cruma.Ui.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Cruma.Ui;

/// <summary>Předvolby vzhledu sdílené komponentami: motiv (FR-34) a šířka editoru (FR-8). Ukládají se přes <see cref="IPreferences"/>.</summary>
public sealed class CrumaUiState(IPreferences preferences)
{
    public const string ThemeKey = "cruma.theme";
    public const string EditorWidthKey = "cruma.editorWidth";

    public static readonly IReadOnlyList<string> EditorWidths = ["standard", "wide", "full"];

    private bool loaded;

    public string Theme { get; private set; } = "light";

    public string EditorWidth { get; private set; } = "standard";

    public event Action? Changed;

    public async Task LoadAsync()
    {
        if (loaded)
        {
            return;
        }

        loaded = true;
        Theme = await preferences.GetAsync(ThemeKey) is "dark" ? "dark" : "light";
        EditorWidth = await preferences.GetAsync(EditorWidthKey) is { } width && EditorWidths.Contains(width) ? width : "standard";
        Changed?.Invoke();
    }

    public async Task SetThemeAsync(string theme)
    {
        Theme = theme == "dark" ? "dark" : "light";
        await preferences.SetAsync(ThemeKey, Theme);
        Changed?.Invoke();
    }

    public async Task SetEditorWidthAsync(string width)
    {
        EditorWidth = EditorWidths.Contains(width) ? width : "standard";
        await preferences.SetAsync(EditorWidthKey, EditorWidth);
        Changed?.Invoke();
    }
}

/// <summary>Barvy karet poznámek – klíče tokenů tématu (UI-006).</summary>
public static class NoteColors
{
    public static readonly IReadOnlyList<(string Key, string Label)> All =
    [
        ("yellow", "Žlutá"),
        ("green", "Zelená"),
        ("blue", "Modrá"),
        ("pink", "Růžová"),
        ("purple", "Fialová"),
        ("gray", "Šedá"),
    ];

    public static string? CssClass(string? color) => color is null ? null : $"cruma-card-color-{color}";
}

/// <summary>Registrace sdíleného UI v kompozičním kořeni shellu.</summary>
public static class CrumaUiServiceCollectionExtensions
{
    public static IServiceCollection AddCrumaUi(this IServiceCollection services) => services.AddScoped<CrumaUiState>();
}
