# 🧪 Testing Guide - GiaLai OCOP Backend

Hướng dẫn chạy và viết tests cho dự án GiaLai OCOP Backend.

---

## 📋 Tổng Quan

Dự án sử dụng:
- **xUnit** - Testing framework
- **FluentAssertions** - Assertion library
- **Moq** - Mocking framework
- **Microsoft.EntityFrameworkCore.InMemory** - In-memory database cho testing
- **Microsoft.AspNetCore.Mvc.Testing** - Integration testing

---

## 🏃 Chạy Tests

### Chạy tất cả tests
```bash
dotnet test
```

### Chạy tests với coverage
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

### Chạy tests trong một class cụ thể
```bash
dotnet test --filter "FullyQualifiedName~RatingServiceTests"
```

### Chạy tests với output chi tiết
```bash
dotnet test --logger "console;verbosity=detailed"
```

---

## 📁 Cấu Trúc Tests

```
Tests/
├── Controllers/          # Integration tests cho Controllers
│   ├── AuthControllerTests.cs
│   └── ProductsControllerTests.cs
├── Services/             # Unit tests cho Services
│   ├── RatingServiceTests.cs
│   ├── TokenServiceTests.cs
│   └── EmailServiceTests.cs
├── Integration/          # Integration tests với WebApplicationFactory
│   ├── WebApplicationFactory.cs
│   ├── AuthControllerIntegrationTests.cs
│   ├── ProductsControllerIntegrationTests.cs
│   └── OrdersControllerIntegrationTests.cs
└── Helpers/              # Test helpers và utilities
    └── TestHelpers.cs
```

---

## 🧩 Unit Tests

### Services Tests

#### RatingServiceTests
- ✅ `UpdateProductAverageRatingAsync_WithReviews_ShouldCalculateCorrectAverage`
- ✅ `UpdateProductAverageRatingAsync_WithoutReviews_ShouldSetNull`
- ✅ `UpdateProductAverageRatingAsync_ProductNotFound_ShouldNotThrow`
- ✅ `UpdateEnterpriseAverageRatingAsync_WithApprovedProducts_ShouldCalculateCorrectAverage`
- ✅ `UpdateEnterpriseAverageRatingAsync_WithoutApprovedProducts_ShouldSetNull`
- ✅ `UpdateEnterpriseAverageRatingAsync_EnterpriseNotFound_ShouldNotThrow`

#### TokenServiceTests
- ✅ `CreateToken_WithValidInput_ShouldReturnValidToken`
- ✅ `CreateToken_ShouldContainCorrectClaims`
- ✅ `CreateToken_ShouldHaveCorrectExpiration`
- ✅ `CreateToken_WithDifferentRoles_ShouldCreateValidToken`
- ✅ `CreateToken_ShouldHaveCorrectIssuerAndAudience`

#### EmailServiceTests
- ✅ `SendOtpEmailAsync_WithMissingConfiguration_ShouldReturnFalse`
- ✅ `SendOtpEmailAsync_WithPlaceholderConfiguration_ShouldReturnFalse`
- ✅ `SendOtpEmailAsync_WithDifferentPurposes_ShouldHaveCorrectSubject`
- ✅ `SendOtpEmailAsync_WithValidConfiguration_ShouldNotThrow`

---

## 🔗 Integration Tests

### AuthControllerIntegrationTests
- ✅ `Register_WithValidData_ShouldReturnCreated`
- ✅ `Register_WithDuplicateEmail_ShouldReturnBadRequest`
- ✅ `Login_WithValidCredentials_ShouldReturnToken`
- ✅ `Login_WithInvalidCredentials_ShouldReturnUnauthorized`
- ✅ `Login_WithNonExistentEmail_ShouldReturnUnauthorized`

### ProductsControllerIntegrationTests
- ✅ `GetProducts_WithoutAuth_ShouldReturnOk`
- ✅ `GetProduct_WithValidId_ShouldReturnProduct`
- ✅ `GetProduct_WithInvalidId_ShouldReturnNotFound`
- ✅ `CreateProduct_WithoutAuth_ShouldReturnUnauthorized`

### OrdersControllerIntegrationTests
- ✅ `CreateOrder_WithoutAuth_ShouldReturnUnauthorized`
- ✅ `GetOrders_WithoutAuth_ShouldReturnUnauthorized`

