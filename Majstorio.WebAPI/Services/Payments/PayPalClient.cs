using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Majstorio.Services.PayPal;
using Microsoft.Extensions.Caching.Memory;

namespace Majstorio.WebAPI.Services.Payments;

// Calls the PayPal sandbox REST API. The HttpClient comes from IHttpClientFactory and the settings from .env (PayPal__*).
public class PayPalClient : IPayPalClient
{
    private const string Currency = "EUR";
    private const string TokenCacheKey = "paypal-token";

    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PayPalClient> _logger;
    private readonly string _clientId;
    private readonly string _clientSecret;

    public PayPalClient(HttpClient http, IMemoryCache cache, IConfiguration configuration, ILogger<PayPalClient> logger)
    {
        _http = http;
        _cache = cache;
        _logger = logger;
        _clientId = configuration["PayPal:ClientId"] ?? string.Empty;
        _clientSecret = configuration["PayPal:ClientSecret"] ?? string.Empty;
        ReturnUrl = configuration["PayPal:ReturnUrl"] ?? string.Empty;
        CancelUrl = configuration["PayPal:CancelUrl"] ?? string.Empty;
    }

    public string ReturnUrl { get; }
    public string CancelUrl { get; }

    public async Task<PayPalOrder> CreateOrderAsync(decimal amount, Guid requestId)
    {
        var body = new
        {
            intent = "CAPTURE",
            purchase_units = new[]
            {
                new { amount = new { currency_code = Currency, value = ToPayPalAmount(amount) } }
            },
            payment_source = new
            {
                paypal = new
                {
                    experience_context = new
                    {
                        user_action = "PAY_NOW",
                        shipping_preference = "NO_SHIPPING",
                        return_url = ReturnUrl,
                        cancel_url = CancelUrl
                    }
                }
            }
        };

        var response = await PostAsync<PayPalOrderResponse>("v2/checkout/orders", body, requestId);

        var approvalLink = response.Links.FirstOrDefault(link => link.Rel == "payer-action");
        if (approvalLink == null)
            throw new PayPalException("PayPal sent no approval page.");

        return new PayPalOrder
        {
            OrderId = response.Id,
            ApprovalUrl = approvalLink.Href
        };
    }

    public async Task<PayPalCapture> CaptureOrderAsync(string orderId, Guid requestId)
    {
        var response = await PostAsync<PayPalOrderResponse>($"v2/checkout/orders/{orderId}/capture", new { }, requestId);

        var capture = response.PurchaseUnits.FirstOrDefault()?.Payments?.Captures.FirstOrDefault();
        if (capture == null || capture.Status != "COMPLETED")
            throw new PayPalException("PayPal did not complete the payment.");

        return new PayPalCapture
        {
            CaptureId = capture.Id,
            Amount = decimal.Parse(capture.Amount.Value, CultureInfo.InvariantCulture)
        };
    }

    public async Task<string> RefundCaptureAsync(string captureId, decimal amount, Guid requestId)
    {
        var body = new
        {
            amount = new { currency_code = Currency, value = ToPayPalAmount(amount) }
        };

        var response = await PostAsync<PayPalRefundResponse>($"v2/payments/captures/{captureId}/refund", body, requestId);

        if (response.Status != "COMPLETED")
            throw new PayPalException("PayPal did not complete the refund.");

        return response.Id;
    }

    // The same PayPal-Request-Id on a repeated call returns PayPal's first answer,
    // so a payment is never charged or refunded twice.
    private async Task<T> PostAsync<T>(string path, object body, Guid requestId)
    {
        var token = await GetTokenAsync();

        using var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("PayPal-Request-Id", requestId.ToString());
        request.Content = JsonContent.Create(body);

        using var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("PayPal answered {StatusCode} for {Path}: {Error}", (int)response.StatusCode, path, error);
            throw new PayPalException("PayPal refused the call.");
        }

        var result = await response.Content.ReadFromJsonAsync<T>();
        if (result == null)
            throw new PayPalException("PayPal sent an empty answer.");

        return result;
    }

    // PayPal tokens last several hours, so one token is kept in the cache until a minute before it expires.
    private async Task<string> GetTokenAsync()
    {
        if (_cache.TryGetValue(TokenCacheKey, out string? cachedToken) && cachedToken != null)
            return cachedToken;

        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}"));

        using var request = new HttpRequestMessage(HttpMethod.Post, "v1/oauth2/token");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["grant_type"] = "client_credentials" });

        using var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("PayPal refused the sandbox credentials ({StatusCode}). Check PayPal__ClientId and "
                + "PayPal__ClientSecret in .env.", (int)response.StatusCode);
            throw new PayPalException("PayPal refused the credentials.");
        }

        var result = await response.Content.ReadFromJsonAsync<PayPalTokenResponse>();
        if (result == null)
            throw new PayPalException("PayPal sent no token.");

        _cache.Set(TokenCacheKey, result.AccessToken, TimeSpan.FromSeconds(result.ExpiresIn - 60));
        return result.AccessToken;
    }

    // PayPal expects "120.00": two decimals and a dot.
    private static string ToPayPalAmount(decimal amount)
    {
        return amount.ToString("0.00", CultureInfo.InvariantCulture);
    }
}
