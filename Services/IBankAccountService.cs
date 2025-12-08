using GiaLaiOCOP.Api.Dtos;
using GiaLaiOCOP.Api.Models;

namespace GiaLaiOCOP.Api.Services
{
    public interface IBankAccountService
    {
        Task<BankAccountDto> CreateBankAccountAsync(int userId, CreateBankAccountDto dto);
        Task<BankAccountDto?> GetBankAccountByIdAsync(int bankAccountId, int userId);
        Task<List<BankAccountDto>> GetUserBankAccountsAsync(int userId);
        Task<BankAccountDto?> UpdateBankAccountAsync(int bankAccountId, int userId, UpdateBankAccountDto dto);
        Task<bool> DeleteBankAccountAsync(int bankAccountId, int userId);
        Task<BankAccountDto?> SetDefaultBankAccountAsync(int bankAccountId, int userId);
        Task<BankAccountDto?> GetDefaultBankAccountAsync(int userId);
        Task<BankAccountDto?> GetBankAccountByIdForAdminAsync(int bankAccountId); // SystemAdmin có thể xem bất kỳ bankAccount nào
    }
}

