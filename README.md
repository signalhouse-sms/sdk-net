# SignalHouse .NET SDK

Official .NET SDK for the [SignalHouse](https://signalhouse.io) SMS/MMS messaging platform.

Full API reference and guides: https://app2.signalhouse.io/docs

- Targets **.NET 8**
- Zero external dependencies (`System.Net.Http` + `System.Text.Json` only)
- Fully async (`Task<SignalHouseResponse>`) API
- 14 domain modules matching the REST API one-to-one

## Installation

```bash
dotnet add package SignalHouseSms.Sdk
```

Or via the NuGet Package Manager:

```
Install-Package SignalHouseSms.Sdk
```

## Quick Start

### Initialize the client

```csharp
using SignalHouse.SDK;

var client = new SignalHouseClient(
    apiKey: "your-api-key",
    baseUrl: "https://v2.signalhouse.io"
);
```

### Login

```csharp
var response = await client.Auth.LoginAsync("user@example.com", "password123");

if (response.Success)
{
    Console.WriteLine($"Token: {response.Data}");
}
else
{
    Console.WriteLine($"Login failed: {response.Error}");
}
```

### Send an SMS

```csharp
var response = await client.Messages.SendSmsAsync(
    senderPhoneNumber: "15551234567",
    recipientPhoneNumbers: new List<string> { "15559876543" },
    messageBody: "Hello from SignalHouse!"
);

Console.WriteLine($"Status: {response.Status}, Success: {response.Success}");
```

### Send an MMS with images

```csharp
using var imageStream = File.OpenRead("photo.jpg");

var response = await client.Messages.SendMmsAsync(
    senderPhoneNumber: "15551234567",
    recipientPhoneNumbers: new List<string> { "15559876543" },
    messageBody: "Check out this image!",
    images: new List<Stream> { imageStream },
    imageFileNames: new List<string> { "photo.jpg" }
);
```

### List phone numbers with pagination

```csharp
var response = await client.Numbers.GetPhoneNumbersAsync(
    groupId: "G0000001",
    page: 1,
    limit: 20
);
```

### Purchase phone numbers

```csharp
var response = await client.Numbers.PurchasePhoneNumberAsync(
    phoneNumbers: new List<string> { "15551112222" },
    subgroupId: "SG000001"
);
```

### Override token per request

```csharp
using SignalHouse.SDK.Models;

var options = new RequestOptions { Token = "another-user-token" };
var response = await client.Users.GetUsersAsync(groupId: "G0000001", options: options);
```

### Idempotent requests

Set `RequestOptions.IdempotencyKey` to make a create, update or delete safe to retry: the SDK sends it as the `Idempotency-Key` header, and a retry with the same key and request replays the first response instead of running it again. Keys are scoped to the authenticated user and kept for 24 hours.

```csharp
var options = new RequestOptions { IdempotencyKey = Guid.NewGuid().ToString() };
```

### Admin operations

Enable admin mode at client creation to access admin-only endpoints:

```csharp
var adminClient = new SignalHouseClient(
    apiKey: "admin-api-key",
    baseUrl: "https://v2.signalhouse.io",
    enableAdmin: true
);

// List all groups (admin only)
var groups = await adminClient.Groups.Admin!.GetGroupsAsync(page: 1, limit: 50);

// Approve a campaign (admin only)
var result = await adminClient.Campaigns.Admin!.ApproveCampaignAsync("campaign-id");
```

## Response Format

Every method returns a `SignalHouseResponse`:

| Property  | Type           | Description                                    |
|-----------|----------------|------------------------------------------------|
| `Success` | `bool`         | `true` when the HTTP status is 2xx             |
| `Data`    | `JsonElement?` | Parsed JSON response body                      |
| `Error`   | `string?`      | Error message (from response or reason phrase)  |
| `Status`  | `int`          | HTTP status code                               |

## Domains

| Property        | Domain Class           | Methods |
|-----------------|------------------------|---------|
| `Auth`          | `AuthDomain`           | 3       |
| `Billing`       | `BillingDomain`        | 9       |
| `Brands`        | `BrandsDomain`         | 19      |
| `Campaigns`     | `CampaignsDomain`      | 4 + 2 admin |
| `Groups`        | `GroupsDomain`         | 2 + 3 admin |
| `Landings`      | `LandingsDomain`       | 6       |
| `Messages`      | `MessagesDomain`       | 5       |
| `Notifications` | `NotificationsDomain`  | 3       |
| `Numbers`       | `NumbersDomain`        | 11      |
| `Shortlinks`    | `ShortlinksDomain`     | 2       |
| `Subgroups`     | `SubgroupsDomain`      | 4       |
| `Subscriptions` | `SubscriptionsDomain`  | 3 + 5 admin |
| `Templates`     | `TemplatesDomain`      | 10      |
| `Users`         | `UsersDomain`          | 7 + 2 admin |
| `Webhooks`      | `WebhooksDomain`       | 4       |

## License

ISC
