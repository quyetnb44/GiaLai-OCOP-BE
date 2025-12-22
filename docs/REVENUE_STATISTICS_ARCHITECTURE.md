# Kiến Trúc Hệ Thống Phân Tích & Thống Kê Doanh Thu

## Tổng Quan

Hệ thống phân tích và thống kê doanh thu được thiết kế với kiến trúc phân tầng, có khả năng mở rộng và dễ bảo trì. Hệ thống tuân theo các nguyên tắc SOLID và sử dụng các design patterns phù hợp.

## Kiến Trúc Tổng Thể

```
┌─────────────────────────────────────────────────────────┐
│                    Controllers Layer                    │
│              (ReportsController)                        │
│  - Nhận request từ client                               │
│  - Validate input                                       │
│  - Gọi services                                         │
│  - Trả về response                                      │
└──────────────────┬──────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────────────────────┐
│                   Services Layer                        │
│  ┌──────────────────────────────────────────────────┐  │
│  │  IRevenueStatisticsService                       │  │
│  │  - Orchestrate business logic                     │  │
│  │  - Coordinate các services khác                  │  │
│  └──────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────┐  │
│  │  IRevenueAuthorizationService                     │  │
│  │  - Kiểm soát phạm vi dữ liệu theo role           │  │
│  │  - Validate quyền truy cập                       │  │
│  └──────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────┐  │
│  │  IRevenueCalculationService                      │  │
│  │  - Tính toán doanh thu                           │  │
│  │  - Đếm số đơn hàng                               │  │
│  │  - Tính toán dữ liệu biểu đồ                     │  │
│  └──────────────────────────────────────────────────┘  │
└──────────────────┬──────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────────────────────┐
│              Strategy Pattern Layer                     │
│  ┌──────────────────────────────────────────────────┐  │
│  │  ITimePeriodStrategy                             │  │
│  │  ├── WeekPeriodStrategy                          │  │
│  │  ├── MonthPeriodStrategy                         │  │
│  │  └── YearPeriodStrategy                          │  │
│  │                                                   │  │
│  │  TimePeriodStrategyFactory                        │  │
│  │  - Tạo strategy phù hợp theo type                 │  │
│  └──────────────────────────────────────────────────┘  │
└──────────────────┬──────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────────────────────┐
│                   Data Access Layer                      │
│              (AppDbContext / EF Core)                  │
│  - Truy vấn database                                    │
│  - Tính toán aggregate                                  │  │
└─────────────────────────────────────────────────────────┘
```

## Các Thành Phần Chính

### 1. DTOs (Data Transfer Objects)

#### `RevenueStatisticsRequestDto`
- Chứa thông tin request từ client
- Fields: `Type`, `Date`, `EnterpriseId`

#### `RevenueStatisticsResponseDto`
- Chứa kết quả thống kê
- Bao gồm: `Filter`, `Summary`, `Chart`

### 2. Services

#### `IRevenueAuthorizationService`
**Trách nhiệm:**
- Kiểm soát phạm vi dữ liệu dựa trên role của user
- Validate quyền truy cập
- Xác định enterpriseId được phép truy cập

**Logic phân quyền:**
- **EnterpriseAdmin**: Chỉ được xem doanh thu của doanh nghiệp mình (tự động lấy từ User.EnterpriseId)
- **SystemAdmin**: Có thể xem toàn hệ thống hoặc filter theo enterpriseId

**Security:**
- KHÔNG tin tưởng dữ liệu từ client
- Luôn validate và override enterpriseId từ request nếu user là EnterpriseAdmin

#### `IRevenueCalculationService`
**Trách nhiệm:**
- Tính tổng doanh thu trong khoảng thời gian
- Đếm số đơn hàng
- Tính toán dữ liệu cho biểu đồ

**Logic tính toán:**
- Doanh thu được tính từ các đơn hàng có `Status == "Completed"`
- Với enterprise: Tính từ `OrderItems` có `Product.EnterpriseId` khớp
- Toàn hệ thống: Tính từ `Order.TotalAmount`

#### `IRevenueStatisticsService`
**Trách nhiệm:**
- Orchestrate toàn bộ business logic
- Coordinate các services khác
- Tạo response DTO

**Flow xử lý:**
1. Validate request
2. Kiểm tra quyền truy cập
3. Xác định enterpriseId được phép
4. Parse reference date
5. Lấy strategy phù hợp
6. Tính toán khoảng thời gian
7. Tính doanh thu cho chart
8. Tính tổng hợp
9. Tạo response

### 3. Strategy Pattern - Time Period

