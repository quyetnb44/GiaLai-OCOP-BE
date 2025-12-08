using GiaLaiOCOP.Api.Dtos;
using GiaLaiOCOP.Api.Models;

namespace GiaLaiOCOP.Api.Services
{
    public interface IWalletService
    {
        Task<Wallet> GetOrCreateWalletAsync(int userId);
        Task<WalletDto> GetWalletAsync(int userId);
        Task<List<WalletTransactionDto>> GetTransactionsAsync(int userId, int page = 1, int pageSize = 20);
        Task<DepositResponseDto> CreateDepositAsync(int userId, DepositRequestDto request);
        Task<WalletTransactionDto> PayOrderAsync(int userId, PayOrderRequestDto request);
        Task<WalletTransactionDto> RefundAsync(int userId, RefundRequestDto request);
        Task<WalletTransactionDto> WithdrawAsync(int userId, WithdrawRequestDto request);
        
        // SystemAdmin: Tổng hợp số tiền hệ thống
        Task<SystemWalletSummaryDto> GetSystemWalletSummaryAsync();
        Task<List<UserWalletSummaryDto>> GetAllUserWalletsAsync(int page = 1, int pageSize = 50);
        Task<int> EnsureAllUsersHaveWalletsAsync(); // Tạo ví cho tất cả user chưa có ví
        
        // SystemAdmin: Quản lý ví của user
        Task<WalletDto> GetUserWalletAsync(int userId); // Xem ví của user cụ thể
        Task<WalletTransactionDto> UpdateUserWalletBalanceAsync(int userId, decimal amount, string description, int adminUserId); // Cập nhật số dư ví
    }
}
