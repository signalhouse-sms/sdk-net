using SignalHouse.SDK.Domains;

namespace SignalHouse.SDK;

/// <summary>
/// Entry point for the SignalHouse .NET SDK.
/// Instantiate with your API key (or user token) and base URL, then access
/// each API domain through the corresponding property.
/// </summary>
public class SignalHouseClient
{
    /// <summary>Authentication operations (login, reset password, history).</summary>
    public AuthDomain Auth { get; }

    /// <summary>Billing, wallet, and payment method operations.</summary>
    public BillingDomain Billing { get; }

    /// <summary>10DLC brand registration and management.</summary>
    public BrandsDomain Brands { get; }

    /// <summary>10DLC campaign registration and management.</summary>
    public CampaignsDomain Campaigns { get; }

    /// <summary>Group management.</summary>
    public GroupsDomain Groups { get; }

    /// <summary>Landing page management.</summary>
    public LandingsDomain Landings { get; }

    /// <summary>SMS, MMS, and group message operations.</summary>
    public MessagesDomain Messages { get; }

    /// <summary>In-app notification management.</summary>
    public NotificationsDomain Notifications { get; }

    /// <summary>Phone number purchase, assignment, and management.</summary>
    public NumbersDomain Numbers { get; }

    /// <summary>Staff-only onboarding data (KYC, account owner, CRM, products).</summary>
    public OnboardingDomain Onboarding { get; }

    /// <summary>Sender registrations: the catalog, quotes, submission, status tracking and staff review for every jurisdiction.</summary>
    public RegistrationsDomain Registrations { get; }

    /// <summary>URL shortlink operations.</summary>
    public ShortlinksDomain Shortlinks { get; }

    /// <summary>Subgroup management.</summary>
    public SubgroupsDomain Subgroups { get; }

    /// <summary>Subscription and template management.</summary>
    public SubscriptionsDomain Subscriptions { get; }

    /// <summary>Versioned message templates on /template.</summary>
    public TemplatesDomain Templates { get; }

    /// <summary>Staff-only customer tickets (Jira create/search for groups).</summary>
    public TicketsDomain Tickets { get; }

    /// <summary>User account management.</summary>
    public UsersDomain Users { get; }

    /// <summary>Voice operations: SIP trunks, SIP endpoints (profiles), call origination, and WebRTC tokens.</summary>
    public VoiceDomain Voice { get; }

    /// <summary>Webhook management.</summary>
    public WebhooksDomain Webhooks { get; }

    /// <summary>AI Agents — customer-facing agent profile (AgentProfile) CRUD under /agent.</summary>
    public AgentsDomain Agents { get; }

    /// <summary>
    /// Create a new SignalHouse SDK client.
    /// </summary>
    /// <param name="apiKey">
    /// A Bearer token (API key or JWT) used to authenticate every request.
    /// Can be overridden per-request via <see cref="Models.RequestOptions.Token"/>.
    /// </param>
    /// <param name="baseUrl">
    /// The base URL of the SignalHouse API (e.g. "https://v2.signalhouse.io").
    /// </param>
    /// <param name="enableAdmin">
    /// When true, admin-only sub-domains (e.g. Campaigns.Admin, Groups.Admin) are
    /// populated. When false they remain null.
    /// </param>
    /// <param name="httpClient">
    /// Optional pre-configured HttpClient instance. When null, a new HttpClient is created internally.
    /// </param>
    public SignalHouseClient(string apiKey, string baseUrl, bool enableAdmin = false, HttpClient? httpClient = null)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("apiKey is required.", nameof(apiKey));
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new ArgumentException("baseUrl is required.", nameof(baseUrl));

        var client = new HttpClientWrapper(apiKey, baseUrl, httpClient);

        Auth = new AuthDomain(client);
        Billing = new BillingDomain(client);
        Brands = new BrandsDomain(client);
        Campaigns = new CampaignsDomain(client, enableAdmin);
        Groups = new GroupsDomain(client, enableAdmin);
        Landings = new LandingsDomain(client);
        Messages = new MessagesDomain(client);
        Notifications = new NotificationsDomain(client);
        Numbers = new NumbersDomain(client, enableAdmin);
        Onboarding = new OnboardingDomain(client, enableAdmin);
        Registrations = new RegistrationsDomain(client, enableAdmin);
        Shortlinks = new ShortlinksDomain(client);
        Subgroups = new SubgroupsDomain(client);
        Subscriptions = new SubscriptionsDomain(client, enableAdmin);
        Templates = new TemplatesDomain(client);
        Tickets = new TicketsDomain(client, enableAdmin);
        Users = new UsersDomain(client, enableAdmin);
        Voice = new VoiceDomain(client);
        Webhooks = new WebhooksDomain(client);
        Agents = new AgentsDomain(client);
    }
}
