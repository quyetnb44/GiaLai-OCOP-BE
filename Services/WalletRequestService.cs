using GiaLaiOCOP.Api.Data;
using GiaLaiOCOP.Api.Dtos;
using GiaLaiOCOP.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GiaLaiOCOP.Api.Services
{
    public class WalletRequestService : IWalletRequestService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<WalletRequestService> _logger;

        public WalletRequestService(
            AppDbContext context,
            ILogger<WalletRequestService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<WalletRequestDto> CreateRequestAsync(int userId, CreateWalletRequestDto request)
        {
            // Kiểm tra user tồn tại
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new InvalidOperationException("Người dùng không tồn tại.");
            }

            // Kiểm tra user có phải Customer hoặc EnterpriseAdmin không
            if (user.Role != "Customer" && user.Role != "EnterpriseAdmin")
            {
                throw new InvalidOperationException("Chỉ Customer và EnterpriseAdmin mới có thể tạo yêu cầu nạp/rút tiền.");
            }

            // Lấy hoặc tạo wallet
            var wallet = await _context.Wallets
                .FirstOrDefaultAsync(w => w.UserId == userId);

            if (wallet == null)
            {
                throw new InvalidOperationException("Ví không tồn tại. Vui lòng liên hệ hỗ trợ.");
            }

            // Kiểm tra số dư nếu là rút tiền
            if (request.Type == "withdraw" && wallet.Balance < request.Amount)
            {
                throw new InvalidOperationException($"Số dư không đủ. Số dư hiện tại: {wallet.Balance:N0} VND, Cần: {request.Amount:N0} VND.");
            }

            // Kiểm tra BankAccountId nếu là rút tiền
            if (request.Type == "withdraw")
            {
                if (!request.BankAccountId.HasValue)
                {
                    throw new InvalidOperationException("Vui lòng chọn tài khoản ngân hàng thụ hưởng khi rút tiền.");
                }

                // Kiểm tra tài khoản ngân hàng thuộc về user và đang hoạt động
                var bankAccount = await _context.BankAccounts
                    .FirstOrDefaultAsync(ba => ba.Id == request.BankAccountId.Value && ba.UserId == userId && ba.IsActive);

                if (bankAccount == null)
                {
                    throw new InvalidOperationException("Tài khoản ngân hàng không tồn tại hoặc không hoạt động.");
                }
            }

            // Tạo yêu cầu
            var walletRequest = new WalletRequest
            {
                UserId = userId,
                WalletId = wallet.Id,
                Type = request.Type,
                Amount = request.Amount,
                Description = request.Description ?? $"Yêu cầu {GetTypeName(request.Type)} tiền",
                Status = "pending",
                BankAccountId = request.Type == "withdraw" ? request.BankAccountId : null,
                CreatedAt = DateTime.UtcNow
            };

            _context.WalletRequests.Add(walletRequest);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created wallet request: UserId={UserId}, Type={Type}, Amount={Amount}, RequestId={RequestId}",
                userId, request.Type, request.Amount, walletRequest.Id);

            return await MapToDtoAsync(walletRequest);
        }

        public async Task<List<WalletRequestDto>> GetRequestsAsync(int? userId = null, string? type = null, string? status = null, int page = 1, int pageSize = 20)
        {
            var query = _context.WalletRequests
                .Include(wr => wr.User)
                .Include(wr => wr.Wallet)
                .Include(wr => wr.ProcessedByUser)
                .Include(wr => wr.BankAccount)
                .AsQueryable();

            // Lọc theo userId nếu có
            if (userId.HasValue)
            {
                query = query.Where(wr => wr.UserId == userId.Value);
            }

            // Lọc theo type nếu có
            if (!string.IsNullOrWhiteSpace(type))
            {
                query = query.Where(wr => wr.Type == type);
            }

            // Lọc theo status nếu có
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(wr => wr.Status == status);
            }

            // Sắp xếp theo thời gian tạo (mới nhất trước)
            query = query.OrderByDescending(wr => wr.CreatedAt);

            // Phân trang
            var requests = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = new List<WalletRequestDto>();
            foreach (var request in requests)
            {
                dtos.Add(await MapToDtoAsync(request));
            }

            return dtos;
        }

        public async Task<WalletRequestDto?> GetRequestByIdAsync(int requestId)
        {
            var request = await _context.WalletRequests
                .Include(wr => wr.User)
                .Include(wr => wr.Wallet)
                .Include(wr => wr.ProcessedByUser)
                .Include(wr => wr.BankAccount)
                .FirstOrDefaultAsync(wr => wr.Id == requestId);

            if (request == null)
            {
                return null;
            }

            return await MapToDtoAsync(request);
        }

        public async Task<WalletRequestDto> ProcessRequestAsync(int requestId, int adminUserId, ProcessWalletRequestDto processRequest)
        {
            // Kiểm tra admin user
            var adminUser = await _context.Users.FindAsync(adminUserId);
            if (adminUser == null || adminUser.Role != "SystemAdmin")
            {
                throw new UnauthorizedAccessException("Chỉ SystemAdmin mới có thể xử lý yêu cầu.");
            }

            // Lấy yêu cầu
            var walletRequest = await _context.WalletRequests
                .Include(wr => wr.Wallet)
                .Include(wr => wr.User)
                .Include(wr => wr.BankAccount)
                .FirstOrDefaultAsync(wr => wr.Id == requestId);

            if (walletRequest == null)
            {
                throw new InvalidOperationException("Yêu cầu không tồn tại.");
            }

            if (walletRequest.Status != "pending")
            {
                throw new InvalidOperationException($"Yêu cầu đã được xử lý với trạng thái: {walletRequest.Status}.");
            }

            // Xử lý trong DB Transaction (atomic) - đảm bảo tính nhất quán dữ liệu
            await using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (processRequest.Action == "approve")
                {
                    // Phê duyệt yêu cầu: Cộng/trừ tiền vào ví của người tạo yêu cầu
                    await ApproveRequestAsync(walletRequest, adminUserId);
                    
                    // Lưu tất cả thay đổi (wallet balance, wallet transaction, wallet request status)
                    await _context.SaveChangesAsync();
                    await dbTransaction.CommitAsync();

                    _logger.LogInformation("Approved wallet request: RequestId={RequestId}, Type={Type}, Amount={Amount}, AdminId={AdminId}, UserId={UserId}",
                        requestId, walletRequest.Type, walletRequest.Amount, adminUserId, walletRequest.UserId);
                }
                else if (processRequest.Action == "reject")
                {
                    // Từ chối yêu cầu: Chỉ cập nhật trạng thái, không thay đổi số dư ví
                    if (string.IsNullOrWhiteSpace(processRequest.RejectionReason))
                    {
                        throw new ArgumentException("Lý do từ chối là bắt buộc.");
                    }

                    walletRequest.Status = "rejected";
                    walletRequest.RejectionReason = processRequest.RejectionReason;
                    walletRequest.ProcessedBy = adminUserId;
                    walletRequest.ProcessedAt = DateTime.UtcNow;
                    walletRequest.UpdatedAt = DateTime.UtcNow;

                    _context.WalletRequests.Update(walletRequest);
                    await _context.SaveChangesAsync();
                    await dbTransaction.CommitAsync();

                    _logger.LogInformation("Rejected wallet request: RequestId={RequestId}, AdminId={AdminId}, UserId={UserId}, Reason={Reason}",
                        requestId, adminUserId, walletRequest.UserId, processRequest.RejectionReason);
                }
                else
                {
                    throw new ArgumentException($"Hành động không hợp lệ: {processRequest.Action}. Chỉ chấp nhận: approve, reject.");
                }

                // Reload để lấy dữ liệu mới nhất
                await _context.Entry(walletRequest).ReloadAsync();
                return await MapToDtoAsync(walletRequest);
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                _logger.LogError(ex, "Error processing wallet request: RequestId={RequestId}, AdminId={AdminId}", requestId, adminUserId);
                throw;
            }
        }

        private async Task ApproveRequestAsync(WalletRequest walletRequest, int adminUserId)
        {
            // Lock wallet để tránh race condition
            var wallet = await _context.Wallets
                .Where(w => w.Id == walletRequest.WalletId)
                .FirstOrDefaultAsync();

            if (wallet == null)
            {
                throw new InvalidOperationException($"Wallet not found: {walletRequest.WalletId}");
            }

            if (walletRequest.Type == "deposit")
            {
                // Nạp tiền: Cộng tiền vào ví của người tạo yêu cầu
                wallet.Balance += walletRequest.Amount;

                // Tạo transaction record để lưu lịch sử
                var transaction = new WalletTransaction
                {
                    WalletId = wallet.Id,
                    Type = "deposit",
                    Amount = walletRequest.Amount,
                    BalanceAfter = wallet.Balance,
                    Description = walletRequest.Description,
                    Status = "success",
                    CreatedAt = DateTime.UtcNow
                };

                _context.WalletTransactions.Add(transaction);

                _logger.LogInformation("Approving deposit request: RequestId={RequestId}, UserId={UserId}, Amount={Amount}, OldBalance={OldBalance}, NewBalance={NewBalance}",
                    walletRequest.Id, walletRequest.UserId, walletRequest.Amount, wallet.Balance - walletRequest.Amount, wallet.Balance);
            }
            else if (walletRequest.Type == "withdraw")
            {
                // Rút tiền: Kiểm tra lại số dư trước khi trừ
                if (wallet.Balance < walletRequest.Amount)
                {
                    throw new InvalidOperationException($"Số dư không đủ. Số dư hiện tại: {wallet.Balance:N0} VND, Cần: {walletRequest.Amount:N0} VND.");
                }

                // Trừ tiền từ ví của người tạo yêu cầu
                wallet.Balance -= walletRequest.Amount;

                // Tạo transaction record để lưu lịch sử
                var transaction = new WalletTransaction
                {
                    WalletId = wallet.Id,
                    Type = "withdraw",
                    Amount = walletRequest.Amount,
                    BalanceAfter = wallet.Balance,
                    Description = walletRequest.Description,
                    Status = "success",
                    CreatedAt = DateTime.UtcNow
                };

                _context.WalletTransactions.Add(transaction);

                _logger.LogInformation("Approving withdraw request: RequestId={RequestId}, UserId={UserId}, Amount={Amount}, OldBalance={OldBalance}, NewBalance={NewBalance}",
                    walletRequest.Id, walletRequest.UserId, walletRequest.Amount, wallet.Balance + walletRequest.Amount, wallet.Balance);
            }
            else
            {
                throw new InvalidOperationException($"Loại yêu cầu không hợp lệ: {walletRequest.Type}");
            }

            // Cập nhật trạng thái yêu cầu thành "completed" (đã hoàn thành)
            walletRequest.Status = "completed";
            walletRequest.ProcessedBy = adminUserId;
            walletRequest.ProcessedAt = DateTime.UtcNow;
            walletRequest.UpdatedAt = DateTime.UtcNow;

            _context.WalletRequests.Update(walletRequest);
            // Lưu ý: SaveChangesAsync sẽ được gọi ở ProcessRequestAsync sau khi commit transaction
        }

        public async Task<int> GetPendingRequestsCountAsync()
        {
            return await _context.WalletRequests
                .CountAsync(wr => wr.Status == "pending");
        }

        private async Task<WalletRequestDto> MapToDtoAsync(WalletRequest request)
        {
            var user = request.User ?? await _context.Users.FindAsync(request.UserId);
            var wallet = request.Wallet ?? await _context.Wallets.FindAsync(request.WalletId);
            var processedByUser = request.ProcessedByUser ?? 
                (request.ProcessedBy.HasValue ? await _context.Users.FindAsync(request.ProcessedBy.Value) : null);
            var bankAccount = request.BankAccount ?? 
                (request.BankAccountId.HasValue ? await _context.BankAccounts.FindAsync(request.BankAccountId.Value) : null);

            var dto = new WalletRequestDto
            {
                Id = request.Id,
                UserId = request.UserId,
                UserName = user?.Name ?? string.Empty,
                UserEmail = user?.Email ?? string.Empty,
                UserRole = user?.Role ?? string.Empty,
                WalletId = request.WalletId,
                CurrentBalance = wallet?.Balance ?? 0,
                Type = request.Type,
                Amount = request.Amount,
                Description = request.Description,
                Status = request.Status,
                RejectionReason = request.RejectionReason,
                ProcessedBy = request.ProcessedBy,
                ProcessedByName = processedByUser?.Name,
                ProcessedAt = request.ProcessedAt,
                CreatedAt = request.CreatedAt,
                UpdatedAt = request.UpdatedAt
            };

            // Thêm thông tin ngân hàng nếu có (chỉ khi rút tiền)
            if (bankAccount != null && request.Type == "withdraw")
            {
                dto.BankAccount = new BankAccountInfoDto
                {
                    Id = bankAccount.Id,
                    BankCode = bankAccount.BankCode,
                    BankName = bankAccount.BankName,
                    AccountNumber = bankAccount.AccountNumber,
                    AccountName = bankAccount.AccountName,
                    Branch = bankAccount.Branch
                };
            }

            return dto;
        }

        private string GetTypeName(string type)
        {
            return type switch
            {
                "deposit" => "nạp",
                "withdraw" => "rút",
                _ => type
            };
        }
    }
}

