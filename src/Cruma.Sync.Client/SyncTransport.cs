using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cruma.Sync;

namespace Cruma.Sync.Client;

/// <summary>Výsledek volání synchronizačního protokolu.</summary>
public sealed record TransportResult<T>(T? Value, TransportFailure Failure = TransportFailure.None, string? ErrorCode = null, string? MinimumVersion = null)
{
    public bool IsSuccess => Failure == TransportFailure.None;
}

public enum TransportFailure
{
    None,

    /// <summary>Server nedostupný – klient pracuje dál offline.</summary>
    Network,

    /// <summary>Klient pod minimální verzí (SYN-001, FR-37 akc. 1).</summary>
    VersionUnsupported,

    /// <summary>Přihlášení chybí nebo vypršelo – lokální práce pokračuje (SEC-003).</summary>
    Unauthenticated,

    /// <summary>Jiná chyba serveru.</summary>
    Server,
}

/// <summary>Synchronizační protokol (versioning-and-sync-pattern.md §5.1) nad přenosem.</summary>
public interface ISyncTransport
{
    Task<TransportResult<HandshakeResponse>> HandshakeAsync(HandshakeRequest request, CancellationToken cancellationToken);

    Task<TransportResult<PushResponse>> PushAsync(PushRequest request, CancellationToken cancellationToken);

    Task<TransportResult<PullResponse>> PullAsync(PullRequest request, CancellationToken cancellationToken);
}

/// <summary>Identita klienta pro handshake a hlavičky push/pull.</summary>
public sealed class SyncClientOptions
{
    public string AppVersion { get; set; } = "0.0.0";

    public Guid ClientInstanceId { get; set; }

    /// <summary>Pravidelná synchronizace, dokud je server dostupný (§5.2).</summary>
    public TimeSpan PeriodicInterval { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>Zpoždění synchronizace po lokální úpravě – krátce po konci editační relace (§5.2).</summary>
    public TimeSpan AfterEditDelay { get; set; } = TimeSpan.FromSeconds(10);

    public int PushBatchSize { get; set; } = 100;

    public int PullPageSize { get; set; } = 500;
}

/// <summary>Přenos přes HTTP na endpointy <c>/api/sync/v1</c> serveru Cruma.</summary>
public sealed class HttpSyncTransport(HttpClient http, SyncClientOptions options) : ISyncTransport
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    public Task<TransportResult<HandshakeResponse>> HandshakeAsync(HandshakeRequest request, CancellationToken cancellationToken) =>
        PostAsync<HandshakeResponse>("api/sync/v1/handshake", request, cancellationToken);

    public Task<TransportResult<PushResponse>> PushAsync(PushRequest request, CancellationToken cancellationToken) =>
        PostAsync<PushResponse>("api/sync/v1/push", request, cancellationToken);

    public Task<TransportResult<PullResponse>> PullAsync(PullRequest request, CancellationToken cancellationToken) =>
        PostAsync<PullResponse>("api/sync/v1/pull", request, cancellationToken);

    private async Task<TransportResult<T>> PostAsync<T>(string url, object body, CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body, body.GetType(), options: Json) };
        message.Headers.Add("X-Cruma-App-Version", options.AppVersion);
        message.Headers.Add("X-Cruma-Protocol-Version", SyncProtocol.CurrentVersion.ToString(System.Globalization.CultureInfo.InvariantCulture));
        message.Headers.Add("X-Cruma-Client", "desktop");
        message.Headers.Add("X-Cruma-Client-Instance", options.ClientInstanceId.ToString());

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(message, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return new TransportResult<T>(default, TransportFailure.Network);
        }

        using (response)
        {
            if (response.IsSuccessStatusCode)
            {
                return new TransportResult<T>(await response.Content.ReadFromJsonAsync<T>(Json, cancellationToken));
            }

            var problem = await ReadProblemAsync(response, cancellationToken);
            return response.StatusCode switch
            {
                HttpStatusCode.UpgradeRequired => new TransportResult<T>(default, TransportFailure.VersionUnsupported, problem.Code, problem.MinimumVersion),
                HttpStatusCode.Unauthorized => new TransportResult<T>(default, TransportFailure.Unauthenticated, problem.Code),
                _ => new TransportResult<T>(default, TransportFailure.Server, problem.Code),
            };
        }
    }

    private static async Task<(string? Code, string? MinimumVersion)> ReadProblemAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var root = document.RootElement;
            return (
                root.TryGetProperty("code", out var code) ? code.GetString() : null,
                root.TryGetProperty("minimumVersion", out var minimum) ? minimum.GetString() : null);
        }
        catch (JsonException)
        {
            // Odpověď bez ProblemDetails (proxy) – rozhoduje stavový kód.
            return (null, null);
        }
    }
}
