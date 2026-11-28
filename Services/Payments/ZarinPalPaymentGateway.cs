#nullable enable

using Common;
using Services.Interfaces.Payments;
using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace Services.Payments
{
    public class ZarinPalPaymentGateway : IPaymentGateway, IScopedDependency
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ZarinPalSettings _settings;

        public ZarinPalPaymentGateway(
            IHttpClientFactory httpClientFactory,
            Microsoft.Extensions.Options.IOptions<ZarinPalSettings> settings)
        {
            _httpClientFactory = httpClientFactory;
            _settings = settings.Value;
        }

        public async Task<GatewayPaymentRequestResult> RequestPaymentAsync(
            GatewayPaymentRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.Amount <= 0)
                throw new ArgumentException("مبلغ پرداخت باید بیشتر از صفر باشد.");

            ValidateSettings();
            ValidateCallbackUrl(request.CallbackUrl);

            long amountInRial = checked(request.Amount * 10);

            var body = new
            {
                merchant_id = _settings.MerchantId,
                amount = amountInRial,
                callback_url = request.CallbackUrl,
                description = request.Description,
                metadata = new
                {
                    mobile = request.Mobile,
                    email = request.Email
                }
            };

            string url =
                $"{_settings.ApiBaseUrl.TrimEnd('/')}/pg/v4/payment/request.json";

            var result = await SendRequestAsync(url, body);

            if (result.Code != 100)
            {
                return new GatewayPaymentRequestResult
                {
                    IsSuccess = false,
                    IsIndeterminate = result.IsIndeterminate,
                    ErrorCode = result.ErrorCode,
                    ErrorMessage = result.ErrorMessage
                };
            }

            if (string.IsNullOrWhiteSpace(result.Authority))
            {
                return new GatewayPaymentRequestResult
                {
                    IsSuccess = false,
                    IsIndeterminate = true,
                    ErrorMessage =
                        "زرین‌پال کد موفقیت 100 را برگرداند، اما Authority دریافت نشد؛ نتیجه درخواست نامشخص است."
                };
            }

            string startPayBaseUrl = _settings.IsSandbox
                ? "https://sandbox.zarinpal.com/pg/StartPay/"
                : "https://www.zarinpal.com/pg/StartPay/";

            return new GatewayPaymentRequestResult
            {
                IsSuccess = true,
                Authority = result.Authority,
                PaymentUrl = startPayBaseUrl + result.Authority
            };
        }

        public async Task<GatewayPaymentVerificationResult> VerifyPaymentAsync(
            string authority,
            long amount)
        {
            if (string.IsNullOrWhiteSpace(authority))
                throw new ArgumentException("Authority معتبر نیست.");

            if (amount <= 0)
                throw new ArgumentException("مبلغ پرداخت باید بیشتر از صفر باشد.");

            ValidateSettings();

            long amountInRial = checked(amount * 10);

            var body = new
            {
                merchant_id = _settings.MerchantId,
                amount = amountInRial,
                authority = authority
            };

            string url =
                $"{_settings.ApiBaseUrl.TrimEnd('/')}/pg/v4/payment/verify.json";

            var result = await SendRequestAsync(url, body);

            bool hasSuccessfulCode =
                result.Code == 100 || result.Code == 101;

            bool hasTrackingCode =
                !string.IsNullOrWhiteSpace(result.TrackingCode);

            bool isSuccess = hasSuccessfulCode && hasTrackingCode;

            bool isIndeterminate =
                result.IsIndeterminate ||
                (hasSuccessfulCode && !hasTrackingCode);

            return new GatewayPaymentVerificationResult
            {
                IsSuccess = isSuccess,
                IsIndeterminate = isIndeterminate,
                TrackingCode = result.TrackingCode,
                ErrorCode = result.ErrorCode,
                ErrorMessage = isSuccess
                    ? null
                    : result.ErrorMessage ??
                      "تأیید پرداخت ناموفق بود یا شماره پیگیری دریافت نشد."
            };
        }

        private void ValidateSettings()
        {
            if (string.IsNullOrWhiteSpace(_settings.MerchantId))
                throw new InvalidOperationException(
                    "MerchantId زرین‌پال تنظیم نشده است.");

            if (string.IsNullOrWhiteSpace(_settings.ApiBaseUrl))
                throw new InvalidOperationException(
                    "ApiBaseUrl زرین‌پال تنظیم نشده است.");

            if (!Uri.TryCreate(
                    _settings.ApiBaseUrl,
                    UriKind.Absolute,
                    out Uri? apiUri))
            {
                throw new InvalidOperationException(
                    "ApiBaseUrl زرین‌پال معتبر نیست.");
            }

            if (apiUri.Scheme != Uri.UriSchemeHttps)
                throw new InvalidOperationException(
                    "آدرس API زرین‌پال باید از HTTPS استفاده کند.");

            string expectedHost = _settings.IsSandbox
                ? "sandbox.zarinpal.com"
                : "api.zarinpal.com";

            if (!string.Equals(
                    apiUri.Host,
                    expectedHost,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    _settings.IsSandbox
                        ? "در حالت Sandbox باید ApiBaseUrl برابر https://sandbox.zarinpal.com باشد."
                        : "در حالت Production باید ApiBaseUrl برابر https://api.zarinpal.com باشد.");
            }

            if (apiUri.Port != 443 ||
                !string.IsNullOrEmpty(apiUri.UserInfo) ||
                !string.IsNullOrEmpty(apiUri.Query) ||
                !string.IsNullOrEmpty(apiUri.Fragment) ||
                (apiUri.AbsolutePath != "/" &&
                 !string.IsNullOrEmpty(apiUri.AbsolutePath)))
            {
                throw new InvalidOperationException(
                    "ApiBaseUrl باید فقط شامل آدرس اصلی سرور زرین‌پال باشد.");
            }
        }

        private void ValidateCallbackUrl(string? callbackUrl)
        {
            if (!Uri.TryCreate(
                    callbackUrl,
                    UriKind.Absolute,
                    out Uri? callbackUri))
            {
                throw new ArgumentException(
                    "آدرس بازگشت پرداخت باید یک آدرس مطلق معتبر باشد.");
            }

            if (callbackUri.Scheme != Uri.UriSchemeHttp &&
                callbackUri.Scheme != Uri.UriSchemeHttps)
            {
                throw new ArgumentException(
                    "آدرس بازگشت پرداخت باید از HTTP یا HTTPS استفاده کند.");
            }
        }

        private async Task<ZarinPalResponse> SendRequestAsync(
            string url,
            object body)
        {
            try
            {
                using var client = _httpClientFactory.CreateClient();

                using var response = await client.PostAsJsonAsync(url, body);

                string json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    int statusCode = (int)response.StatusCode;

                    bool isIndeterminate =
                        statusCode == 408 ||
                        statusCode == 429 ||
                        statusCode >= 500;

                    return new ZarinPalResponse
                    {
                        IsIndeterminate = isIndeterminate,
                        ErrorCode = statusCode.ToString(
                            CultureInfo.InvariantCulture),
                        ErrorMessage = isIndeterminate
                            ? $"نتیجه درخواست به زرین‌پال نامشخص است. کد HTTP: {statusCode}"
                            : $"درخواست به زرین‌پال رد شد. کد HTTP: {statusCode}"
                    };
                }

                using var document = JsonDocument.Parse(json);

                JsonElement root = document.RootElement;

                if (root.ValueKind != JsonValueKind.Object)
                {
                    return CreateIndeterminateResponse(
                        "ساختار پاسخ دریافتی از زرین‌پال معتبر نیست.");
                }

                if (root.TryGetProperty("errors", out var errors))
                {
                    bool hasErrors =
                        (errors.ValueKind == JsonValueKind.Object &&
                         errors.EnumerateObject().Any()) ||
                        (errors.ValueKind == JsonValueKind.Array &&
                         errors.GetArrayLength() > 0);

                    if (hasErrors)
                    {
                        string? errorCode = GetErrorCode(root, 0);
                        string? errorMessage = GetErrorMessage(root);

                        // کد خطای عددی و منفی، به‌تنهایی
                        // برای قطعی دانستن شکست کافی نیست.
                        // تا زمان مشخص شدن وضعیت کد، نامشخص می‌ماند.
                        bool hasValidErrorCode =
                            int.TryParse(
                                errorCode,
                                NumberStyles.Integer,
                                CultureInfo.InvariantCulture,
                                out int parsedErrorCode) &&
                            parsedErrorCode < 0;

                        return new ZarinPalResponse
                        {
                            Code = 0,
                            IsIndeterminate = true,
                            ErrorCode = errorCode,
                            ErrorMessage = errorMessage ??
                                (hasValidErrorCode
                                    ? "زرین‌پال خطا برگرداند؛ وضعیت نهایی درخواست نیازمند بررسی است."
                                    : "ساختار یا کد خطای زرین‌پال معتبر نیست؛ نتیجه نامشخص است.")
                        };
                    }
                }

                if (!root.TryGetProperty("data", out var data) ||
                    data.ValueKind != JsonValueKind.Object ||
                    !data.TryGetProperty("code", out var codeElement) ||
                    codeElement.ValueKind != JsonValueKind.Number ||
                    !codeElement.TryGetInt32(out int code))
                {
                    return CreateIndeterminateResponse(
                        "پاسخ زرین‌پال فاقد کد وضعیت معتبر است؛ نتیجه درخواست نامشخص است.");
                }

                return new ZarinPalResponse
                {
                    Code = code,
                    Authority = GetStringValue(root, "data", "authority"),
                    TrackingCode = GetStringValue(root, "data", "ref_id"),
                    ErrorCode = GetErrorCode(root, code),
                    ErrorMessage = GetErrorMessage(root)
                };
            }
            catch (HttpRequestException)
            {
                return CreateIndeterminateResponse(
                    "نتیجه درخواست به درگاه نامشخص است؛ ارتباط با زرین‌پال برقرار نشد.");
            }
            catch (TaskCanceledException)
            {
                return CreateIndeterminateResponse(
                    "نتیجه درخواست به درگاه نامشخص است؛ مهلت ارتباط با زرین‌پال به پایان رسید.");
            }
            catch (JsonException)
            {
                return CreateIndeterminateResponse(
                    "پاسخ دریافتی از زرین‌پال معتبر نیست و نتیجه درخواست مشخص نیست.");
            }
        }

        private static ZarinPalResponse CreateIndeterminateResponse(
            string message)
        {
            return new ZarinPalResponse
            {
                IsIndeterminate = true,
                ErrorMessage = message
            };
        }

        private static string? GetStringValue(
            JsonElement root,
            string parentName,
            string propertyName)
        {
            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty(parentName, out var parent) ||
                parent.ValueKind != JsonValueKind.Object ||
                !parent.TryGetProperty(propertyName, out var value))
            {
                return null;
            }

            return value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
        }

        private static string? GetErrorCode(
            JsonElement root,
            int code)
        {
            if (root.ValueKind == JsonValueKind.Object &&
                root.TryGetProperty("errors", out var errors))
            {
                JsonElement errorItem = default;
                bool hasErrorItem = false;

                if (errors.ValueKind == JsonValueKind.Object)
                {
                    errorItem = errors;
                    hasErrorItem = true;
                }
                else if (errors.ValueKind == JsonValueKind.Array &&
                         errors.GetArrayLength() > 0 &&
                         errors[0].ValueKind == JsonValueKind.Object)
                {
                    errorItem = errors[0];
                    hasErrorItem = true;
                }

                if (hasErrorItem &&
                    errorItem.TryGetProperty("code", out var errorCode) &&
                    errorCode.ValueKind == JsonValueKind.Number &&
                    errorCode.TryGetInt32(out int parsedErrorCode))
                {
                    return parsedErrorCode.ToString(
                        CultureInfo.InvariantCulture);
                }
            }

            return code != 0
                ? code.ToString(CultureInfo.InvariantCulture)
                : null;
        }

        private static string? GetErrorMessage(JsonElement root)
        {
            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("errors", out var errors))
            {
                return null;
            }

            JsonElement errorItem = default;
            bool hasErrorItem = false;

            if (errors.ValueKind == JsonValueKind.Object)
            {
                errorItem = errors;
                hasErrorItem = true;
            }
            else if (errors.ValueKind == JsonValueKind.Array &&
                     errors.GetArrayLength() > 0 &&
                     errors[0].ValueKind == JsonValueKind.Object)
            {
                errorItem = errors[0];
                hasErrorItem = true;
            }

            if (!hasErrorItem ||
                !errorItem.TryGetProperty("message", out var message) ||
                message.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            string? errorMessage = message.GetString();

            return string.IsNullOrWhiteSpace(errorMessage)
                ? null
                : errorMessage;
        }

        private class ZarinPalResponse
        {
            public int Code { get; set; }
            public string? Authority { get; set; }
            public string? TrackingCode { get; set; }
            public string? ErrorCode { get; set; }
            public string? ErrorMessage { get; set; }
            public bool IsIndeterminate { get; set; }
        }
    }
}