using SignalHouse.SDK.Models;

namespace SignalHouse.SDK.Domains.Voice;

/// <summary>
/// WebRTC voice tokens — mints ephemeral SIP identities the browser/mobile voice
/// SDK uses to register against the SignalHouse voice fabric. Wraps voice-
/// backend's <c>/voice/tokens</c> endpoint.
/// Accessed via <c>client.Voice.Tokens</c>.
///
/// Customer flow:
/// <list type="number">
///   <item>Backend calls <c>client.Voice.Tokens.CreateAsync(...)</c> and returns the resulting <c>token</c> to the customer's browser/app.</item>
///   <item>Browser SDK registers using the embedded <c>sip_credentials</c>.</item>
///   <item>Outbound calls from the SDK go through OpenSIPS → Asterisk → carrier; the server-side <c>Calls.CreateAsync</c> flow with <c>toIdentity</c> can also ring this identity for click-to-call patterns.</item>
/// </list>
/// </summary>
public sealed class TokensDomain
{
    private readonly HttpClientWrapper _client;

    internal TokensDomain(HttpClientWrapper client)
    {
        _client = client;
    }

    /// <summary>
    /// Mint an ephemeral voice token + SIP credentials. All fields on
    /// <paramref name="tokenData"/> are optional: <c>identity</c> (logical name,
    /// defaults to "anonymous"), <c>ttl</c> (seconds, clamped to server max),
    /// <c>grants</c> (capability grants). When omitted entirely, an empty body is sent.
    /// Returns the token plus embedded SIP credentials (the password is sent ONCE,
    /// never recoverable).
    /// <c>POST /voice/tokens</c>.
    /// </summary>
    public async Task<SignalHouseResponse> CreateAsync(IDictionary<string, object?>? tokenData = null, RequestOptions? options = null)
    {
        var body = tokenData ?? new Dictionary<string, object?>();
        return await _client.RequestAsync("/voice/tokens", HttpMethod.Post, body, options);
    }
}
