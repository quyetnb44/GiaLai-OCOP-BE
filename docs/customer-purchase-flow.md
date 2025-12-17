# Luồng Mua Hàng của Customer

## Sơ đồ tổng quan

```mermaid
flowchart TD
    Start([Customer bắt đầu]) --> Browse[Duyệt sản phẩm]
    Browse --> AddToCart[Thêm vào giỏ hàng]
    AddToCart --> Checkout[Tiến hành thanh toán]
    
    Checkout --> CreateOrder[POST /api/orders<br/>Tạo đơn hàng]
    
    CreateOrder --> ValidateOrder{Validate<br/>đơn hàng}
    ValidateOrder -->|Lỗi| ErrorOrder[Trả về lỗi]
    ValidateOrder -->|OK| CreateOrderItems[Tạo OrderItems]
    
    CreateOrderItems --> CreateOrderEnterpriseStatus[Tạo OrderEnterpriseStatus<br/>cho mỗi Enterprise]
    CreateOrderEnterpriseStatus --> NotifyEnterpriseAdmin[Gửi notification<br/>cho EnterpriseAdmin]
    
    NotifyEnterpriseAdmin --> OrderCreated[Đơn hàng tạo thành công<br/>Status: Pending]
    
    OrderCreated --> CreatePayment{Chọn phương thức<br/>thanh toán}
    
    CreatePayment -->|COD| CreatePaymentCOD[POST /api/payments<br/>Method: COD]
    CreatePayment -->|BankTransfer| CreatePaymentBT[POST /api/payments<br/>Method: BankTransfer]
    
    CreatePaymentCOD --> PaymentCOD[Payment Status: Pending<br/>Order PaymentStatus: Pending]
    
    CreatePaymentBT --> PaymentBT[Payment Status: AwaitingTransfer<br/>Order PaymentStatus: AwaitingTransfer]
    PaymentBT --> SystemAdminReview{SystemAdmin<br/>xét duyệt?}
    
    SystemAdminReview -->|Xác nhận| BankTransferConfirmed[BankTransferConfirmed<br/>Payment: Paid]
    SystemAdminReview -->|Từ chối| BankTransferRejected[BankTransferRejected<br/>+ Lý do từ chối]
    BankTransferRejected --> CustomerRetry{Customer<br/>thử lại?}
    CustomerRetry -->|Có| CreatePaymentBT
    CustomerRetry -->|Không| End([Kết thúc])
    
    PaymentCOD --> EnterpriseProcess[EnterpriseAdmin<br/>xử lý đơn hàng]
    BankTransferConfirmed --> EnterpriseProcess
    
    EnterpriseProcess --> Processing[Status: Processing]
    Processing --> Shipped[Status: Shipped]
    
    Shipped --> RequestCompletion[EnterpriseAdmin request<br/>completion]
    RequestCompletion --> PendingCompletion[Status: PendingCompletion]
    
    PendingCompletion --> SystemAdminApprove{SystemAdmin<br/>xác nhận?}
    
    SystemAdminApprove -->|Đồng ý| Completed[Status: Completed<br/>Cộng tiền vào ví<br/>EnterpriseAdmin]
    SystemAdminApprove -->|Từ chối| Rejected[Quay lại Shipped<br/>+ Lý do từ chối]
    
    Rejected --> EnterpriseProcess
    
    Completed --> End
    
    ErrorOrder --> End
    
    style Start fill:#90EE90
    style End fill:#FFB6C1
    style OrderCreated fill:#87CEEB
    style Completed fill:#98FB98
    style ErrorOrder fill:#FF6B6B
    style BankTransferRejected fill:#FFA07A
```

## Luồng chi tiết theo từng bước

### Bước 1: Tạo Đơn Hàng

```mermaid
sequenceDiagram
    participant C as Customer
    participant API as OrdersController
    participant DB as Database
    participant EA as EnterpriseAdmin
    participant N as Notification System

    C->>API: POST /api/orders<br/>{Items, PaymentMethod, ShippingAddress}
    API->>API: Validate shipping address
    API->>API: Validate items (quantity > 0)
    API->>DB: Check products exist, approved, in stock
    DB-->>API: Products info
    
    alt Validation failed
        API-->>C: 400 Bad Request
    else Validation success
        API->>DB: Create Order (Status: Pending)
        API->>DB: Create OrderItems
        API->>DB: Calculate TotalAmount
        API->>DB: Create OrderEnterpriseStatus<br/>for each Enterprise
        API->>N: Create notifications<br/>for EnterpriseAdmins
        N->>EA: Send notification
        API-->>C: 201 Created (OrderDto)
    end
```

