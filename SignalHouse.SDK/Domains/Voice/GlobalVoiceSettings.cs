using SignalHouse.SDK.Models;

namespace SignalHouse.SDK.Domains.Voice;

/// <summary>
/// Global Voice Settings — account-wide voice defaults (accepted regions, max
/// spend per minute, E911). SIP trunks and Programmable Voice Profiles override
/// these. Calls the Voice service mounted under /voice. Accessed via
/// <c>client.Voice.GlobalVoiceSettings</c>.
/// </summary>
public sealed class GlobalVoiceSettingsDomain
{
    private readonly HttpClientWrapper _client;

    internal GlobalVoiceSettingsDomain(HttpClientWrapper client)
    {
        _client = client;
    }

    /// <summary>
    /// Get the current account's global voice settings. Returns <c>data: null</c> if
    /// the account has never configured them (defaults apply).
    /// Returns <c>{ success, data: { accountId, acceptedRegions, maxSpendPerMinute, e911Enabled, ... } | null }</c>.
    /// <c>GET /voice/api/v1/global-voice-settings</c>.
    /// </summary>
    public async Task<SignalHouseResponse> GetAsync(RequestOptions? options = null)
        => await _client.RequestAsync("/voice/api/v1/global-voice-settings", HttpMethod.Get, options: options);

    /// <summary>
    /// Upsert the account's global voice settings. Partial — only the fields you
    /// provide are written; omitted fields are preserved. Setting keys (camelCase):
    /// <c>acceptedRegions</c> (string[]; empty = all allowed),
    /// <c>maxSpendPerMinute</c> (double or null; null = no limit),
    /// <c>e911Enabled</c> (bool).
    /// Returns <c>{ success, data: { ...settings } }</c>.
    /// <c>PUT /voice/api/v1/global-voice-settings</c>.
    /// </summary>
    public async Task<SignalHouseResponse> UpdateAsync(IDictionary<string, object?> settings, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "settings", settings } });
        return await _client.RequestAsync("/voice/api/v1/global-voice-settings", new HttpMethod("PUT"), settings, options);
    }
}
