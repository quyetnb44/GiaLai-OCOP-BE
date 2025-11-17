using GiaLaiOCOP.Api.Data;
using GiaLaiOCOP.Api.Models;
using GiaLaiOCOP.Api.Services;
using GiaLaiOCOP.Api.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GiaLaiOCOP.Api.Dtos;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);

// 🔹 Kết nối Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 🔹 Add JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtSettings = builder.Configuration.GetSection("Jwt");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]))
        };
    });

// 🔹 Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// 🔹 Add Email Service
builder.Services.AddScoped<IEmailService, EmailService>();

// 🔹 Add Controllers và Swagger với JSON options
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Đảm bảo property names được convert sang camelCase
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        // Đảm bảo boolean values được serialize (kể cả false)
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "GiaLaiOCOP API", Version = "v1" });

    // ✅ Thêm cấu hình JWT vào Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Nhập token theo dạng: Bearer {your token}",
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// 🔹 Configure BankTransfer settings
builder.Services.Configure<BankTransferSettings>(builder.Configuration.GetSection("BankTransfer"));


var app = builder.Build();

// 🔹 Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

// 🔹 Serve static files (uploads/images)
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
// 🔹 Khởi tạo dữ liệu mặc định
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // 1️⃣ Tạo Enterprise mặc định nếu chưa có
    Enterprise defaultEnterprise;
    if (!db.Enterprises.Any())
    {
        defaultEnterprise = new Enterprise
        {
            Name = "Default Enterprise",
            Description = "Enterprise mặc định để gán các sản phẩm cũ"
        };
        db.Enterprises.Add(defaultEnterprise);
        db.SaveChanges();
        Console.WriteLine("Đã tạo Enterprise mặc định.");
    }
    else
    {
        defaultEnterprise = db.Enterprises.First();
    }

    // 2️⃣ Gán EnterpriseId cho Product chưa có
    var productsWithoutEnterprise = db.Products
        .Where(p => p.EnterpriseId == 0) // 🔥 Sửa: EnterpriseId là int, chỉ check == 0
        .ToList();

    foreach (var p in productsWithoutEnterprise)
    {
        p.EnterpriseId = defaultEnterprise.Id;
    }
    db.SaveChanges();
    Console.WriteLine($"Đã gán EnterpriseId cho {productsWithoutEnterprise.Count} sản phẩm chưa có.");

    // 3️⃣ Tạo SystemAdmin nếu chưa có
    if (!db.Users.Any(u => u.Role == "SystemAdmin"))
    {
        var sysAdmin = new User
        {
            Name = "System Administrator",
            Email = "admin@system.com",
            Password = BCrypt.Net.BCrypt.HashPassword("123456"),
            Role = "SystemAdmin",
            IsEmailVerified = true // SystemAdmin mặc định đã verify
        };
        db.Users.Add(sysAdmin);
        db.SaveChanges();
        Console.WriteLine("SystemAdmin mặc định đã được tạo: admin@system.com / 123456");
    }

    // 4️⃣ Cập nhật IsEmailVerified = true cho user cũ (chỉ user tạo TRƯỚC khi có tính năng xác thực email)
    // CHỈ update user cũ - user tạo trước ngày deploy tính năng xác thực email
    // ⚠️ THAY ĐỔI NGÀY NÀY thành ngày bạn deploy tính năng xác thực email (ví dụ: 2024-12-20)
    var emailVerificationFeatureDate = new DateTime(2024, 12, 20, 0, 0, 0, DateTimeKind.Utc); // ⚠️ ĐỔI NGÀY NÀY!

    var usersNotVerified = db.Users
        .Where(u => !u.IsEmailVerified 
                    && u.CreatedAt < emailVerificationFeatureDate) // CHỈ update user tạo TRƯỚC ngày này
        .ToList();

    if (usersNotVerified.Any())
    {
        foreach (var user in usersNotVerified)
        {
            user.IsEmailVerified = true;
        }
        db.SaveChanges();
        Console.WriteLine($"✅ Đã cập nhật IsEmailVerified = true cho {usersNotVerified.Count} user cũ (tạo trước {emailVerificationFeatureDate:yyyy-MM-dd}).");
    }
    else
    {
        Console.WriteLine($"ℹ️ Không có user cũ cần cập nhật IsEmailVerified.");
    }

    // 5️⃣ Seed dữ liệu mẫu cho Map (chỉ trong Development)
    if (app.Environment.IsDevelopment())
    {
        MapSeedData.SeedMapData(db);
    }
}
app.Run();