### Bước 2: Tạo Thanh Toán

```mermaid
sequenceDiagram
    participant C as Customer
    participant API as PaymentsController
    participant DB as Database
    participant QR as QR Code Service

    C->>API: POST /api/payments<br/>{OrderId, Method}
    API->>DB: Load Order + OrderItems + Products
    API->>DB: Cancel existing pending payments
    
    API->>API: Group OrderItems by EnterpriseId
    API->>API: Calculate amount per Enterprise
    
    loop For each Enterprise
        alt Method = BankTransfer
            API->>DB: Get EnterpriseBankInfo
            API->>QR: Generate QR Code URL
            QR-->>API: QR Code URL
            API->>DB: Create Payment<br/>(Status: AwaitingTransfer)
        else Method = COD
            API->>DB: Create Payment<br/>(Status: Pending)
        end
    end
    
    API->>DB: Update Order.PaymentStatus
    API-->>C: 201 Created (PaymentDto[])
```

### Bước 3: Xử lý BankTransfer (nếu có)

```mermaid
sequenceDiagram
    participant C as Customer
    participant SA as SystemAdmin
    participant API as OrdersController
    participant DB as Database
    participant N as Notification System

    Note over C,DB: Payment Status: AwaitingTransfer
    
    SA->>API: POST /api/orders/{id}/confirm-bank-transfer<br/>{Confirmed: true/false}
    API->>DB: Load Order + Payments
    
    alt Confirmed = true
        API->>DB: Update Order.PaymentStatus = BankTransferConfirmed
        API->>DB: Update Payments Status = Paid
        API->>N: Notify Customer + EnterpriseAdmins
        N->>C: Bank transfer confirmed
        N->>EA: Bank transfer confirmed
        API-->>SA: 200 OK
    else Confirmed = false
        API->>DB: Update Order.PaymentStatus = BankTransferRejected
        API->>DB: Save rejection reason
        API->>N: Notify Customer
        N->>C: Bank transfer rejected + reason
        API-->>SA: 200 OK
        Note over C: Customer có thể tạo payment mới
    end
```

### Bước 4: Xử lý Đơn Hàng

```mermaid
sequenceDiagram
    participant EA as EnterpriseAdmin
    participant API as OrdersController
    participant DB as Database
    
    Note over EA,DB: Order Status: Pending
    
    EA->>API: PUT /api/orders/{id}/status<br/>{Status: Processing}
    API->>DB: Update OrderEnterpriseStatus
    API->>DB: Check all enterprises updated
    API->>DB: Update Order.Status = Processing
    
    EA->>API: PUT /api/orders/{id}/status<br/>{Status: Shipped}
    API->>DB: Update OrderEnterpriseStatus
    API->>DB: Update Order.Status = Shipped
```

### Bước 5: Xác nhận Hoàn Thành

```mermaid
sequenceDiagram
    participant EA as EnterpriseAdmin
    participant SA as SystemAdmin
    participant API as OrdersController
    participant DB as Database
    participant W as Wallet Service
    participant N as Notification System

    Note over EA,DB: Order Status: Shipped
    
    EA->>API: POST /api/orders/{id}/request-completion
    API->>DB: Update Order.Status = PendingCompletion
    API->>N: Notify SystemAdmin
    N->>SA: Completion request notification
    
    SA->>API: POST /api/orders/{id}/approve-completion<br/>{Approved: true/false}
    
    alt Approved = true
        API->>DB: Update Order.Status = Completed
        loop For each Enterprise
            API->>DB: Calculate amount per Enterprise
            API->>W: Add money to EnterpriseAdmin wallet
            W->>DB: Create wallet transaction
            API->>N: Notify EnterpriseAdmin (money added)
        end
        API-->>SA: 200 OK (Order Completed)
    else Approved = false
        API->>DB: Update Order.Status = Shipped
        API->>DB: Save rejection reason
        API->>N: Notify EnterpriseAdmin (rejected)
        API-->>SA: 200 OK (Order Rejected)
    end
```

## Trạng thái và Chuyển đổi

### Order Status Flow

