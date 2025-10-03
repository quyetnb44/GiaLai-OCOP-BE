using GiaLaiOCOP.Api.Data;
using GiaLaiOCOP.Api.Dtos;
using GiaLaiOCOP.Api.Models;
using GiaLaiOCOP.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));


var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];
var tokenLifetimeMinutes = int.Parse(builder.Configuration["Jwt:TokenLifetimeMinutes"] ?? "60");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // dev only; bật true cho production + HTTPS
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(2)
    };
});

// Role-based policies (tuỳ chỉnh)
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("ProducerOnly", policy => policy.RequireRole("Producer"));
});

// Swagger: bổ sung cấu hình để nhập Bearer token
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "GiaLaiOCOP API", Version = "v1" });
    // JWT Bearer in Swagger
    var securityScheme = new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter 'Bearer {token}'",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new Microsoft.OpenApi.Models.OpenApiReference
        {
            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
            Id = JwtBearerDefaults.AuthenticationScheme
        }
    };
    c.AddSecurityDefinition(securityScheme.Reference.Id, securityScheme);
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        { securityScheme, Array.Empty<string>() }
    });
});

builder.Services.AddScoped<ITokenService, TokenService>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Seed admin user if not exists
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // ensure DB created (optional if migrations done)
    db.Database.EnsureCreated();

    var adminEmail = "admin@example.com";
    if (!db.Users.Any(u => u.Email == adminEmail))
    {
        var admin = new User
        {
            Name = "Administrator",
            Email = adminEmail,
            Password = BCrypt.Net.BCrypt.HashPassword("Admin@123"), // change later
            Role = "Admin",
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(admin);
        db.SaveChanges();
        Console.WriteLine("Seeded admin user: admin@example.com / Admin@123");
    }
}

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// ===================== WeatherForecast Test =====================
var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast(
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        )).ToArray();
    return forecast;
});

// ===================== Auth =====================
app.MapPost("/api/auth/register", async (RegisterDto dto, AppDbContext db) =>
{
    if (await db.Users.AnyAsync(u => u.Email == dto.Email))
        return Results.BadRequest("Email already exists");

    var user = new User
    {
        Name = dto.Name,
        Email = dto.Email,
        Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
        Role = dto.Role ?? "Customer",
        CreatedAt = DateTime.UtcNow
    };

    db.Users.Add(user);
    await db.SaveChangesAsync();

    return Results.Created($"/api/users/{user.Id}", new
    {
        user.Id,
        user.Name,
        user.Email,
        user.Role
    });
}).AllowAnonymous();


app.MapPost("/api/auth/login", async (LoginDto dto, AppDbContext db, ITokenService tokenService) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
    if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.Password))
        return Results.BadRequest("Invalid email or password");

    var token = tokenService.CreateToken(user.Id, user.Email, user.Role);

    return Results.Ok(new
    {
        user.Id,
        user.Name,
        user.Email,
        user.Role,
        Token = token
    });
}).AllowAnonymous();

// ===================== Users CRUD =====================
app.MapGet("/api/users", async (AppDbContext db) => await db.Users.ToListAsync())
   .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });

app.MapPost("/api/users", async (User user, AppDbContext db) =>
{
    db.Users.Add(user);
    await db.SaveChangesAsync();
    return Results.Created($"/api/users/{user.Id}", user);
})
.AllowAnonymous();

app.MapDelete("/api/users/{id}", async (int id, AppDbContext db) =>
{
    var user = await db.Users.FindAsync(id);
    if (user is null) return Results.NotFound();

    db.Users.Remove(user);
    await db.SaveChangesAsync();
    return Results.NoContent();
})
.RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });

// ===================== Producers CRUD =====================
app.MapGet("/api/producers", async (AppDbContext db) => await db.Producers.ToListAsync())
   .AllowAnonymous();

