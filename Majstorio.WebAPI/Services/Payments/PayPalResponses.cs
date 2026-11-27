using System.Text.Json.Serialization;

namespace Majstorio.WebAPI.Services.Payments;

// The parts of PayPal's JSON answers that the API reads. PayPal writes names with underscores.

public class PayPalTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    // Seconds until the token expires.
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
}

public class PayPalOrderResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("links")]
    public List<PayPalLink> Links { get; set; } = [];

    [JsonPropertyName("purchase_units")]
    public List<PayPalPurchaseUnit> PurchaseUnits { get; set; } = [];
}

public class PayPalLink
{
    [JsonPropertyName("href")]
    public string Href { get; set; } = string.Empty;

    // "payer-action" is the page where the customer approves the payment.
    [JsonPropertyName("rel")]
    public string Rel { get; set; } = string.Empty;
}

public class PayPalPurchaseUnit
{
    [JsonPropertyName("payments")]
    public PayPalPayments? Payments { get; set; }
}

public class PayPalPayments
{
    [JsonPropertyName("captures")]
    public List<PayPalCaptureResponse> Captures { get; set; } = [];
}

public class PayPalCaptureResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public PayPalAmount Amount { get; set; } = new();
}

public class PayPalAmount
{
    [JsonPropertyName("currency_code")]
    public string CurrencyCode { get; set; } = string.Empty;

    // PayPal sends the amount as text, for example "120.00".
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}

public class PayPalRefundResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}
