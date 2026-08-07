using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace AttendVisionReportsApi.Services.HikCentral
{
    // Signs outgoing requests per HikCentral's Artemis OpenAPI AK/SK scheme
    // (Alibaba Cloud API Gateway-style HMAC-SHA256 over method/headers/resource).
    public class ArtemisSigningHandler(IConfiguration configuration) : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var appKey = configuration["HikCentral:AppKey"] ?? "";
            var appSecret = configuration["HikCentral:AppSecret"] ?? "";

            const string accept = "application/json";
            const string contentType = "application/json";

            var bodyBytes = request.Content is null
                ? Array.Empty<byte>()
                : await request.Content.ReadAsByteArrayAsync(cancellationToken);

            var contentMd5 = bodyBytes.Length > 0 ? Convert.ToBase64String(MD5.HashData(bodyBytes)) : "";
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

            request.Headers.Accept.Clear();
            request.Headers.Accept.ParseAdd(accept);

            if (request.Content is not null)
            {
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
                if (contentMd5.Length > 0)
                    request.Content.Headers.TryAddWithoutValidation("Content-MD5", contentMd5);
            }

            request.Headers.TryAddWithoutValidation("X-Ca-Key", appKey);
            request.Headers.TryAddWithoutValidation("X-Ca-Timestamp", timestamp);
            request.Headers.TryAddWithoutValidation("X-Ca-Signature-Headers", "x-ca-key,x-ca-timestamp");

            var canonicalizedHeaders = $"x-ca-key:{appKey}\nx-ca-timestamp:{timestamp}\n";
            var canonicalizedResource = BuildCanonicalizedResource(request.RequestUri!);

            var stringToSign = $"{request.Method.Method}\n{accept}\n{contentMd5}\n{contentType}\n{canonicalizedHeaders}{canonicalizedResource}";
            var signature = Convert.ToBase64String(HMACSHA256.HashData(Encoding.UTF8.GetBytes(appSecret), Encoding.UTF8.GetBytes(stringToSign)));

            request.Headers.TryAddWithoutValidation("X-Ca-Signature", signature);

            return await base.SendAsync(request, cancellationToken);
        }

        private static string BuildCanonicalizedResource(Uri uri)
        {
            var path = uri.AbsolutePath;
            var query = uri.Query.TrimStart('?');
            if (string.IsNullOrEmpty(query)) return path;

            var pairs = query.Split('&', StringSplitOptions.RemoveEmptyEntries)
                .Select(p =>
                {
                    var idx = p.IndexOf('=');
                    return idx < 0 ? (Key: p, Value: "") : (Key: p[..idx], Value: p[(idx + 1)..]);
                })
                .OrderBy(p => p.Key, StringComparer.Ordinal)
                .Select(p => p.Value.Length > 0 ? $"{p.Key}={p.Value}" : p.Key);

            return $"{path}?{string.Join("&", pairs)}";
        }
    }
}
