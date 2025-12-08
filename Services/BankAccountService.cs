using GiaLaiOCOP.Api.Data;
using GiaLaiOCOP.Api.Dtos;
using GiaLaiOCOP.Api.Models;
using GiaLaiOCOP.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace GiaLaiOCOP.Api.Services
{
    public class BankAccountService : IBankAccountService
    {
        private readonly AppDbContext _context;
        private readonly IVietQRPaymentService _vietQRService;

        public BankAccountService(AppDbContext context, IVietQRPaymentService vietQRService)
        {
            _context = context;
            _vietQRService = vietQRService;
        }

        public async Task<BankAccountDto> CreateBankAccountAsync(int userId, CreateBankAccountDto dto)
        {
            // Kiểm tra user tồn tại
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new Exception("Người dùng không tồn tại.");
            }

            // Nếu đặt làm mặc định, bỏ mặc định của các tài khoản khác
            if (dto.IsDefault)
            {
                var existingDefault = await _context.BankAccounts
                    .Where(ba => ba.UserId == userId && ba.IsDefault && ba.IsActive)
                    .FirstOrDefaultAsync();

                if (existingDefault != null)
                {
                    existingDefault.IsDefault = false;
                    existingDefault.UpdatedAt = DateTime.UtcNow;
                }
            }

            var bankAccount = new BankAccount
            {
                UserId = userId,
                BankCode = dto.BankCode,
                BankName = dto.BankName,
                AccountNumber = dto.AccountNumber,
                AccountName = dto.AccountName,
                Branch = dto.Branch,
                IsDefault = dto.IsDefault,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.BankAccounts.Add(bankAccount);
            await _context.SaveChangesAsync();

            return MapToDto(bankAccount);
        }

        public async Task<BankAccountDto?> GetBankAccountByIdAsync(int bankAccountId, int userId)
        {
            var bankAccount = await _context.BankAccounts
                .FirstOrDefaultAsync(ba => ba.Id == bankAccountId && ba.UserId == userId);

            if (bankAccount == null)
                return null;

            return MapToDto(bankAccount);
        }

        public async Task<List<BankAccountDto>> GetUserBankAccountsAsync(int userId)
        {
            var bankAccounts = await _context.BankAccounts
                .Where(ba => ba.UserId == userId)
                .OrderByDescending(ba => ba.IsDefault)
                .ThenByDescending(ba => ba.CreatedAt)
                .ToListAsync();

            return bankAccounts.Select(MapToDto).ToList();
        }

        public async Task<BankAccountDto?> UpdateBankAccountAsync(int bankAccountId, int userId, UpdateBankAccountDto dto)
        {
            var bankAccount = await _context.BankAccounts
                .FirstOrDefaultAsync(ba => ba.Id == bankAccountId && ba.UserId == userId);

            if (bankAccount == null)
                return null;

            // Cập nhật các trường
            if (!string.IsNullOrWhiteSpace(dto.BankCode))
                bankAccount.BankCode = dto.BankCode;

            if (!string.IsNullOrWhiteSpace(dto.BankName))
                bankAccount.BankName = dto.BankName;

            if (!string.IsNullOrWhiteSpace(dto.AccountNumber))
                bankAccount.AccountNumber = dto.AccountNumber;

            if (!string.IsNullOrWhiteSpace(dto.AccountName))
                bankAccount.AccountName = dto.AccountName;

            if (dto.Branch != null)
                bankAccount.Branch = dto.Branch;

            if (dto.IsActive.HasValue)
                bankAccount.IsActive = dto.IsActive.Value;

            // Xử lý IsDefault
            if (dto.IsDefault.HasValue && dto.IsDefault.Value && !bankAccount.IsDefault)
            {
                // Bỏ mặc định của các tài khoản khác
                var existingDefault = await _context.BankAccounts
                    .Where(ba => ba.UserId == userId && ba.Id != bankAccountId && ba.IsDefault && ba.IsActive)
                    .FirstOrDefaultAsync();

                if (existingDefault != null)
                {
                    existingDefault.IsDefault = false;
                    existingDefault.UpdatedAt = DateTime.UtcNow;
                }

                bankAccount.IsDefault = true;
            }
            else if (dto.IsDefault.HasValue && !dto.IsDefault.Value)
            {
                bankAccount.IsDefault = false;
            }

            bankAccount.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return MapToDto(bankAccount);
        }

        public async Task<bool> DeleteBankAccountAsync(int bankAccountId, int userId)
        {
            var bankAccount = await _context.BankAccounts
                .FirstOrDefaultAsync(ba => ba.Id == bankAccountId && ba.UserId == userId);

            if (bankAccount == null)
                return false;

            // Kiểm tra xem có đang được sử dụng trong WalletRequest chưa hoàn thành không
            var hasPendingRequests = await _context.WalletRequests
                .AnyAsync(wr => wr.BankAccountId == bankAccountId && 
                               (wr.Status == "pending" || wr.Status == "approved"));

            if (hasPendingRequests)
            {
                throw new Exception("Không thể xóa tài khoản ngân hàng đang được sử dụng trong yêu cầu rút tiền chưa hoàn thành.");
            }

            _context.BankAccounts.Remove(bankAccount);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<BankAccountDto?> SetDefaultBankAccountAsync(int bankAccountId, int userId)
        {
            var bankAccount = await _context.BankAccounts
                .FirstOrDefaultAsync(ba => ba.Id == bankAccountId && ba.UserId == userId);

            if (bankAccount == null || !bankAccount.IsActive)
                return null;

            // Bỏ mặc định của các tài khoản khác
            var existingDefault = await _context.BankAccounts
                .Where(ba => ba.UserId == userId && ba.Id != bankAccountId && ba.IsDefault && ba.IsActive)
                .FirstOrDefaultAsync();

            if (existingDefault != null)
            {
                existingDefault.IsDefault = false;
                existingDefault.UpdatedAt = DateTime.UtcNow;
            }

            bankAccount.IsDefault = true;
            bankAccount.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return MapToDto(bankAccount);
        }

        public async Task<BankAccountDto?> GetDefaultBankAccountAsync(int userId)
        {
            var bankAccount = await _context.BankAccounts
                .Where(ba => ba.UserId == userId && ba.IsDefault && ba.IsActive)
                .FirstOrDefaultAsync();

            if (bankAccount == null)
                return null;

            return MapToDto(bankAccount);
        }

        public async Task<BankAccountDto?> GetBankAccountByIdForAdminAsync(int bankAccountId)
        {
            var bankAccount = await _context.BankAccounts
                .FirstOrDefaultAsync(ba => ba.Id == bankAccountId);

            if (bankAccount == null)
                return null;

            return MapToDto(bankAccount);
        }

        private BankAccountDto MapToDto(BankAccount bankAccount)
        {
            var dto = new BankAccountDto
            {
                Id = bankAccount.Id,
                UserId = bankAccount.UserId,
                BankCode = bankAccount.BankCode,
                BankName = bankAccount.BankName,
                AccountNumber = bankAccount.AccountNumber,
                AccountName = bankAccount.AccountName,
                Branch = bankAccount.Branch,
                IsDefault = bankAccount.IsDefault,
                IsActive = bankAccount.IsActive,
                CreatedAt = bankAccount.CreatedAt,
                UpdatedAt = bankAccount.UpdatedAt
            };

            // Generate QR code URL
            dto.QrCodeUrl = _vietQRService.GenerateQrCodeUrlForBankAccount(
                bankAccount.BankCode,
                bankAccount.AccountNumber
            );

            return dto;
        }
    }
}

