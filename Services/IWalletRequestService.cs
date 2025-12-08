using GiaLaiOCOP.Api.Dtos;

namespace GiaLaiOCOP.Api.Services
{
    public interface IWalletRequestService
    {
        Task<WalletRequestDto> CreateRequestAsync(int userId, CreateWalletRequestDto request);
        Task<List<WalletRequestDto>> GetRequestsAsync(int? userId = null, string? type = null, string? status = null, int page = 1, int pageSize = 20);
        Task<WalletRequestDto?> GetRequestByIdAsync(int requestId);
        Task<WalletRequestDto> ProcessRequestAsync(int requestId, int adminUserId, ProcessWalletRequestDto processRequest);
        Task<int> GetPendingRequestsCountAsync();
    }
}

