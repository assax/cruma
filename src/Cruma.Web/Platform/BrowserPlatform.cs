using Cruma.Ui.Services;
using Microsoft.JSInterop;

namespace Cruma.Web.Platform;

/// <summary>Přístup k modulu <c>cruma-web.js</c> – načte se jednou pro celou aplikaci.</summary>
public sealed class BrowserModule(IJSRuntime js) : IAsyncDisposable
{
    private Task<IJSObjectReference>? module;

    public Task<IJSObjectReference> GetAsync() => module ??= js.InvokeAsync<IJSObjectReference>("import", "./cruma-web.js").AsTask();

    public async ValueTask DisposeAsync()
    {
        if (module is not null)
        {
            await (await module).DisposeAsync();
        }
    }
}

/// <summary>
/// Připojení k serveru: stav prohlížeče (online/offline) a zároveň výsledek posledního volání API – server nedostupný
/// při „online“ prohlížeči je pro uživatele také offline (FR-29 akc. 4).
/// </summary>
public sealed class BrowserConnectivity : IConnectivity
{
    private bool browserOnline = true;
    private bool serverReachable = true;
    private DotNetObjectReference<BrowserConnectivity>? self;

    public bool IsOnline => browserOnline && serverReachable;

    public event Action? Changed;

    public async Task StartAsync(BrowserModule browser)
    {
        var module = await browser.GetAsync();
        browserOnline = await module.InvokeAsync<bool>("isOnline");
        self = DotNetObjectReference.Create(this);
        await module.InvokeVoidAsync("watchConnectivity", self);
    }

    [JSInvokable]
    public void OnBrowserConnectivityChanged(bool online)
    {
        browserOnline = online;
        if (online)
        {
            // Po návratu sítě se dostupnost serveru ověří dalším voláním.
            serverReachable = true;
        }

        Changed?.Invoke();
    }

    /// <summary>Výsledek volání API: síťová chyba znamená nedostupný server.</summary>
    public void ReportServerReachable(bool reachable)
    {
        if (serverReachable != reachable)
        {
            serverReachable = reachable;
            Changed?.Invoke();
        }
    }
}

/// <summary>Předvolby v localStorage (jen UI – UI-004).</summary>
public sealed class BrowserPreferences(BrowserModule browser) : IPreferences
{
    public async ValueTask<string?> GetAsync(string key) => await (await browser.GetAsync()).InvokeAsync<string?>("getPreference", key);

    public async ValueTask SetAsync(string key, string value) => await (await browser.GetAsync()).InvokeVoidAsync("setPreference", key, value);
}

/// <summary>Schopnosti tenkého klienta (ui-pattern.md §2).</summary>
public sealed class WebCapabilities : ICapabilities
{
    public bool WorksOffline => false;

    public bool EditorWidthModes => false;

    public bool QueuesNewNotesOffline => true;

    public bool ConflictResolution => true;
}
