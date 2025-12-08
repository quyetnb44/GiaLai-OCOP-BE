using GiaLaiOCOP.Api.Data;
using GiaLaiOCOP.Api.Dtos;
using GiaLaiOCOP.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GiaLaiOCOP.Api.Services
{
    public class WalletService : IWalletService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<WalletService> _logger;
        private readonly IVietQRPaymentService _vietQRPaymentService;

        public WalletService(
            AppDbContext context,
            ILogger<WalletService> logger,
            IVietQRPaymentService vietQRPaymentService)
        {
            _context = context;
            _logger = logger;
            _vietQRPaymentService = vietQRPaymentService;
        }

        public async Task<Wallet> GetOrCreateWalletAsync(int userId)
        {
            var wallet = await _context.Wallets
                .FirstOrDefaultAsync(w => w.UserId == userId);

            if (wallet == null)
            {
                wallet = new Wallet
                {
                    UserId = userId,
                    Balance = 0,
                    Currency = "VND",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Wallets.Add(wallet);
                await _context.SaveChangesAsync();
            }

            return wallet;
        }

        public async Task<WalletDto> GetWalletAsync(int userId)
        {
            var wallet = await GetOrCreateWalletAsync(userId);

            return new WalletDto
            {
                Id = wallet.Id,
                UserId = wallet.UserId,
                Balance = wallet.Balance,
                Currency = wallet.Currency,
                CreatedAt = wallet.CreatedAt
            };
        }

        public async Task<List<WalletTransactionDto>> GetTransactionsAsync(int userId, int page = 1, int pageSize = 20)
        {
            var wallet = await GetOrCreateWalletAsync(userId);

            var transactions = await _context.WalletTransactions
                .Where(wt => wt.WalletId == wallet.Id)
                .OrderByDescending(wt => wt.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(wt => new WalletTransactionDto
                {
                    Id = wt.Id,
                    WalletId = wt.WalletId,
                    Type = wt.Type,
                    Amount = wt.Amount,
                    BalanceAfter = wt.BalanceAfter,
                    Description = wt.Description,
                    Status = wt.Status,
                    CreatedAt = wt.CreatedAt,
                    OrderId = wt.OrderId,
                    PaymentGatewayTransactionId = wt.PaymentGatewayTransactionId,
                    PaymentGateway = wt.PaymentGateway
                })
                .ToListAsync();

            return transactions;
        }

        public async Task<DepositResponseDto> CreateDepositAsync(int userId, DepositRequestDto request)
        {
            var wallet = await GetOrCreateWalletAsync(userId);

            // Tạo transaction pending
            var transaction = new WalletTransaction
            {
                WalletId = wallet.Id,
                Type = "deposit",
                Amount = request.Amount,
                BalanceAfter = wallet.Balance, // Chưa cộng tiền, giữ nguyên balance
                Description = request.Description ?? "Nạp tiền qua VietQR",
                Status = "pending",
                CreatedAt = DateTime.UtcNow,
                PaymentGateway = "vietqr"
            };

            _context.WalletTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            // Tạo QR code URL từ VietQR
            var reference = $"BT-{DateTime.UtcNow:yyyyMMddHHmmss}-{transaction.Id}";
            var description = transaction.Description ?? "Nạp tiền vào ví";
            var qrCodeUrl = _vietQRPaymentService.GeneratePaymentQrCodeUrl(
                request.Amount,
                description,
                reference
            );

            // Cập nhật transaction với reference
            transaction.PaymentGatewayTransactionId = reference;
            await _context.SaveChangesAsync();

            return new DepositResponseDto
            {
                PaymentUrl = qrCodeUrl,
                TransactionId = transaction.Id.ToString(),
                Amount = request.Amount,
                PaymentGateway = "vietqr",
                Description = description,
                Reference = reference
            };
        }

        public async Task<WalletTransactionDto> PayOrderAsync(int userId, PayOrderRequestDto request)
        {
            var wallet = await GetOrCreateWalletAsync(userId);

            // Kiểm tra đơn hàng tồn tại và thuộc về user
            var order = await _context.Orders
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.Id == request.OrderId && o.UserId == userId);

            if (order == null)
            {
                throw new InvalidOperationException("Đơn hàng không tồn tại hoặc không thuộc về bạn.");
            }

            // Tính tổng số tiền cần thanh toán
            var totalAmount = order.Payments
                .Where(p => p.Status != "Paid" && p.Status != "Cancelled")
                .Sum(p => p.Amount);

            if (totalAmount <= 0)
            {
                throw new InvalidOperationException("Đơn hàng đã được thanh toán hoặc không có khoản thanh toán nào.");
            }

            // Kiểm tra số dư
            if (wallet.Balance < totalAmount)
            {
                throw new InvalidOperationException($"Số dư không đủ. Số dư hiện tại: {wallet.Balance:N0} VND, Cần: {totalAmount:N0} VND.");
            }

            // Thực hiện thanh toán trong DB Transaction (atomic)
            await using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Lock wallet để tránh race condition
                var lockedWallet = await _context.Wallets
                    .Where(w => w.Id == wallet.Id)
                    .FirstOrDefaultAsync();

                if (lockedWallet == null)
                {
                    throw new InvalidOperationException($"Wallet not found: {wallet.Id}");
                }

                // Kiểm tra lại số dư sau khi lock
                if (lockedWallet.Balance < totalAmount)
                {
                    throw new InvalidOperationException($"Số dư không đủ. Số dư hiện tại: {lockedWallet.Balance:N0} VND, Cần: {totalAmount:N0} VND.");
                }

                // Trừ tiền từ ví
                lockedWallet.Balance -= totalAmount;

                // Tạo transaction record
                var transaction = new WalletTransaction
                {
                    WalletId = lockedWallet.Id,
                    Type = "payment",
                    Amount = totalAmount,
                    BalanceAfter = lockedWallet.Balance,
                    Description = request.Description ?? $"Thanh toán đơn hàng #{request.OrderId}",
                    Status = "success",
                    CreatedAt = DateTime.UtcNow,
                    OrderId = request.OrderId
                };

                _context.WalletTransactions.Add(transaction);

                // Cập nhật trạng thái thanh toán của đơn hàng
                foreach (var payment in order.Payments.Where(p => p.Status != "Paid" && p.Status != "Cancelled"))
                {
                    payment.Status = "Paid";
                    payment.PaidAt = DateTime.UtcNow;
                }

                order.PaymentStatus = "Paid";

                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                _logger.LogInformation("Payment successful: User {UserId}, Order {OrderId}, Amount: {Amount}, New Balance: {Balance}", 
                    userId, request.OrderId, totalAmount, lockedWallet.Balance);

                return new WalletTransactionDto
                {
                    Id = transaction.Id,
                    WalletId = transaction.WalletId,
                    Type = transaction.Type,
                    Amount = transaction.Amount,
                    BalanceAfter = transaction.BalanceAfter,
                    Description = transaction.Description,
                    Status = transaction.Status,
                    CreatedAt = transaction.CreatedAt,
                    OrderId = transaction.OrderId
                };
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                _logger.LogError(ex, "Error processing payment for order: {OrderId}", request.OrderId);
                throw;
            }
        }

        public async Task<WalletTransactionDto> RefundAsync(int userId, RefundRequestDto request)
        {
            var wallet = await GetOrCreateWalletAsync(userId);

            // Kiểm tra đơn hàng tồn tại và thuộc về user
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == request.OrderId && o.UserId == userId);

            if (order == null)
            {
                throw new InvalidOperationException("Đơn hàng không tồn tại hoặc không thuộc về bạn.");
            }

            // Kiểm tra số tiền hoàn hợp lệ
            if (request.Amount <= 0)
            {
                throw new InvalidOperationException("Số tiền hoàn phải lớn hơn 0.");
            }

            // Thực hiện hoàn tiền trong DB Transaction (atomic)
            await using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Lock wallet để tránh race condition
                var lockedWallet = await _context.Wallets
                    .Where(w => w.Id == wallet.Id)
                    .FirstOrDefaultAsync();

                if (lockedWallet == null)
                {
                    throw new InvalidOperationException($"Wallet not found: {wallet.Id}");
                }

                // Cộng tiền vào ví
                lockedWallet.Balance += request.Amount;

                // Tạo transaction record
                var transaction = new WalletTransaction
                {
                    WalletId = lockedWallet.Id,
                    Type = "refund",
                    Amount = request.Amount,
                    BalanceAfter = lockedWallet.Balance,
                    Description = request.Description ?? $"Hoàn tiền đơn hàng #{request.OrderId}",
                    Status = "success",
                    CreatedAt = DateTime.UtcNow,
                    OrderId = request.OrderId
                };

                _context.WalletTransactions.Add(transaction);

                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                _logger.LogInformation("Refund successful: User {UserId}, Order {OrderId}, Amount: {Amount}, New Balance: {Balance}", 
                    userId, request.OrderId, request.Amount, lockedWallet.Balance);

                return new WalletTransactionDto
                {
                    Id = transaction.Id,
                    WalletId = transaction.WalletId,
                    Type = transaction.Type,
                    Amount = transaction.Amount,
                    BalanceAfter = transaction.BalanceAfter,
                    Description = transaction.Description,
                    Status = transaction.Status,
                    CreatedAt = transaction.CreatedAt,
                    OrderId = transaction.OrderId
                };
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                _logger.LogError(ex, "Error processing refund for order: {OrderId}", request.OrderId);
                throw;
            }
        }

        public async Task<WalletTransactionDto> WithdrawAsync(int userId, WithdrawRequestDto request)
        {
            var wallet = await GetOrCreateWalletAsync(userId);

            // Kiểm tra số dư
            if (wallet.Balance < request.Amount)
            {
                throw new InvalidOperationException($"Số dư không đủ. Số dư hiện tại: {wallet.Balance:N0} VND, Cần: {request.Amount:N0} VND.");
            }

            // Thực hiện rút tiền trong DB Transaction (atomic)
            await using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Lock wallet để tránh race condition
                var lockedWallet = await _context.Wallets
                    .Where(w => w.Id == wallet.Id)
                    .FirstOrDefaultAsync();

                if (lockedWallet == null)
                {
                    throw new InvalidOperationException($"Wallet not found: {wallet.Id}");
                }

                // Kiểm tra lại số dư sau khi lock
                if (lockedWallet.Balance < request.Amount)
                {
                    throw new InvalidOperationException($"Số dư không đủ. Số dư hiện tại: {lockedWallet.Balance:N0} VND, Cần: {request.Amount:N0} VND.");
                }

                // Trừ tiền từ ví
                lockedWallet.Balance -= request.Amount;

                // Tạo transaction record
                var transaction = new WalletTransaction
                {
                    WalletId = lockedWallet.Id,
                    Type = "withdraw",
                    Amount = request.Amount,
                    BalanceAfter = lockedWallet.Balance,
                    Description = request.Description ?? "Rút tiền từ ví",
                    Status = "success",
                    CreatedAt = DateTime.UtcNow
                };

                _context.WalletTransactions.Add(transaction);

                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                _logger.LogInformation("Withdraw successful: User {UserId}, Amount: {Amount}, New Balance: {Balance}", 
                    userId, request.Amount, lockedWallet.Balance);

                return new WalletTransactionDto
                {
                    Id = transaction.Id,
                    WalletId = transaction.WalletId,
                    Type = transaction.Type,
                    Amount = transaction.Amount,
                    BalanceAfter = transaction.BalanceAfter,
                    Description = transaction.Description,
                    Status = transaction.Status,
                    CreatedAt = transaction.CreatedAt
                };
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                _logger.LogError(ex, "Error processing withdraw for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<SystemWalletSummaryDto> GetSystemWalletSummaryAsync()
        {
            // Lấy ví của SystemAdmin (tự động tạo nếu chưa có)
            var systemAdmin = await _context.Users
                .FirstOrDefaultAsync(u => u.Role == "SystemAdmin");

            decimal systemAdminBalance = 0;
            if (systemAdmin != null)
            {
                var systemAdminWallet = await GetOrCreateWalletAsync(systemAdmin.Id);
                systemAdminBalance = systemAdminWallet.Balance;
            }

            // Tổng hợp số tiền của tất cả Customer
            var customersBalance = await _context.Wallets
                .Join(_context.Users,
                    wallet => wallet.UserId,
                    user => user.Id,
                    (wallet, user) => new { wallet, user })
                .Where(x => x.user.Role == "Customer")
                .SumAsync(x => x.wallet.Balance);

            // Tổng hợp số tiền của tất cả EnterpriseAdmin
            var enterpriseAdminsBalance = await _context.Wallets
                .Join(_context.Users,
                    wallet => wallet.UserId,
                    user => user.Id,
                    (wallet, user) => new { wallet, user })
                .Where(x => x.user.Role == "EnterpriseAdmin")
                .SumAsync(x => x.wallet.Balance);

            // Tổng số tiền của tất cả User (Customer + EnterpriseAdmin)
            var allUsersBalance = customersBalance + enterpriseAdminsBalance;

            // Tổng số tiền trong hệ thống (SystemAdmin + tất cả User)
            var totalSystemBalance = systemAdminBalance + allUsersBalance;

            // Đếm số lượng
            var totalCustomers = await _context.Users
                .CountAsync(u => u.Role == "Customer" && _context.Wallets.Any(w => w.UserId == u.Id));

            var totalEnterpriseAdmins = await _context.Users
                .CountAsync(u => u.Role == "EnterpriseAdmin" && _context.Wallets.Any(w => w.UserId == u.Id));

            var totalUsers = totalCustomers + totalEnterpriseAdmins;

            return new SystemWalletSummaryDto
            {
                TotalSystemBalance = totalSystemBalance,
                SystemAdminBalance = systemAdminBalance,
                AllUsersBalance = allUsersBalance,
                TotalUsers = totalUsers,
                TotalCustomers = totalCustomers,
                TotalEnterpriseAdmins = totalEnterpriseAdmins,
                Breakdown = new SystemWalletBreakdownDto
                {
                    CustomersBalance = customersBalance,
                    EnterpriseAdminsBalance = enterpriseAdminsBalance
                }
            };
        }

        public async Task<List<UserWalletSummaryDto>> GetAllUserWalletsAsync(int page = 1, int pageSize = 50)
        {
            // Load wallets với user info
            var walletsData = await _context.Wallets
                .Join(_context.Users,
                    wallet => wallet.UserId,
                    user => user.Id,
                    (wallet, user) => new { wallet, user })
                .Where(x => x.user.Role == "Customer" || x.user.Role == "EnterpriseAdmin")
                .OrderByDescending(x => x.wallet.Balance)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Load transaction counts cho các wallet
            var walletIds = walletsData.Select(x => x.wallet.Id).ToList();
            var transactionCounts = await _context.WalletTransactions
                .Where(wt => walletIds.Contains(wt.WalletId))
                .GroupBy(wt => wt.WalletId)
                .Select(g => new { WalletId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.WalletId, x => x.Count);

            // Map sang DTOs
            var wallets = walletsData.Select(x => new UserWalletSummaryDto
            {
                UserId = x.user.Id,
                UserName = x.user.Name,
                UserEmail = x.user.Email,
                UserRole = x.user.Role,
                WalletId = x.wallet.Id,
                Balance = x.wallet.Balance,
                Currency = x.wallet.Currency,
                WalletCreatedAt = x.wallet.CreatedAt,
                TotalTransactions = transactionCounts.GetValueOrDefault(x.wallet.Id, 0)
            }).ToList();

            return wallets;
        }

        public async Task<int> EnsureAllUsersHaveWalletsAsync()
        {
            // Lấy tất cả user chưa có ví
            var usersWithoutWallets = await _context.Users
                .Where(u => !_context.Wallets.Any(w => w.UserId == u.Id))
                .ToListAsync();

            if (usersWithoutWallets.Count == 0)
            {
                return 0;
            }

            // Tạo ví cho tất cả user chưa có ví
            var wallets = usersWithoutWallets.Select(user => new Wallet
            {
                UserId = user.Id,
                Balance = 0,
                Currency = "VND",
                CreatedAt = DateTime.UtcNow
            }).ToList();

            _context.Wallets.AddRange(wallets);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created wallets for {Count} users who didn't have wallets", wallets.Count);

            return wallets.Count;
        }

        public async Task<WalletDto> GetUserWalletAsync(int userId)
        {
            // Kiểm tra user tồn tại
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new InvalidOperationException("User không tồn tại.");
            }

            // Lấy hoặc tạo ví
            var wallet = await GetOrCreateWalletAsync(userId);

            return new WalletDto
            {
                Id = wallet.Id,
                UserId = wallet.UserId,
                Balance = wallet.Balance,
                Currency = wallet.Currency,
                CreatedAt = wallet.CreatedAt
            };
        }

        public async Task<WalletTransactionDto> UpdateUserWalletBalanceAsync(int userId, decimal amount, string description, int adminUserId)
        {
            // Kiểm tra user tồn tại
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new InvalidOperationException("User không tồn tại.");
            }

            // Lấy hoặc tạo ví
            var wallet = await GetOrCreateWalletAsync(userId);

            // Kiểm tra số dư sau khi trừ (nếu amount < 0)
            if (amount < 0 && wallet.Balance + amount < 0)
            {
                throw new InvalidOperationException($"Không thể trừ số tiền này. Số dư hiện tại: {wallet.Balance:N0} VND, Số tiền trừ: {Math.Abs(amount):N0} VND.");
            }

            // Thực hiện cập nhật trong DB Transaction (atomic)
            await using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Lock wallet để tránh race condition
                var lockedWallet = await _context.Wallets
                    .Where(w => w.Id == wallet.Id)
                    .FirstOrDefaultAsync();

                if (lockedWallet == null)
                {
                    throw new InvalidOperationException($"Wallet not found: {wallet.Id}");
                }

                // Kiểm tra lại số dư sau khi lock (nếu trừ tiền)
                if (amount < 0 && lockedWallet.Balance + amount < 0)
                {
                    throw new InvalidOperationException($"Không thể trừ số tiền này. Số dư hiện tại: {lockedWallet.Balance:N0} VND, Số tiền trừ: {Math.Abs(amount):N0} VND.");
                }

                // Cập nhật số dư
                lockedWallet.Balance += amount;

                // Xác định loại giao dịch
                string transactionType = amount >= 0 ? "deposit" : "withdraw";

                // Tạo transaction record để lưu lịch sử
                var transaction = new WalletTransaction
                {
                    WalletId = lockedWallet.Id,
                    Type = transactionType,
                    Amount = Math.Abs(amount),
                    BalanceAfter = lockedWallet.Balance,
                    Description = $"[SystemAdmin] {description}",
                    Status = "success",
                    CreatedAt = DateTime.UtcNow,
                    PaymentGateway = "admin" // Đánh dấu là do SystemAdmin thực hiện
                };

                _context.WalletTransactions.Add(transaction);
                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                _logger.LogInformation("SystemAdmin updated wallet balance: AdminId={AdminId}, UserId={UserId}, Amount={Amount}, OldBalance={OldBalance}, NewBalance={NewBalance}, Description={Description}",
                    adminUserId, userId, amount, lockedWallet.Balance - amount, lockedWallet.Balance, description);

                return new WalletTransactionDto
                {
                    Id = transaction.Id,
                    WalletId = transaction.WalletId,
                    Type = transaction.Type,
                    Amount = transaction.Amount,
                    BalanceAfter = transaction.BalanceAfter,
                    Description = transaction.Description,
                    Status = transaction.Status,
                    CreatedAt = transaction.CreatedAt,
                    PaymentGateway = transaction.PaymentGateway
                };
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                _logger.LogError(ex, "Error updating wallet balance: UserId={UserId}, Amount={Amount}", userId, amount);
                throw;
            }
        }
    }
}

