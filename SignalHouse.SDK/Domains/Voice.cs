using SignalHouse.SDK.Domains.Voice;

namespace SignalHouse.SDK.Domains;

/// <summary>
/// Voice domain — wraps the Voice service (mounted under <c>/voice</c>).
/// Sub-resources:
/// <list type="bullet">
///   <item><c>client.Voice.SipTrunks</c> — peer-to-peer SIP connections to a PBX/carrier.</item>
///   <item><c>client.Voice.SipProfiles</c> — single registerable SIP endpoints.</item>
///   <item><c>client.Voice.ProgrammableVoiceProfiles</c> — route a set of numbers through Signal House.</item>
///   <item><c>client.Voice.NumberRoutes</c> — per-number inbound route (overrides the profile and subgroup settings).</item>
///   <item><c>client.Voice.Calls</c> — outbound call origination and call log queries.</item>
///   <item><c>client.Voice.CallLogs</c> — account-scoped call history: list/get + presigned recording.</item>
///   <item><c>client.Voice.Analytics</c> — aggregated call metrics (summary tiles + byDate/byNumber/byCarrier breakdowns).</item>
///   <item><c>client.Voice.GlobalVoiceSettings</c> — account-wide voice defaults (accepted regions, max spend, E911).</item>
///   <item><c>client.Voice.Tokens</c> — mint ephemeral SIP credentials for the browser voice SDK.</item>
/// </list>
/// </summary>
public sealed class VoiceDomain
{
    /// <summary>SIP trunk management.</summary>
    public SipTrunksDomain SipTrunks { get; }

    /// <summary>SIP profile (endpoint) management.</summary>
    public SipProfilesDomain SipProfiles { get; }

    /// <summary>Programmable Voice Profile management.</summary>
    public ProgrammableVoiceProfilesDomain ProgrammableVoiceProfiles { get; }

    /// <summary>Per-number inbound route (overrides the Programmable Voice Profile and subgroup settings).</summary>
    public NumberRoutesDomain NumberRoutes { get; }

    /// <summary>Outbound call origination and call log queries.</summary>
    public CallsDomain Calls { get; }

    /// <summary>Account-scoped call history: list/get + presigned recording.</summary>
    public CallLogsDomain CallLogs { get; }

    /// <summary>Aggregated voice call metrics: per-direction summary tiles + byDate/byNumber/byCarrier breakdowns.</summary>
    public AnalyticsDomain Analytics { get; }

    /// <summary>Account-wide voice defaults (accepted regions, max spend per minute, E911).</summary>
    public GlobalVoiceSettingsDomain GlobalVoiceSettings { get; }

    /// <summary>Ephemeral voice token + SIP credential minting for the browser/mobile voice SDK.</summary>
    public TokensDomain Tokens { get; }

    internal VoiceDomain(HttpClientWrapper client)
    {
        SipTrunks = new SipTrunksDomain(client);
        SipProfiles = new SipProfilesDomain(client);
        ProgrammableVoiceProfiles = new ProgrammableVoiceProfilesDomain(client);
        NumberRoutes = new NumberRoutesDomain(client);
        Calls = new CallsDomain(client);
        CallLogs = new CallLogsDomain(client);
        Analytics = new AnalyticsDomain(client);
        GlobalVoiceSettings = new GlobalVoiceSettingsDomain(client);
        Tokens = new TokensDomain(client);
    }
}