```mermaid
stateDiagram-v2
    [*] --> Pending: Customer tạo đơn
    
    Pending --> Processing: EnterpriseAdmin xử lý
    Pending --> Cancelled: Customer hủy
    
    Processing --> Shipped: EnterpriseAdmin giao hàng
    
    Shipped --> PendingCompletion: EnterpriseAdmin<br/>request completion
    
    PendingCompletion --> Completed: SystemAdmin<br/>approve
    PendingCompletion --> Shipped: SystemAdmin<br/>reject
    
    Completed --> [*]
    Cancelled --> [*]
    
    note right of Pending
        - Order mới tạo
        - Chờ EnterpriseAdmin xử lý
    end note
    
    note right of Processing
        - EnterpriseAdmin đã xác nhận
        - Đang chuẩn bị hàng
    end note
    
    note right of Shipped
        - Đã giao hàng
        - Chờ customer nhận
    end note
    
    note right of PendingCompletion
        - EnterpriseAdmin yêu cầu xác nhận hoàn thành
        - Chờ SystemAdmin xét duyệt
    end note
    
    note right of Completed
        - Đơn hàng hoàn thành
        - Đã cộng tiền vào ví EnterpriseAdmin
    end note
```

### Payment Status Flow

```mermaid
stateDiagram-v2
    [*] --> Pending: COD Payment<br/>created
    
    [*] --> AwaitingTransfer: BankTransfer<br/>Payment created
    
    AwaitingTransfer --> BankTransferConfirmed: SystemAdmin<br/>confirm
    AwaitingTransfer --> BankTransferRejected: SystemAdmin<br/>reject
    
    BankTransferConfirmed --> Paid: Auto update
    
    Pending --> Paid: EnterpriseAdmin<br/>confirm (COD)
    Pending --> Cancelled: Cancel payment
    
    AwaitingTransfer --> Cancelled: Cancel payment
    BankTransferRejected --> Cancelled: Customer cancel
    
    Cancelled --> [*]
    Paid --> [*]
    
    note right of Pending
        COD: Chờ thanh toán
        khi nhận hàng
    end note
    
    note right of AwaitingTransfer
        BankTransfer: Chờ SystemAdmin
        xác nhận đã nhận tiền
    end note
    
    note right of Paid
        Đã thanh toán thành công
    end note
```

## Mối quan hệ giữa Order và Payment

```mermaid
erDiagram
    ORDER ||--o{ ORDER_ITEM : contains
    ORDER ||--o{ PAYMENT : has
    ORDER }o--|| USER : "belongs to (Customer)"
    ORDER }o--o| SHIPPING_ADDRESS : uses
    
    ORDER_ITEM }o--|| PRODUCT : references
    PRODUCT }o--|| ENTERPRISE : belongs_to
    
    PAYMENT }o--|| ENTERPRISE : "paid to"
    
    ORDER {
        int Id
        int UserId
        string Status
        string PaymentStatus
        string PaymentMethod
        decimal TotalAmount
    }
    
    ORDER_ITEM {
        int Id
        int OrderId
        int ProductId
        int Quantity
        decimal Price
    }
    
    PAYMENT {
        int Id
        int OrderId
        int EnterpriseId
        decimal Amount
        string Method
        string Status
    }
    
    ENTERPRISE {
        int Id
        string Name
    }
```

## API Endpoints Summary

### Customer Endpoints

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| POST | `/api/orders` | Tạo đơn hàng mới |
| GET | `/api/orders` | Lấy danh sách đơn hàng của mình |
| GET | `/api/orders/{id}` | Xem chi tiết đơn hàng |
| PUT | `/api/orders/{id}/status` | Hủy đơn hàng (chỉ khi Pending) |
| DELETE | `/api/orders/{id}` | Xóa đơn hàng (chỉ khi Pending/Cancelled) |
| POST | `/api/payments` | Tạo thanh toán cho đơn hàng |
| GET | `/api/payments/order/{orderId}` | Xem các payment của đơn hàng |
| GET | `/api/payments/{id}/qr-code` | Lấy QR code thanh toán |

### EnterpriseAdmin Endpoints

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/api/orders` | Xem đơn hàng có sản phẩm của Enterprise mình |
| PUT | `/api/orders/{id}/status` | Cập nhật trạng thái (Processing → Shipped) |
| POST | `/api/orders/{id}/request-completion` | Yêu cầu xác nhận hoàn thành |

### SystemAdmin Endpoints

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| POST | `/api/orders/{id}/confirm-bank-transfer` | Xác nhận/từ chối chuyển khoản |
| POST | `/api/orders/{id}/approve-completion` | Xác nhận/từ chối hoàn thành đơn hàng |

