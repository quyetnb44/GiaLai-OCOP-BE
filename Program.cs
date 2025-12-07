using GiaLaiOCOP.Api.Data;
using GiaLaiOCOP.Api.Models;
using GiaLaiOCOP.Api.Services;
using GiaLaiOCOP.Api.Options;
using GiaLaiOCOP.Api.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GiaLaiOCOP.Api.Dtos;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.FileProviders;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

// 🔹 Cấu hình cho Production trên Render - Tắt reloadOnChange để tránh inotify limit
// 🔥 Đảm bảo ASPNETCORE_ENVIRONMENT = Production trên Render
var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    EnvironmentName = environmentName
});

// 🔹 Xóa tất cả config sources mặc định (có thể có reloadOnChange = true)
builder.Configuration.Sources.Clear();

// 🔹 Tạo lại ConfigurationBuilder để tắt reloadOnChange và chỉ load appsettings.json + Production
// 🔥 Production: Chỉ load appsettings.json và appsettings.Production.json
// 🔥 Không load Development hoặc Local files để tránh inotify limit
builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables(); // Environment variables override file config (quan trọng cho Render)

// 🔹 Kết nối Database (chỉ khi không phải Testing environment)
if (!builder.Environment.EnvironmentName.Equals("Testing", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
}

// 🔹 Add JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtSettings = builder.Configuration.GetSection("Jwt");
        var jwtKey = jwtSettings["Key"];
        if (string.IsNullOrWhiteSpace(jwtKey))
        {
            throw new InvalidOperationException("JWT key is not configured.");
        }
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            // Đảm bảo claims được map đúng cách
            NameClaimType = ClaimTypes.NameIdentifier,
            RoleClaimType = ClaimTypes.Role
        };
        
        // Đảm bảo JWT events được xử lý đúng
        options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogError(context.Exception, "JWT Authentication failed. Path: {Path}, Exception: {Exception}", 
                    context.Request.Path, context.Exception?.Message);
                
                // Không throw exception, để middleware xử lý
                context.Response.StatusCode = 401;
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                var claims = context.Principal?.Claims.Select(c => $"{c.Type}={c.Value}").ToList();
                logger.LogInformation($"JWT Token validated successfully. Path: {context.Request.Path}, Claims: {string.Join(", ", claims ?? new List<string>())}");
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogWarning("JWT Challenge triggered. Path: {Path}, Error: {Error}, ErrorDescription: {ErrorDescription}", 
                    context.Request.Path, context.Error, context.ErrorDescription);
                return Task.CompletedTask;
            }
        };
    });

// 🔹 Add CORS
builder.Services.AddCors(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        // Development: Cho phép tất cả origins
        options.AddPolicy("AllowAll", policy =>
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod());
    }
    else
    {
        // Production: Chỉ cho phép origins cụ thể
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
                           ?? new[] { "https://yourdomain.com" };
        
        options.AddPolicy("AllowAll", policy =>
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials());
    }
});

// 🔹 Add Email Service
builder.Services.AddScoped<IEmailService, EmailService>();

// 🔹 Add Rating Service
builder.Services.AddScoped<GiaLaiOCOP.Api.Services.IRatingService, GiaLaiOCOP.Api.Services.RatingService>();

// 🔹 Add GPS Address Service
builder.Services.AddScoped<IGpsAddressService, GpsAddressService>();

// 🔹 Add VietQR Service
builder.Services.AddScoped<IVietQrService, VietQrService>();

// 🔹 Add Social Auth Service (Google, Facebook)
builder.Services.AddHttpClient<ISocialAuthService, SocialAuthService>();

// 🔹 Add HttpClient for external API calls (Vietnam Address API)
builder.Services.AddHttpClient();

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

// 🔹 Configure app settings
builder.Services.Configure<BankTransferSettings>(builder.Configuration.GetSection("BankTransfer"));
builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection("Cloudinary"));

// 🔹 Cloudinary service
builder.Services.AddSingleton<ICloudinaryService, CloudinaryService>();

// 🔹 Add Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database");


var app = builder.Build();

// 🔹 Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 🔹 Global Exception Handler (phải đặt trước các middleware khác)
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

app.UseCors("AllowAll");

// 🔹 Serve static files (uploads/images + avatars)
// 🔥 Production: PhysicalFileProvider không tự động watch files nếu không sử dụng IChangeToken
// StaticFileMiddleware không sử dụng Watch() nên không gây inotify limit
app.UseStaticFiles();

var avatarsRoot = Path.Combine(builder.Environment.ContentRootPath, "uploads", "images", "avatars");
if (!Directory.Exists(avatarsRoot))
{
    Directory.CreateDirectory(avatarsRoot);
}

var imagesPath = Path.Combine(builder.Environment.ContentRootPath, "uploads", "images");
var imagesFileProvider = new PhysicalFileProvider(imagesPath);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = imagesFileProvider,
    RequestPath = "/uploads/images"
});

var documentsRoot = Path.Combine(builder.Environment.ContentRootPath, "uploads", "documents");
if (!Directory.Exists(documentsRoot))
{
    Directory.CreateDirectory(documentsRoot);
}

var documentsFileProvider = new PhysicalFileProvider(documentsRoot);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = documentsFileProvider,
    RequestPath = "/uploads/documents"
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// 🔹 Health Check endpoint
app.MapHealthChecks("/health");
// 🔹 Khởi tạo dữ liệu mặc định (chỉ khi không phải Testing environment)
if (!app.Environment.EnvironmentName.Equals("Testing", StringComparison.OrdinalIgnoreCase))
{
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

        // 4️⃣ Cập nhật IsEmailVerified = true cho user cũ (tạo trước khi có tính năng xác thực email)
        var usersNotVerified = db.Users
            .Where(u => !u.IsEmailVerified)
            .ToList();

        if (usersNotVerified.Any())
        {
            foreach (var user in usersNotVerified)
            {
                user.IsEmailVerified = true;
            }
            db.SaveChanges();
            Console.WriteLine($"Đã cập nhật IsEmailVerified = true cho {usersNotVerified.Count} user cũ.");
        }

        // 5️⃣ Seed dữ liệu mẫu cho Map (chỉ trong Development)
        if (app.Environment.IsDevelopment())
        {
            MapSeedData.SeedMapData(db);
        }

        // 6️⃣ Cập nhật AverageRating cho dữ liệu hiện có (chỉ chạy 1 lần khi deploy)
        // Uncomment 2 dòng dưới để chạy script cập nhật AverageRating cho dữ liệu hiện có
        // var ratingService = scope.ServiceProvider.GetRequiredService<GiaLaiOCOP.Api.Services.IRatingService>();
        // await GiaLaiOCOP.Api.Scripts.UpdateAverageRatingsScript.RunAsync(db, ratingService);
    }
}
app.Run();

// Make Program class accessible for integration tests
public partial class Program { }