app.MapPost("/api/producers", async (Producer producer, AppDbContext db) =>
{
    db.Producers.Add(producer);
    await db.SaveChangesAsync();
    return Results.Created($"/api/producers/{producer.Id}", producer);
})
.RequireAuthorization(new AuthorizeAttribute { Roles = "Producer,Admin" });

app.MapDelete("/api/producers/{id}", async (int id, AppDbContext db) =>
{
    var producer = await db.Producers.FindAsync(id);
    if (producer is null) return Results.NotFound();

    db.Producers.Remove(producer);
    await db.SaveChangesAsync();
    return Results.NoContent();
})
.RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });

// ===================== Products CRUD =====================
app.MapGet("/api/products", async (AppDbContext db) => await db.Products.ToListAsync())
   .AllowAnonymous();

app.MapPost("/api/products", async (Product product, AppDbContext db) =>
{
    db.Products.Add(product);
    await db.SaveChangesAsync();
    return Results.Created($"/api/products/{product.Id}", product);
})
.RequireAuthorization(new AuthorizeAttribute { Roles = "Producer,Admin" });

app.MapPut("/api/products/{id}", async (int id, Product updatedProduct, AppDbContext db) =>
{
    var product = await db.Products.FindAsync(id);
    if (product is null) return Results.NotFound();

    product.Name = updatedProduct.Name;
    product.Price = updatedProduct.Price;
    await db.SaveChangesAsync();

    return Results.Ok(product);
})
.RequireAuthorization(new AuthorizeAttribute { Roles = "Producer,Admin" });

app.MapDelete("/api/products/{id}", async (int id, AppDbContext db) =>
{
    var product = await db.Products.FindAsync(id);
    if (product is null) return Results.NotFound();

    db.Products.Remove(product);
    await db.SaveChangesAsync();
    return Results.NoContent();
})
.RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });

// ===================== Orders CRUD =====================
app.MapGet("/api/orders", async (AppDbContext db) => await db.Orders.ToListAsync())
   .RequireAuthorization();

app.MapGet("/api/orders/{id}", async (int id, AppDbContext db) =>
{
    var order = await db.Orders.FindAsync(id);
    return order is not null ? Results.Ok(order) : Results.NotFound();
});

app.MapPost("/api/orders", async (Order order, AppDbContext db) =>
{
    db.Orders.Add(order);
    await db.SaveChangesAsync();
    return Results.Created($"/api/orders/{order.Id}", order);
})
.RequireAuthorization();

