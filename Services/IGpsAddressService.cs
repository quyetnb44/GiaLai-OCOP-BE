using GiaLaiOCOP.Api.Dtos;
using System.Threading;

namespace GiaLaiOCOP.Api.Services
{
    public interface IGpsAddressService
    {
        Task<GpsAddressLookupDto?> GetAddressFromGpsAsync(double latitude, double longitude, CancellationToken cancellationToken = default);
    }
}