#### `ITimePeriodStrategy`
Interface định nghĩa các phương thức:
- `CalculatePeriod(DateTime referenceDate)`: Tính khoảng thời gian
- `GenerateChartDataPoints(DateTime referenceDate)`: Tạo các điểm dữ liệu cho biểu đồ

#### Implementations

**`WeekPeriodStrategy`**
- Tuần bắt đầu từ thứ 2
- Tạo 7 điểm dữ liệu (7 ngày)

**`MonthPeriodStrategy`**
- Tháng từ ngày 1 đến ngày cuối cùng
- Tạo điểm dữ liệu cho từng ngày trong tháng

**`YearPeriodStrategy`**
- Năm từ tháng 1 đến tháng 12
- Tạo 12 điểm dữ liệu (12 tháng)

#### `TimePeriodStrategyFactory`
- Factory pattern để tạo strategy phù hợp
- Hỗ trợ mở rộng dễ dàng (chỉ cần thêm strategy mới vào dictionary)

## Mở Rộng Trong Tương Lai

### 1. Thêm Loại Thời Gian Mới
```csharp
// Tạo class mới implement ITimePeriodStrategy
public class QuarterPeriodStrategy : ITimePeriodStrategy
{
    public string Type => "quarter";
    // Implement các methods...
}

// Đăng ký vào factory
_strategies.Add("quarter", new QuarterPeriodStrategy());
```

### 2. So Sánh Giữa Các Kỳ
- Thêm method `GetPreviousPeriod()` vào `ITimePeriodStrategy`
- Tạo service mới `IRevenueComparisonService`
- Tính toán và so sánh doanh thu giữa 2 kỳ

### 3. Export Báo Cáo
- Tạo service `IRevenueReportExportService`
- Implement các format: PDF, Excel, CSV
- Sử dụng thư viện như EPPlus, iTextSharp

### 4. Realtime Statistics
- Sử dụng SignalR để push updates
- Cache kết quả với Redis
- Background job để tính toán định kỳ

### 5. Phân Loại Doanh Thu
- Theo sản phẩm
- Theo danh mục
- Theo khu vực
- Tạo thêm các strategy mới cho từng loại

### 6. Trừ Hoàn Tiền / Áp Dụng Thuế
- Mở rộng `IRevenueCalculationService`
- Thêm logic tính toán phức tạp hơn
- Có thể tách thành các strategy riêng

## Best Practices

### 1. Separation of Concerns
- Controller chỉ xử lý HTTP request/response
- Business logic nằm trong Services
- Data access logic trong Calculation Service

### 2. Single Responsibility Principle
- Mỗi service có một trách nhiệm rõ ràng
- Authorization service chỉ xử lý authorization
- Calculation service chỉ xử lý tính toán

### 3. Open/Closed Principle
- Dễ dàng thêm strategy mới mà không sửa code cũ
- Chỉ cần implement interface và đăng ký vào factory

### 4. Dependency Injection
- Tất cả dependencies được inject qua constructor
- Dễ dàng test và mock

### 5. Error Handling
- Sử dụng exception types phù hợp
- Controller catch và convert thành HTTP status codes

## Testing

### Unit Tests
- Test từng service riêng biệt
- Mock dependencies
- Test các edge cases

### Integration Tests
- Test flow hoàn chỉnh từ controller đến database
- Test authorization logic
- Test calculation accuracy

## Performance Considerations

### 1. Database Queries
- Sử dụng `AsNoTracking()` cho read-only queries
- Index trên các columns thường query: `OrderDate`, `Status`, `EnterpriseId`
- Consider materialized views cho các báo cáo phức tạp

### 2. Caching
- Cache kết quả thống kê theo key: `{type}_{date}_{enterpriseId}`
- Invalidate cache khi có đơn hàng mới

### 3. Pagination
- Nếu chart data quá lớn, consider pagination
- Hoặc limit số lượng điểm dữ liệu

## Security

### 1. Authorization
- Luôn validate role từ token
- Không tin tưởng client input
- EnterpriseAdmin không thể filter theo enterpriseId khác

### 2. Data Privacy
- Chỉ trả về dữ liệu user được phép xem
- Log các truy cập để audit

### 3. Input Validation
- Validate type, date format
- Sanitize input để tránh injection

## Logging

- Log các operations quan trọng
- Log authorization decisions
- Log calculation results (debug level)
- Log errors với đầy đủ context

## Kết Luận

Kiến trúc này cung cấp:
- ✅ Separation of concerns rõ ràng
- ✅ Dễ dàng mở rộng
- ✅ Dễ bảo trì
- ✅ Dễ test
- ✅ Security tốt
- ✅ Performance tối ưu

Hệ thống sẵn sàng cho các yêu cầu mở rộng trong tương lai như so sánh kỳ, export báo cáo, realtime statistics, v.v.