app.MapPut("/api/orders/{id}", async (int id, Order updatedOrder, AppDbContext db) =>
{
    var order = await db.Orders.FindAsync(id);
    if (order is null) return Results.NotFound();

    order.UserId = updatedOrder.UserId;
    order.TotalAmount = updatedOrder.TotalAmount;
    order.OrderDate = updatedOrder.OrderDate;

    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapDelete("/api/orders/{id}", async (int id, AppDbContext db) =>
{
    var order = await db.Orders.FindAsync(id);
    if (order is null) return Results.NotFound();

    db.Orders.Remove(order);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// ===================== OrderItems CRUD =====================
app.MapGet("/api/orderitems", async (AppDbContext db) => await db.OrderItems.ToListAsync());

app.MapGet("/api/orderitems/{id}", async (int id, AppDbContext db) =>
{
    var item = await db.OrderItems.FindAsync(id);
    return item is not null ? Results.Ok(item) : Results.NotFound();
});

app.MapPost("/api/orderitems", async (OrderItem item, AppDbContext db) =>
{
    db.OrderItems.Add(item);
    await db.SaveChangesAsync();
    return Results.Created($"/api/orderitems/{item.Id}", item);
});

app.MapPut("/api/orderitems/{id}", async (int id, OrderItem updatedItem, AppDbContext db) =>
{
    var item = await db.OrderItems.FindAsync(id);
    if (item is null) return Results.NotFound();

    item.OrderId = updatedItem.OrderId;
    item.ProductId = updatedItem.ProductId;
    item.Quantity = updatedItem.Quantity;
    item.Price = updatedItem.Price;

    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapDelete("/api/orderitems/{id}", async (int id, AppDbContext db) =>
{
    var item = await db.OrderItems.FindAsync(id);
    if (item is null) return Results.NotFound();

    db.OrderItems.Remove(item);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// ===================== Reviews CRUD =====================
app.MapGet("/api/reviews", async (AppDbContext db) => await db.Reviews.ToListAsync());

app.MapGet("/api/reviews/{id}", async (int id, AppDbContext db) =>
{
    var review = await db.Reviews.FindAsync(id);
    return review is not null ? Results.Ok(review) : Results.NotFound();
});

app.MapPost("/api/reviews", async (Review review, AppDbContext db) =>
{
    db.Reviews.Add(review);
    await db.SaveChangesAsync();
    return Results.Created($"/api/reviews/{review.Id}", review);
});

app.MapPut("/api/reviews/{id}", async (int id, Review updatedReview, AppDbContext db) =>
{
    var review = await db.Reviews.FindAsync(id);
    if (review is null) return Results.NotFound();

    review.UserId = updatedReview.UserId;
    review.ProductId = updatedReview.ProductId;
    review.Comment = updatedReview.Comment;
    review.Rating = updatedReview.Rating;

    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapDelete("/api/reviews/{id}", async (int id, AppDbContext db) =>
{
    var review = await db.Reviews.FindAsync(id);
    if (review is null) return Results.NotFound();

    db.Reviews.Remove(review);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// ===================== Transactions CRUD =====================
app.MapGet("/api/transactions", async (AppDbContext db) => await db.Transactions.ToListAsync())
   .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });

app.MapGet("/api/transactions/{id}", async (int id, AppDbContext db) =>
{
    var txn = await db.Transactions.FindAsync(id);
    return txn is not null ? Results.Ok(txn) : Results.NotFound();
});

app.MapPost("/api/transactions", async (Transaction txn, AppDbContext db) =>
{
    db.Transactions.Add(txn);
    await db.SaveChangesAsync();
    return Results.Created($"/api/transactions/{txn.Id}", txn);
});

app.MapPut("/api/transactions/{id}", async (int id, Transaction updatedTxn, AppDbContext db) =>
{
    var txn = await db.Transactions.FindAsync(id);
    if (txn is null) return Results.NotFound();

    txn.OrderId = updatedTxn.OrderId;
    txn.Amount = updatedTxn.Amount;
    txn.PaymentMethod = updatedTxn.PaymentMethod;
    txn.TransactionDate = updatedTxn.TransactionDate;

    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapDelete("/api/transactions/{id}", async (int id, AppDbContext db) =>
{
    var txn = await db.Transactions.FindAsync(id);
    if (txn is null) return Results.NotFound();

    db.Transactions.Remove(txn);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// ===================== Locations CRUD =====================
app.MapGet("/api/locations", async (AppDbContext db) => await db.Locations.ToListAsync());

app.MapGet("/api/locations/{id}", async (int id, AppDbContext db) =>
{
    var loc = await db.Locations.FindAsync(id);
    return loc is not null ? Results.Ok(loc) : Results.NotFound();
});

app.MapPost("/api/locations", async (Location loc, AppDbContext db) =>
{
    db.Locations.Add(loc);
    await db.SaveChangesAsync();
    return Results.Created($"/api/locations/{loc.Id}", loc);
});

app.MapPut("/api/locations/{id}", async (int id, Location updatedLoc, AppDbContext db) =>
{
    var loc = await db.Locations.FindAsync(id);
    if (loc is null) return Results.NotFound();

    loc.Name = updatedLoc.Name;
    loc.Address = updatedLoc.Address;
    loc.Latitude = updatedLoc.Latitude;
    loc.Longitude = updatedLoc.Longitude;

    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapDelete("/api/locations/{id}", async (int id, AppDbContext db) =>
{
    var loc = await db.Locations.FindAsync(id);
    if (loc is null) return Results.NotFound();

    db.Locations.Remove(loc);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();
