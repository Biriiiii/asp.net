using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using BookStore.Application.DTOs.Order;
using BookStore.Application.Interfaces;
using BookStore.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BookStore.Application.Services;

public class VnPayPaymentGatewayService : IPaymentGatewayService
{
    private readonly IConfiguration _config;
    private readonly ILogger<VnPayPaymentGatewayService> _logger;

    public VnPayPaymentGatewayService(IConfiguration config, ILogger<VnPayPaymentGatewayService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public Task<string> CreatePaymentUrlAsync(string orderCode, decimal amount, PaymentMethod method, string returnUrl)
    {
        if (method != PaymentMethod.VNPay)
            return Task.FromResult(returnUrl);

        var tmnCode = GetRequired("VnPay:TmnCode");
        var payUrl = GetRequired("VnPay:PayUrl");
        var locale = _config["VnPay:Locale"] ?? "vn";
        var currCode = _config["VnPay:CurrCode"] ?? "VND";
        var version = _config["VnPay:Version"] ?? "2.1.0";
        var command = _config["VnPay:Command"] ?? "pay";
        var orderType = _config["VnPay:OrderType"] ?? "other";

        var created = DateTime.Now;
        var parameters = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["vnp_Version"] = version,
            ["vnp_Command"] = command,
            ["vnp_TmnCode"] = tmnCode,
            ["vnp_Amount"] = ((long)(amount * 100)).ToString(CultureInfo.InvariantCulture),
            ["vnp_CreateDate"] = created.ToString("yyyyMMddHHmmss"),
            ["vnp_CurrCode"] = currCode,
            ["vnp_IpAddr"] = "127.0.0.1",
            ["vnp_Locale"] = locale,
            ["vnp_OrderInfo"] = $"Thanh toan don hang {orderCode}",
            ["vnp_OrderType"] = orderType,
            ["vnp_ReturnUrl"] = returnUrl,
            ["vnp_TxnRef"] = orderCode
        };

        var hashData = BuildQuery(parameters);
        var secureHash = HmacSha512(GetRequired("VnPay:HashSecret"), hashData);
        return Task.FromResult($"{payUrl}?{hashData}&vnp_SecureHash={secureHash}");
    }

    public bool VerifyCallback(PaymentCallbackRequest callback)
    {
        if (!string.Equals(callback.Provider, "vnpay", StringComparison.OrdinalIgnoreCase))
            return true;

        if (string.IsNullOrWhiteSpace(callback.RawData) || string.IsNullOrWhiteSpace(callback.Signature))
        {
            _logger.LogWarning("VNPay callback missing raw data or secure hash.");
            return false;
        }

        var parameters = ParseQuery(callback.RawData);
        parameters.Remove("vnp_SecureHash");
        parameters.Remove("vnp_SecureHashType");

        var hashData = BuildQuery(parameters);
        var expected = HmacSha512(GetRequired("VnPay:HashSecret"), hashData);
        var valid = string.Equals(expected, callback.Signature, StringComparison.OrdinalIgnoreCase);
        if (!valid)
            _logger.LogWarning("Invalid VNPay signature for order {OrderCode}.", callback.OrderCode);
        return valid;
    }

    private string GetRequired(string key) =>
        _config[key] ?? throw new InvalidOperationException($"Missing configuration: {key}");

    private static string BuildQuery(SortedDictionary<string, string> parameters) =>
        string.Join('&', parameters
            .Where(p => !string.IsNullOrWhiteSpace(p.Value))
            .Select(p => $"{WebUtility.UrlEncode(p.Key)}={WebUtility.UrlEncode(p.Value)}"));

    private static SortedDictionary<string, string> ParseQuery(string query)
    {
        var result = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var part in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var index = part.IndexOf('=');
            if (index <= 0) continue;
            var key = WebUtility.UrlDecode(part[..index]);
            var value = WebUtility.UrlDecode(part[(index + 1)..]);
            if (!string.IsNullOrWhiteSpace(key))
                result[key] = value ?? string.Empty;
        }
        return result;
    }

    private static string HmacSha512(string key, string input)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var inputBytes = Encoding.UTF8.GetBytes(input);
        using var hmac = new HMACSHA512(keyBytes);
        return Convert.ToHexString(hmac.ComputeHash(inputBytes)).ToLowerInvariant();
    }
}