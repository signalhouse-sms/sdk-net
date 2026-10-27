using SignalHouse.SDK.Models;

namespace SignalHouse.SDK.Domains;

/// <summary>
/// Billing, wallet, payment method, invoice, and fee operations.
/// </summary>
/// <remarks>
/// Date-range parameters (startDate, endDate) on list and history methods
/// accept ISO-8601 date or timestamp strings. The server normalizes them to
/// full UTC-day boundaries: startDate -&gt; start-of-UTC-day, endDate -&gt; end-of-
/// UTC-day. The end day is always included; hourly resolution is not supported.
/// </remarks>
public sealed class BillingDomain
{
    private readonly HttpClientWrapper _client;

    internal BillingDomain(HttpClientWrapper client)
    {
        _client = client;
    }

    /// <summary>
    /// Get transaction history with optional filters.
    /// </summary>
    public async Task<SignalHouseResponse> GetTransactionHistoryAsync(
        string? groupId = null,
        string? subgroupId = null,
        string? brandId = null,
        string? campaignId = null,
        string? entryType = null,
        string? transactionType = null,
        string? startDate = null,
        string? endDate = null,
        RequestOptions? options = null)
    {
        var qs = HttpClientWrapper.GetQueryString(new Dictionary<string, string?>
        {
            { "groupId", groupId },
            { "subgroupId", subgroupId },
            { "brandId", brandId },
            { "campaignId", campaignId },
            { "entryType", entryType },
            { "transactionType", transactionType },
            { "startDate", startDate },
            { "endDate", endDate },
        });

        return await _client.RequestAsync($"/billing/wallet/transactionHistory{qs}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Get payment history (wallet recharge transactions) with server-side pagination.
    /// </summary>
    /// <param name="groupId">The group ID.</param>
    /// <param name="startDate">Start date in YYYY-MM-DD format (inclusive).</param>
    /// <param name="endDate">End date in YYYY-MM-DD format (inclusive).</param>
    /// <param name="page">Page number (positive integer, default 1).</param>
    /// <param name="limit">Results per page (positive integer, max 10000, default 10).</param>
    /// <param name="timezone">IANA timezone (default: "UTC").</param>
    /// <param name="options">Optional per-request overrides.</param>
    public async Task<SignalHouseResponse> GetPaymentHistoryAsync(
        string groupId,
        string startDate,
        string endDate,
        int? page = null,
        int? limit = null,
        string? timezone = null,
        RequestOptions? options = null)
    {
        var qs = HttpClientWrapper.GetQueryString(new Dictionary<string, string?>
        {
            { "groupId", groupId },
            { "startDate", startDate },
            { "endDate", endDate },
            { "page", page?.ToString() },
            { "limit", limit?.ToString() },
            { "timezone", timezone },
        });

        return await _client.RequestAsync($"/billing/wallet/paymentHistory{qs}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Get the wallet information for a specific group.
    /// </summary>
    /// <param name="groupId">The group ID.</param>
    /// <param name="options">Optional per-request overrides.</param>
    public async Task<SignalHouseResponse> GetWalletAsync(string groupId, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "groupId", groupId } });

        var safeGroupId = Uri.EscapeDataString(groupId);
        return await _client.RequestAsync($"/billing/wallet/{safeGroupId}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Update the wallet settings for a specific group.
    /// </summary>
    public async Task<SignalHouseResponse> UpdateWalletAsync(
        string groupId,
        bool? autoRechargeEnabled = null,
        int? autoRechargeThreshold = null,
        int? autoRechargeToAmount = null,
        string? primaryPaymentMethodId = null,
        string? secondaryPaymentMethodId = null,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "groupId", groupId } });

        var safeGroupId = Uri.EscapeDataString(groupId);
        return await _client.RequestAsync($"/billing/wallet/{safeGroupId}", HttpMethod.Put, new
        {
            autoRechargeEnabled,
            autoRechargeThreshold,
            autoRechargeToAmount,
            primaryPaymentMethodId,
            secondaryPaymentMethodId,
        }, options);
    }

    /// <summary>
    /// Get the payment methods for a specific group.
    /// </summary>
    public async Task<SignalHouseResponse> GetPaymentMethodsAsync(string groupId, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "groupId", groupId } });

        var safeGroupId = Uri.EscapeDataString(groupId);
        return await _client.RequestAsync($"/billing/wallet/paymentMethods/{safeGroupId}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Add funds to a group's wallet.
    /// </summary>
    /// <param name="groupId">The group ID.</param>
    /// <param name="amount">Amount in microdollars (e.g. 10_000_000 = $10).</param>
    /// <param name="paymentMethodId">Optional payment method ID to charge.</param>
    /// <param name="options">Optional per-request overrides.</param>
    public async Task<SignalHouseResponse> AddFundsAsync(
        string groupId,
        int amount,
        string? paymentMethodId = null,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "groupId", groupId }, { "amount", amount } });

        var safeGroupId = Uri.EscapeDataString(groupId);
        return await _client.RequestAsync($"/billing/wallet/addfunds/{safeGroupId}", HttpMethod.Post, new
        {
            amount,
            paymentMethodId,
        }, options);
    }

    /// <summary>
    /// Add a payment method to a group's wallet.
    /// </summary>
    public async Task<SignalHouseResponse> AddPaymentMethodAsync(
        string groupId,
        string paymentMethodId,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "groupId", groupId }, { "paymentMethodId", paymentMethodId } });

        var safeGroupId = Uri.EscapeDataString(groupId);
        return await _client.RequestAsync($"/billing/wallet/paymentMethod/{safeGroupId}", HttpMethod.Post, new { paymentMethodId }, options);
    }

    /// <summary>
    /// Remove a payment method from a group's wallet.
    /// </summary>
    public async Task<SignalHouseResponse> RemovePaymentMethodAsync(
        string groupId,
        string paymentMethodId,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "groupId", groupId }, { "paymentMethodId", paymentMethodId } });

        var safeGroupId = Uri.EscapeDataString(groupId);
        var safePaymentMethodId = Uri.EscapeDataString(paymentMethodId);
        return await _client.RequestAsync($"/billing/wallet/paymentMethod/{safeGroupId}/{safePaymentMethodId}", HttpMethod.Delete, options: options);
    }

    /// <summary>
    /// Get invoice details with optional filters.
    /// </summary>
    public async Task<SignalHouseResponse> GetInvoiceDetailsAsync(
        string? groupId = null,
        string? subgroupId = null,
        string? startDate = null,
        string? endDate = null,
        RequestOptions? options = null)
    {
        var qs = HttpClientWrapper.GetQueryString(new Dictionary<string, string?>
        {
            { "groupId", groupId },
            { "subgroupId", subgroupId },
            { "startDate", startDate },
            { "endDate", endDate },
        });

        return await _client.RequestAsync($"/billing/invoiceDetails{qs}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Get the fee schedule for a specific group.
    /// When the group is on a custom volume-tiered plan, the response also includes volumePricing: tiers, the current period's dates, count and minimum, ramp, and current rate (rates in microdollars per segment).
    /// </summary>
    public async Task<SignalHouseResponse> GetFeesAsync(string groupId, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "groupId", groupId } });

        var safeGroupId = Uri.EscapeDataString(groupId);
        return await _client.RequestAsync($"/billing/fees/{safeGroupId}", HttpMethod.Get, options: options);
    }
}