---

## 🛠️ Test Helpers

### TestHelpers Class

Cung cấp các helper methods để tạo test data:

```csharp
// Tạo in-memory database
var context = TestHelpers.CreateInMemoryDbContext();

// Tạo test user
var user = TestHelpers.CreateTestUser("Test User", "test@example.com");

// Tạo test enterprise
var enterprise = TestHelpers.CreateTestEnterprise("Test Enterprise");

// Tạo test product
var product = TestHelpers.CreateTestProduct("Test Product", 100000);

// Seed test data
await TestHelpers.SeedTestDataAsync(context);
```

---

## 📊 Test Coverage

### Xem Coverage Report

Sau khi chạy tests với coverage:

```bash
# Generate coverage report
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# Xem report (cần cài ReportGenerator)
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"**/coverage.opencover.xml" -targetdir:"coverage" -reporttypes:Html
```

### Target Coverage

- **Services:** 80%+
- **Controllers:** 70%+
- **Overall:** 75%+

---

## ✍️ Viết Tests Mới

### Unit Test Template

```csharp
public class MyServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly MyService _service;

    public MyServiceTests()
    {
        _context = TestHelpers.CreateInMemoryDbContext();
        _service = new MyService(_context);
    }

    [Fact]
    public async Task MyMethod_WithValidInput_ShouldReturnExpected()
    {
        // Arrange
        // ...

        // Act
        var result = await _service.MyMethod();

        // Assert
        result.Should().NotBeNull();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
```

### Integration Test Template

```csharp
public class MyControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>, IDisposable
{
    private readonly HttpClient _client;
    private readonly AppDbContext _context;

    public MyControllerIntegrationTests(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
        var scope = factory.Services.CreateScope();
        _context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }

    [Fact]
    public async Task GetEndpoint_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/api/endpoint");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        _client.Dispose();
    }
}
```

---

## 🎯 Best Practices

1. **Arrange-Act-Assert Pattern**
   - Arrange: Setup test data
   - Act: Execute method under test
   - Assert: Verify results

2. **Test Naming**
   - Format: `MethodName_Scenario_ExpectedBehavior`
   - Example: `Login_WithInvalidPassword_ShouldReturnUnauthorized`

3. **One Assert Per Test**
   - Mỗi test chỉ nên test một behavior
   - Dễ debug khi test fail

4. **Use Test Helpers**
   - Sử dụng `TestHelpers` để tạo test data
   - Tránh duplicate code

5. **Clean Up**
   - Dispose resources trong `Dispose()` method
   - Sử dụng in-memory database riêng cho mỗi test

6. **Mock External Dependencies**
   - Mock SMTP client, external APIs
   - Sử dụng Moq cho mocking

---

## 🚀 CI/CD Integration

### GitHub Actions Example

```yaml
name: Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '9.0.x'
      - name: Restore dependencies
        run: dotnet restore
      - name: Build
        run: dotnet build --no-restore
      - name: Test
        run: dotnet test --no-build --verbosity normal /p:CollectCoverage=true
      - name: Upload coverage
        uses: codecov/codecov-action@v3
```

---

## 📈 Test Metrics

### Current Status

- **Total Tests:** ~20+
- **Unit Tests:** ~10+
- **Integration Tests:** ~10+
- **Coverage:** Đang cải thiện

### Goals

- **Total Tests:** 100+
- **Coverage:** 75%+
- **All Critical Paths:** Covered

---

## 🔍 Debugging Tests

### Visual Studio
1. Set breakpoint trong test
2. Right-click test → Debug Test

### VS Code
1. Install C# Test Explorer extension
2. Set breakpoint
3. Click Debug Test

### Command Line
```bash
# Run specific test with debugger
dotnet test --filter "FullyQualifiedName~MyTest" --logger "console;verbosity=detailed"
```

---

## 📚 Resources

- [xUnit Documentation](https://xunit.net/)
- [FluentAssertions Documentation](https://fluentassertions.com/)
- [Moq Documentation](https://github.com/moq/moq4)
- [ASP.NET Core Testing](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests)

---

**Last Updated:** 2024-11-17
