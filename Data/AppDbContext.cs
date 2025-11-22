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
                .OnDelete(DeleteBehavior.Restrict);

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
        }
    }
}
