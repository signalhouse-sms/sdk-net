using SignalHouse.SDK.Exceptions;
using SignalHouse.SDK.Models;

namespace SignalHouse.SDK.Domains;

/// <summary>
/// Carrier phone-number checks on /verify: SIM Swap and Number Verify. Every check the carrier answers is billed to the
/// group's wallet (402 on insufficient funds); a check the carrier cannot run for the number is a 422 and is not charged.
/// </summary>
public sealed class VerifyDomain
{
    private readonly HttpClientWrapper _client;

    internal VerifyDomain(HttpClientWrapper client)
    {
        _client = client;
    }

    /// <summary>
    /// Check whether a number's SIM card changed recently, as reported by its carrier. Billed once when the check
    /// answers; 422 when the check is not available for the number (not charged). Roles: api, admin, developer.
    /// </summary>
    /// <param name="phoneNumber">The number to check, in international format (e.g. "+12025550123").</param>
    /// <param name="maxAge">Lookback window in hours, 1-2400; null uses the carrier's default.</param>
    /// <param name="includeSwapDate">Also return the latest SIM change date (no extra charge); null uses the API default (false).</param>
    /// <param name="options">Additional request options.</param>
    /// <returns>{ checkId, phoneNumber, swapped, maxAge, latestSimChange?, cost }.</returns>
    /// <exception cref="ValidationException">phoneNumber is null.</exception>
    public async Task<SignalHouseResponse> CheckSimSwapAsync(
        string phoneNumber,
        int? maxAge = null,
        bool? includeSwapDate = null,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "phoneNumber", phoneNumber } });

        var body = new Dictionary<string, object?>
        {
            { "phoneNumber", phoneNumber },
            { "maxAge", maxAge },
            { "includeSwapDate", includeSwapDate },
        };
        return await _client.RequestAsync("/verify/sim-swap", HttpMethod.Post, body, options);
    }

    /// <summary>
    /// Start a Number Verify check. The end user's phone must open the returned authorizationUrl on mobile data (Wi-Fi
    /// off) within 10 minutes; read the result with <see cref="GetNumberVerificationAsync"/>. Billed only when the check
    /// is answered (VERIFIED or NOT_VERIFIED). Roles: api, admin, developer.
    /// </summary>
    /// <param name="phoneNumber">The number to verify, in international format (e.g. "+12025550123").</param>
    /// <param name="devicePhoneNumber">The phone's own number, sent to the carrier as a login hint; null omits it.</param>
    /// <param name="redirectUrl">An https URL the end user's browser is sent to after the check, with ?checkId=…; null omits it.</param>
    /// <param name="options">Additional request options.</param>
    /// <returns>{ checkId, phoneNumber, status: "PENDING", authorizationUrl, expiresAt }.</returns>
    /// <exception cref="ValidationException">phoneNumber is null.</exception>
    public async Task<SignalHouseResponse> StartNumberVerificationAsync(
        string phoneNumber,
        string? devicePhoneNumber = null,
        string? redirectUrl = null,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "phoneNumber", phoneNumber } });

        var body = new Dictionary<string, object?>
        {
            { "phoneNumber", phoneNumber },
            { "devicePhoneNumber", devicePhoneNumber },
            { "redirectUrl", redirectUrl },
        };
        return await _client.RequestAsync("/verify/number", HttpMethod.Post, body, options);
    }

    /// <summary>
    /// Get a Number Verify check's status and result. A PENDING check past its expiresAt reads as EXPIRED.
    /// Roles: api, admin, developer, billing, user.
    /// </summary>
    /// <param name="checkId">The check ID (e.g. "NVC…").</param>
    /// <param name="options">Additional request options.</param>
    /// <returns>{ checkId, phoneNumber, status, devicePhoneNumberVerified, failureReason, createdAt, expiresAt, completedAt, cost }.</returns>
    /// <exception cref="ValidationException">checkId is null.</exception>
    public async Task<SignalHouseResponse> GetNumberVerificationAsync(
        string checkId,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "checkId", checkId } });

        var safeCheckId = Uri.EscapeDataString(checkId);
        return await _client.RequestAsync($"/verify/number/{safeCheckId}", HttpMethod.Get, options: options);
    }
}
