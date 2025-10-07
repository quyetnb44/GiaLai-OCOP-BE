using GiaLaiOCOP.Api.Data;
using GiaLaiOCOP.Api.Models;
using GiaLaiOCOP.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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

// 🔹 Add Controllers và Swagger
builder.Services.AddControllers();
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


var app = builder.Build();

// 🔹 Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
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
        .Where(p => p.EnterpriseId == 0 || p.EnterpriseId == null)
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
            Role = "SystemAdmin"
        };
        db.Users.Add(sysAdmin);
        db.SaveChanges();
        Console.WriteLine("SystemAdmin mặc định đã được tạo: admin@system.com / 123456");
    }
}
app.Run();
