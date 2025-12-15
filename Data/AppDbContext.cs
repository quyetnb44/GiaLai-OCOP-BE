using Microsoft.EntityFrameworkCore;
using GiaLaiOCOP.Api.Models;

namespace GiaLaiOCOP.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        // Các bảng trong database
        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Producer> Producers { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<Enterprise> Enterprises { get; set; }
        public DbSet<EnterpriseApplication> EnterpriseApplications { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<EmailVerification> EmailVerifications { get; set; }
        public DbSet<ShippingAddress> ShippingAddresses { get; set; }
        public DbSet<Image> Images { get; set; }
        public DbSet<Province> Provinces { get; set; }
        public DbSet<District> Districts { get; set; }
        public DbSet<Ward> Wards { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<InventoryHistory> InventoryHistories { get; set; }
        public DbSet<EnterpriseSettings> EnterpriseSettings { get; set; }
        public DbSet<EnterpriseBankInfo> EnterpriseBankInfos { get; set; }
        public DbSet<Wallet> Wallets { get; set; }
        public DbSet<WalletTransaction> WalletTransactions { get; set; }
        public DbSet<WalletRequest> WalletRequests { get; set; }
        public DbSet<BankAccount> BankAccounts { get; set; }
        public DbSet<OrderEnterpriseStatus> OrderEnterpriseStatuses { get; set; }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 🟩 Cấu hình quan hệ Order - OrderItem (1-n)
            modelBuilder.Entity<Order>()
                .HasMany(o => o.OrderItems)
                .WithOne(i => i.Order)
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // 🟩 Cấu hình quan hệ User - Order (1-n)
            modelBuilder.Entity<User>()
                .HasMany(u => u.Orders)
                .WithOne(o => o.User)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // 🟩 Cấu hình khóa chính nếu cần (tùy bạn có hay không)
            modelBuilder.Entity<Order>()
                .HasKey(o => o.Id);

            modelBuilder.Entity<OrderItem>()
                .HasKey(i => i.Id);

            // 🟩 Cấu hình decimal hoặc kiểu dữ liệu nếu cần
            modelBuilder.Entity<OrderItem>()
                .Property(i => i.Price)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<OrderItem>()
                .HasOne(i => i.Product)
                .WithMany(p => p.OrderItems)
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .Property(o => o.TotalAmount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Order)
                .WithMany(o => o.Payments)
                .HasForeignKey(p => p.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Enterprise)
                .WithMany(e => e.Payments)
                .HasForeignKey(p => p.EnterpriseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            // 🟩 Cấu hình quan hệ User - ShippingAddress (1-n)
            modelBuilder.Entity<User>()
                .HasMany(u => u.ShippingAddresses)
                .WithOne(sa => sa.User)
                .HasForeignKey(sa => sa.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // 🟩 Cấu hình quan hệ Order - ShippingAddress (n-1)
            modelBuilder.Entity<Order>()
                .HasOne(o => o.ShippingAddressDetail)
                .WithMany(sa => sa.Orders)
                .HasForeignKey(o => o.ShippingAddressId)
                .OnDelete(DeleteBehavior.Restrict);

            // 🟩 Đảm bảo chỉ một địa chỉ mặc định cho mỗi user
            modelBuilder.Entity<ShippingAddress>()
                .HasIndex(sa => new { sa.UserId, sa.IsDefault })
                .IsUnique()
                .HasFilter("\"IsDefault\" = true");

            // 🟩 Cấu hình quan hệ Image - User (1-n) cho avatar
            modelBuilder.Entity<Image>()
                .HasOne(img => img.User)
                .WithMany(u => u.Images)
                .HasForeignKey(img => img.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // 🟩 Cấu hình quan hệ Image - Product (1-n) cho ảnh sản phẩm
            modelBuilder.Entity<Image>()
                .HasOne(img => img.Product)
                .WithMany(p => p.Images)
                .HasForeignKey(img => img.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // 🟩 Cấu hình quan hệ Image - Enterprise (1-n) cho ảnh doanh nghiệp
            modelBuilder.Entity<Image>()
                .HasOne(img => img.Enterprise)
                .WithMany(e => e.Images)
                .HasForeignKey(img => img.EnterpriseId)
                .OnDelete(DeleteBehavior.Cascade);

            // 🟩 Cấu hình quan hệ Image - UploadedByUser (1-n)
            modelBuilder.Entity<Image>()
                .HasOne(img => img.UploadedByUser)
                .WithMany()
                .HasForeignKey(img => img.UploadedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // 🟩 Cấu hình quan hệ Province - District (1-n)
            modelBuilder.Entity<Province>()
                .HasMany(p => p.Districts)
                .WithOne(d => d.Province)
                .HasForeignKey(d => d.ProvinceId)
                .OnDelete(DeleteBehavior.Restrict);

            // 🟩 Cấu hình quan hệ District - Ward (1-n)
            modelBuilder.Entity<District>()
                .HasMany(d => d.Wards)
                .WithOne(w => w.District)
                .HasForeignKey(w => w.DistrictId)
                .OnDelete(DeleteBehavior.Restrict);

            // 🟩 Cấu hình quan hệ User - Province/District/Ward
            modelBuilder.Entity<User>()
                .HasOne(u => u.Province)
                .WithMany()
                .HasForeignKey(u => u.ProvinceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasOne(u => u.District)
                .WithMany()
                .HasForeignKey(u => u.DistrictId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasOne(u => u.Ward)
                .WithMany()
                .HasForeignKey(u => u.WardId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EnterpriseSettings>()
                .HasOne(es => es.Enterprise)
                .WithOne(e => e.Settings)
                .HasForeignKey<EnterpriseSettings>(es => es.EnterpriseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<InventoryHistory>()
                .HasOne(ih => ih.Product)
                .WithMany(p => p.InventoryHistories)
                .HasForeignKey(ih => ih.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<InventoryHistory>()
                .HasOne(ih => ih.Enterprise)
                .WithMany(e => e.InventoryHistories!)
                .HasForeignKey(ih => ih.EnterpriseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<InventoryHistory>()
                .HasOne(ih => ih.CreatedByUser)
                .WithMany()
                .HasForeignKey(ih => ih.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Enterprise)
                .WithMany(e => e.Notifications!)
                .HasForeignKey(n => n.EnterpriseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Product)
                .WithMany(p => p.Notifications)
                .HasForeignKey(n => n.ProductId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Order)
                .WithMany()
                .HasForeignKey(n => n.OrderId)
                .OnDelete(DeleteBehavior.SetNull);

            // 🟩 Cấu hình quan hệ Enterprise - EnterpriseBankInfo (1-1)
            modelBuilder.Entity<EnterpriseBankInfo>()
                .HasOne(ebi => ebi.Enterprise)
                .WithMany()
                .HasForeignKey(ebi => ebi.EnterpriseId)
                .OnDelete(DeleteBehavior.Cascade);

            // 🟩 Đảm bảo mỗi Enterprise chỉ có một EnterpriseBankInfo
            modelBuilder.Entity<EnterpriseBankInfo>()
                .HasIndex(ebi => ebi.EnterpriseId)
                .IsUnique();

            // 🟩 Cấu hình quan hệ EmailVerification - User (n-1, optional)
            // Nullable vì OTP có thể được gửi cho email chưa đăng ký (đăng ký mới)
            modelBuilder.Entity<EmailVerification>()
                .HasOne(ev => ev.User)
                .WithMany()
                .HasForeignKey(ev => ev.UserId)
                .OnDelete(DeleteBehavior.SetNull); // Set null khi user bị xóa (không xóa OTP)

            // 🟩 Cấu hình quan hệ User - Wallet (1-1)
            modelBuilder.Entity<Wallet>()
                .HasOne(w => w.User)
                .WithMany()
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // 🟩 Đảm bảo mỗi User chỉ có một Wallet
            modelBuilder.Entity<Wallet>()
                .HasIndex(w => w.UserId)
                .IsUnique();

            // 🟩 Cấu hình quan hệ Wallet - WalletTransaction (1-n)
            modelBuilder.Entity<WalletTransaction>()
                .HasOne(wt => wt.Wallet)
                .WithMany(w => w.Transactions)
                .HasForeignKey(wt => wt.WalletId)
                .OnDelete(DeleteBehavior.Cascade);

            // 🟩 Cấu hình quan hệ WalletTransaction - Order (n-1, optional)
            modelBuilder.Entity<WalletTransaction>()
                .HasOne(wt => wt.Order)
                .WithMany()
                .HasForeignKey(wt => wt.OrderId)
                .OnDelete(DeleteBehavior.SetNull);

            // 🟩 Cấu hình decimal cho Wallet và WalletTransaction
            modelBuilder.Entity<Wallet>()
                .Property(w => w.Balance)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<WalletTransaction>()
                .Property(wt => wt.Amount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<WalletTransaction>()
                .Property(wt => wt.BalanceAfter)
                .HasColumnType("decimal(18,2)");

            // 🟩 Cấu hình quan hệ WalletRequest - User (n-1)
            modelBuilder.Entity<WalletRequest>()
                .HasOne(wr => wr.User)
                .WithMany()
                .HasForeignKey(wr => wr.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // 🟩 Cấu hình quan hệ WalletRequest - Wallet (n-1)
            modelBuilder.Entity<WalletRequest>()
                .HasOne(wr => wr.Wallet)
                .WithMany()
                .HasForeignKey(wr => wr.WalletId)
                .OnDelete(DeleteBehavior.Restrict);

            // 🟩 Cấu hình quan hệ WalletRequest - ProcessedByUser (n-1, optional)
            modelBuilder.Entity<WalletRequest>()
                .HasOne(wr => wr.ProcessedByUser)
                .WithMany()
                .HasForeignKey(wr => wr.ProcessedBy)
                .OnDelete(DeleteBehavior.SetNull);

            // 🟩 Cấu hình decimal cho WalletRequest
            modelBuilder.Entity<WalletRequest>()
                .Property(wr => wr.Amount)
                .HasColumnType("decimal(18,2)");

            // 🟩 Cấu hình quan hệ User - BankAccount (1-n)
            modelBuilder.Entity<BankAccount>()
                .HasOne(ba => ba.User)
                .WithMany()
                .HasForeignKey(ba => ba.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // 🟩 Đảm bảo chỉ một tài khoản mặc định cho mỗi user
            modelBuilder.Entity<BankAccount>()
                .HasIndex(ba => new { ba.UserId, ba.IsDefault })
                .IsUnique()
                .HasFilter("\"IsDefault\" = true");

            // 🟩 Cấu hình quan hệ OrderEnterpriseStatus - Order (n-1)
            modelBuilder.Entity<OrderEnterpriseStatus>()
                .HasOne(oes => oes.Order)
                .WithMany()
                .HasForeignKey(oes => oes.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // 🟩 Cấu hình quan hệ OrderEnterpriseStatus - Enterprise (n-1)
            modelBuilder.Entity<OrderEnterpriseStatus>()
                .HasOne(oes => oes.Enterprise)
                .WithMany()
                .HasForeignKey(oes => oes.EnterpriseId)
                .OnDelete(DeleteBehavior.Restrict);

            // 🟩 Đảm bảo mỗi Order-Enterprise chỉ có một OrderEnterpriseStatus
            modelBuilder.Entity<OrderEnterpriseStatus>()
                .HasIndex(oes => new { oes.OrderId, oes.EnterpriseId })
                .IsUnique();
        }
    }
}
