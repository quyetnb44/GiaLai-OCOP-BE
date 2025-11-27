using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Net.Http;
using GiaLaiOCOP.Api.Dtos;
using Microsoft.Extensions.Logging;

namespace GiaLaiOCOP.Api.Services
{
    public class GpsAddressService : IGpsAddressService
    {
        private const string NominatimEndpoint = "https://nominatim.openstreetmap.org/reverse";
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<GpsAddressService> _logger;

        public GpsAddressService(IHttpClientFactory httpClientFactory, ILogger<GpsAddressService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<GpsAddressLookupDto?> GetAddressFromGpsAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
        {
            var client = _httpClientFactory.CreateClient();
            var url = $"{NominatimEndpoint}?format=jsonv2&lat={latitude.ToString(CultureInfo.InvariantCulture)}&lon={longitude.ToString(CultureInfo.InvariantCulture)}&addressdetails=1&accept-language=vi";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.ParseAdd("GiaLaiOCOP/1.0 (+https://github.com/)");
            request.Headers.Accept.ParseAdd("application/json");

            using var response = await client.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Reverse geocoding thất bại với mã {StatusCode}", response.StatusCode);
                throw new InvalidOperationException("Không thể truy vấn dịch vụ bản đồ.");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = document.RootElement;

            if (!root.TryGetProperty("address", out var addressElement))
            {
                return null;
            }

            var addressLine = BuildAddressLine(root, addressElement);
            var ward = GetAddressValue(addressElement, "suburb", "village", "town", "city_district", "municipality");
            var district = GetAddressValue(addressElement, "county", "district", "state_district");
            var province = GetAddressValue(addressElement, "state", "region");
            var country = GetAddressValue(addressElement, "country");

            if (string.IsNullOrWhiteSpace(addressLine) &&
                string.IsNullOrWhiteSpace(ward) &&
                string.IsNullOrWhiteSpace(district) &&
                string.IsNullOrWhiteSpace(province) &&
                string.IsNullOrWhiteSpace(country))
            {
                return null;
            }

            return new GpsAddressLookupDto
            {
                AddressLine = addressLine,
                Ward = ward,
                District = district,
                Province = province,
                Country = country,
                Latitude = latitude,
                Longitude = longitude
            };
        }

        private static string BuildAddressLine(JsonElement root, JsonElement addressElement)
        {
            var parts = new[]
            {
                GetAddressValue(addressElement, "house_number"),
                GetAddressValue(addressElement, "road"),
                GetAddressValue(addressElement, "residential"),
                GetAddressValue(addressElement, "hamlet")
            }.Where(part => !string.IsNullOrWhiteSpace(part));

            var addressLine = string.Join(", ", parts);

            if (string.IsNullOrWhiteSpace(addressLine) &&
                root.TryGetProperty("display_name", out var displayNameElement))
            {
                addressLine = displayNameElement.GetString() ?? string.Empty;
            }

            return addressLine;
        }

        private static string GetAddressValue(JsonElement addressElement, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (addressElement.TryGetProperty(key, out var valueElement))
                {
                    var value = valueElement.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        return value.Trim();
                    }
                }
            }

            return string.Empty;
        }
    }
}

