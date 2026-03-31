# WebDemoCA — Hướng dẫn toàn diện (Clean Architecture)

> **Stack:** ASP.NET Core 8 · MediatR (CQRS) · FluentValidation · Entity Framework Core · PostgreSQL (Neon)

---

## 📁 Mục lục

1. [Tổng quan kiến trúc](#1-tổng-quan-kiến-trúc)  
2. [Cấu trúc thư mục](#2-cấu-trúc-thư-mục)  
3. [Luồng hoạt động chi tiết](#3-luồng-hoạt-động-chi-tiết)  
   - [POST /api/products — Tạo sản phẩm](#31-post-apiproducts--tạo-sản-phẩm)  
   - [GET /api/products — Lấy danh sách](#32-get-apiproducts--lấy-danh-sách-sản-phẩm)  
4. [Từng lớp giải thích](#4-từng-lớp-giải-thích)  
   - [Domain](#41-webdemodomain)  
   - [Application](#42-webdemoapplication)  
   - [Infrastructure](#43-webdemoinfrastructure)  
   - [API](#44-webdemoapi)  
5. [Trắc nghiệm từng phần](#5-trắc-nghiệm-từng-phần)  
6. [Hướng dẫn thêm endpoint mới](#6-hướng-dẫn-thêm-endpoint-mới)  
7. [Hướng dẫn thêm Entity mới](#7-hướng-dẫn-thêm-entity-mới)  
8. [Cài đặt & chạy dự án](#8-cài-đặt--chạy-dự-án)

---

## 1. Tổng quan kiến trúc

Dự án áp dụng **Clean Architecture** (kiến trúc sạch), chia thành 4 lớp độc lập:

```
┌──────────────────────────────────────────────────┐
│                   API Layer                      │  ← Tiếp nhận HTTP request
│            (WebDemo.Api)                         │
├──────────────────────────────────────────────────┤
│               Application Layer                  │  ← Business logic / CQRS
│           (WebDemo.Application)                  │
├──────────────────────────────────────────────────┤
│                 Domain Layer                     │  ← Entity, Event, Rule
│             (WebDemo.Domain)                     │
├──────────────────────────────────────────────────┤
│            Infrastructure Layer                  │  ← DB, Repository, Services
│          (WebDemo.Infrastructure)                │
└──────────────────────────────────────────────────┘
```

**Nguyên tắc cốt lõi:**
- Lớp trong cùng (**Domain**) không phụ thuộc vào ai  
- Lớp ngoài luôn phụ thuộc vào lớp trong, không bao giờ ngược lại  
- **MediatR** làm cầu nối giữa Controller và Handler (CQRS pattern)  
- **FluentValidation** chạy tự động qua `ValidationBehaviour` trước khi Handler được gọi  

---

## 2. Cấu trúc thư mục

```
WebDemoCA/
├── WebDemo.Api/                         # Layer: API
│   ├── Controllers/
│   │   └── ProductsController.cs        # Endpoint HTTP
│   ├── Middleware/                      # (trống, chỗ thêm middleware tương lai)
│   ├── Program.cs                       # Entry point, DI registration
│   └── appsettings.json                 # Connection string, config
│
├── WebDemo.Application/                 # Layer: Application (Use Cases)
│   ├── DependencyInjection.cs           # Đăng ký MediatR + Validator + Behaviour
│   ├── Common/
│   │   ├── Behaviors/
│   │   │   └── ValidationBehaviour.cs   # Pipeline: validate trước khi handle
│   │   └── Interfaces/
│   │       └── IApplicationDbContext.cs # Interface DB (không phụ thuộc EF cụ thể)
│   └── Features/
│       └── Products/
│           ├── Commands/
│           │   ├── CreateProduct/
│           │   │   ├── CreateProductCommand.cs          # Command + Handler
│           │   │   └── CreateProductCommandValidator.cs # FluentValidation rules
│           │   ├── UpdateProduct/  (thư mục trống - chờ implement)
│           │   └── DeleteProduct/  (thư mục trống - chờ implement)
│           ├── Queries/
│           │   └── GetProducts/
│           │       ├── GetProductsQuery.cs  # Query + Handler
│           │       └── ProductDto.cs        # Data Transfer Object
│           └── EventHandlers/
│               └── LogProductCreated.cs     # Xử lý sự kiện sau khi tạo product
│
├── WebDemo.Domain/                      # Layer: Domain (nghiệp vụ thuần túy)
│   ├── Common/
│   │   ├── BaseEntity.cs                # ID + DomainEvents list
│   │   └── BaseEvent.cs                 # Kế thừa INotification của MediatR
│   ├── Entities/
│   │   └── Product.cs                   # Entity có validation nội bộ
│   ├── Events/
│   │   └── ProductCreatedEvent.cs       # Domain Event
│   ├── Exceptions/
│   │   └── DomainException.cs           # Exception nghiệp vụ
│   ├── Errors/
│   │   └── ProductErrors.cs             # (trống - chờ dùng Result pattern)
│   ├── ValueObjects/
│   │   └── ProductId.cs                 # (trống - chờ dùng strongly-typed ID)
│   └── Enums/                           # (trống)
│
└── WebDemo.Infrastructure/              # Layer: Infrastructure (kỹ thuật)
    ├── DependencyInjection.cs           # Đăng ký DbContext + interface binding
    ├── Persistence/
    │   ├── ApplicationDbContext.cs      # EF DbContext + publish domain events
    │   └── Configurations/
    │       └── ProductConfiguration.cs  # Fluent API cấu hình bảng Products
    ├── Repositories/                    # (trống - chờ implement nếu cần)
    ├── Services/                        # (trống - chờ implement email/storage...)
    └── Migrations/                      # EF Migration files
```

---

## 3. Luồng hoạt động chi tiết

### 3.1 POST /api/products — Tạo sản phẩm

```
Client
  │  POST /api/products  { "name": "Táo", "price": 15000 }
  ▼
ProductsController.Create(CreateProductCommand command)
  │  _mediator.Send(command)
  ▼
MediatR Pipeline
  │
  ├─► [1] ValidationBehaviour<CreateProductCommand, int>
  │       Gọi CreateProductCommandValidator
  │       → Kiểm tra: Name không rỗng, MaxLength 200
  │       → Kiểm tra: Price > 0
  │       → Nếu lỗi: throw ValidationException → trả 400 Bad Request
  │
  └─► [2] CreateProductCommandHandler.Handle()
          │  Product.Create("Táo", 15000)
          │    → gọi Validate() trong Domain
          │    → nếu lỗi: throw DomainException
          │    → tạo entity + AddDomainEvent(ProductCreatedEvent)
          │
          │  _context.Products.Add(entity)
          │  _context.SaveChangesAsync()
          │    ├─ Thu thập DomainEvents từ tất cả entity đang tracked
          │    ├─ ClearDomainEvents()
          │    ├─ base.SaveChangesAsync() → INSERT vào PostgreSQL
          │    └─ Publish từng event qua MediatR
          │         └─► LogProductCreated.Handle()
          │               Console.WriteLine("Product created: Táo")
          │
          └─ return entity.Id (int)

ProductsController
  │  return Ok(id)
  ▼
Client nhận: 200 OK, body = { id }
```

---

### 3.2 GET /api/products — Lấy danh sách sản phẩm

```
Client
  │  GET /api/products
  ▼
ProductsController.Get()
  │  _mediator.Send(new GetProductsQuery())
  ▼
MediatR Pipeline
  │  (Không có Validator cho Query này → bỏ qua bước validation)
  │
  └─► GetProductsQueryHandler.Handle()
        │  _context.Products
        │    .AsNoTracking()          ← không track entity (read-only, nhanh hơn)
        │    .Select(p => new ProductDto { Id, Name, Price })
        │    .ToListAsync()
        │
        └─ return List<ProductDto>

ProductsController
  │  return Ok(list)
  ▼
Client nhận: 200 OK, body = [ { id, name, price }, ... ]
```

---

## 4. Từng lớp giải thích

### 4.1 WebDemo.Domain

**Đây là trái tim của hệ thống — không import bất kỳ thứ gì từ bên ngoài (trừ MediatR interface).**

#### `BaseEntity`
```csharp
public abstract class BaseEntity
{
    public int Id { get; set; }
    private readonly List<object> _domainEvents = new();
    public IReadOnlyCollection<object> DomainEvents => _domainEvents;
    public void AddDomainEvent(object eventItem) { ... }
    public void ClearDomainEvents() { ... }
}
```
- Mọi Entity đều có `Id`  
- Mỗi entity có thể phát ra **Domain Events** (sự kiện nghiệp vụ)

#### `Product` Entity
```csharp
public class Product : BaseEntity
{
    public string Name { get; private set; }  // private set = chỉ thay đổi qua method
    public decimal Price { get; private set; }

    // Factory method — cách tạo đối tượng đúng DDD
    public static Product Create(string name, decimal price) { ... }

    // Cập nhật qua method, không update trực tiếp property
    public void Update(string name, decimal price) { ... }

    // Validate nội bộ — Domain tự bảo vệ chính nó
    private static void Validate(string name, decimal price) { ... }
}
```

#### `ProductCreatedEvent`
```csharp
public class ProductCreatedEvent : BaseEvent
{
    public Product Product { get; }
}
```
- Kế thừa `BaseEvent : INotification` → MediatR sẽ publish  
- Được thêm vào trong constructor của `Product`

---

### 4.2 WebDemo.Application

**Chứa toàn bộ Use Case (CQRS pattern = Commands + Queries tách biệt).**

#### `IApplicationDbContext` (Interface)
```csharp
public interface IApplicationDbContext
{
    DbSet<Product> Products { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
```
- Application chỉ biết **interface**, không biết EF Core cụ thể  
- Infrastructure sẽ implement interface này

#### `CreateProductCommand` + Handler
```csharp
// Command = "yêu cầu thực hiện hành động"
public record CreateProductCommand : IRequest<int>
{
    public string Name { get; init; }
    public decimal Price { get; init; }
}

// Handler = "người thực hiện yêu cầu"
public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, int>
{
    public async Task<int> Handle(CreateProductCommand request, CancellationToken ct)
    {
        var entity = Product.Create(request.Name, request.Price);
        _context.Products.Add(entity);
        await _context.SaveChangesAsync(ct);
        return entity.Id;
    }
}
```

#### `CreateProductCommandValidator`
```csharp
public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(v => v.Name).NotEmpty().MaximumLength(200);
        RuleFor(v => v.Price).GreaterThan(0);
    }
}
```
- Chạy tự động nhờ `ValidationBehaviour` trong MediatR pipeline  
- Nếu lỗi → throw `ValidationException` → trả về 400

#### `ValidationBehaviour` (MediatR Pipeline Behavior)
```csharp
public class ValidationBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    // Chặn mọi request, tìm validator phù hợp, chạy, nếu lỗi thì ném exception
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, ...)
    {
        if (validators.Any())
        {
            var failures = validators.Select(v => v.Validate(context))...
            if (failures.Count != 0) throw new ValidationException(failures);
        }
        return await next(); // gọi handler tiếp theo
    }
}
```

#### `DependencyInjection.cs` (Application)
```csharp
services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(...)); // tự scan handler
services.AddValidatorsFromAssembly(...);                           // tự scan validator
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>)); // gắn pipeline
```

---

### 4.3 WebDemo.Infrastructure

**Triển khai kỹ thuật cụ thể: kết nối DB, EF Core, publish events.**

#### `ApplicationDbContext`
```csharp
public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly IMediator _mediator;

    public DbSet<Product> Products => Set<Product>();

    // Override SaveChangesAsync để publish Domain Events SAU KHI lưu DB
    public override async Task<int> SaveChangesAsync(CancellationToken ct)
    {
        // 1. Thu domain events
        var domainEvents = ChangeTracker.Entries<BaseEntity>()
            .SelectMany(e => e.Entity.DomainEvents).ToList();

        // 2. Xóa events khỏi entity
        foreach (var entity in ChangeTracker.Entries<BaseEntity>())
            entity.Entity.ClearDomainEvents();

        // 3. Lưu DB trước
        var result = await base.SaveChangesAsync(ct);

        // 4. PUBLISH events sau khi lưu thành công
        foreach (var domainEvent in domainEvents)
            await _mediator.Publish(domainEvent, ct);

        return result;
    }
}
```

> ⚠️ **Quan trọng:** Events được publish **sau** khi `SaveChangesAsync` thành công. Đảm bảo data đã commit trước khi side effect xảy ra.

#### `ProductConfiguration`
```csharp
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Price).IsRequired();
    }
}
```
- Cấu hình bảng DB tách khỏi entity  
- Được load tự động bởi `ApplyConfigurationsFromAssembly()`

#### `DependencyInjection.cs` (Infrastructure)
```csharp
services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

services.AddScoped<IApplicationDbContext>(provider =>
    provider.GetRequiredService<ApplicationDbContext>());
```

---

### 4.4 WebDemo.Api

#### `ProductsController`
```csharp
[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    [HttpPost]
    public async Task<IActionResult> Create(CreateProductCommand command)
        => Ok(await _mediator.Send(command));

    [HttpGet]
    public async Task<IActionResult> Get()
        => Ok(await _mediator.Send(new GetProductsQuery()));
}
```
- Controller **không có bất kỳ logic** nào  
- Chỉ nhận input → chuyển cho MediatR → trả kết quả  

#### `Program.cs`
```csharp
builder.Services.AddApplication();        // Application DI
builder.Services.AddInfrastructure(...);  // Infrastructure DI
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.Run();
```

---

## 5. Trắc nghiệm từng phần

### 🔵 Domain Layer

**Câu 1:** `BaseEntity` dùng để làm gì?  
- A. Chứa logic gọi database  
- B. Khai báo `Id` và quản lý Domain Events ✅  
- C. Định nghĩa HTTP route  
- D. Validate dữ liệu đầu vào  

**Câu 2:** Tại sao `Product.Name` có `private set`?  
- A. Để EF Core có thể gán giá trị  
- B. Để giữ bất biến (encapsulation) — chỉ thay đổi qua method `Update()` ✅  
- C. Vì C# bắt buộc  
- D. Để tránh null  

**Câu 3:** `ProductCreatedEvent` được phát ra ở đâu?  
- A. Trong `CreateProductCommandHandler`  
- B. Trong `ApplicationDbContext.SaveChangesAsync()`  
- C. Trong constructor của `Product` ✅  
- D. Trong `LogProductCreated`  

**Câu 4:** `DomainException` được ném khi nào?  
- A. Khi không tìm thấy entity trong DB  
- B. Khi `Name` rỗng hoặc `Price <= 0` trong `Product.Validate()` ✅  
- C. Khi MediatR không tìm thấy Handler  
- D. Khi FluentValidation thất bại  

---

### 🟢 Application Layer

**Câu 5:** `IRequest<int>` trong `CreateProductCommand` nghĩa là gì?  
- A. Command này nhận tham số là `int`  
- B. Handler của command này trả về kiểu `int` ✅  
- C. Command được gửi qua HTTP GET  
- D. Command có priority là int  

**Câu 6:** `ValidationBehaviour` can thiệp vào pipeline ở đâu?  
- A. Sau khi Handler xử lý xong  
- B. Trước khi request đến Controller  
- C. Trước khi Handler được gọi ✅  
- D. Sau khi SaveChangesAsync  

**Câu 7:** Nếu bạn thêm một `GetProductByIdQuery` nhưng **không** tạo Validator, điều gì xảy ra?  
- A. Ứng dụng lỗi compile  
- B. `ValidationBehaviour` sẽ bỏ qua (không có validator → chạy thẳng handler) ✅  
- C. Sẽ throw `NullReferenceException`  
- D. FluentValidation tự tạo validator mặc định  

**Câu 8:** `AsNoTracking()` trong `GetProductsQueryHandler` có tác dụng gì?  
- A. Không cho phép thêm entity mới  
- B. EF Core không theo dõi thay đổi của entity, tiết kiệm bộ nhớ và nhanh hơn ✅  
- C. Query chạy trên bộ nhớ thay vì DB  
- D. Chỉ lấy entity chưa bị xóa  

**Câu 9:** Tại sao `IApplicationDbContext` là interface thay vì dùng thẳng `ApplicationDbContext`?  
- A. Interface nhanh hơn class  
- B. Để Application không phụ thuộc vào EF Core, dễ test/thay thế ✅  
- C. Interface giải quyết lỗi DI  
- D. Bắt buộc bởi MediatR  

---

### 🟡 Infrastructure Layer

**Câu 10:** Domain Events được publish ở thời điểm nào?  
- A. Trước khi gọi `base.SaveChangesAsync()`  
- B. Ngay khi entity được Add vào DbContext  
- C. Sau khi `base.SaveChangesAsync()` thành công ✅  
- D. Trong constructor của `ApplicationDbContext`  

**Câu 11:** `ProductConfiguration` được áp dụng vào model như thế nào?  
- A. Đăng ký thủ công trong `Program.cs`  
- B. Tự động load qua `ApplyConfigurationsFromAssembly()` ✅  
- C. Attribute trên class `Product`  
- D. Convention của EF Core  

**Câu 12:** Tại sao `AddScoped<IApplicationDbContext>` thay vì `AddTransient`?  
- A. Scoped nhanh hơn  
- B. `DbContext` nên sống trong phạm vi một HTTP request, không tạo lại mỗi lần inject ✅  
- C. Transient không hỗ trợ DbContext  
- D. MediatR yêu cầu Scoped  

---

### 🔴 API Layer

**Câu 13:** Controller có logic nghiệp vụ không?  
- A. Có, Controller validate dữ liệu  
- B. Không, Controller chỉ chuyển tiếp qua MediatR ✅  
- C. Có, Controller gọi trực tiếp DbContext  
- D. Có, Controller xử lý exception  

**Câu 14:** Tại sao `Program.cs` gọi `AddApplication()` và `AddInfrastructure()` riêng biệt?  
- A. Để tăng tốc khởi động  
- B. Để mỗi layer tự quản lý DI của mình, tách biệt trách nhiệm ✅  
- C. Bắt buộc bởi .NET 8  
- D. Để tránh circular dependency  

---

## 6. Hướng dẫn thêm endpoint mới

### Ví dụ: Thêm `GET /api/products/{id}` — Lấy sản phẩm theo ID

#### Bước 1: Tạo Query và DTO

**File:** `WebDemo.Application/Features/Products/Queries/GetProductById/GetProductByIdQuery.cs`

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using WebDemo.Application.Common.Interfaces;

namespace WebDemo.Application.Features.Products.Queries.GetProductById;

// Query chứa tham số đầu vào
public record GetProductByIdQuery(int Id) : IRequest<ProductDetailDto?>;

// DTO trả về (có thể thêm field hơn ProductDto)
public class ProductDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public decimal Price { get; set; }
}

// Handler xử lý query
public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ProductDetailDto?>
{
    private readonly IApplicationDbContext _context;

    public GetProductByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ProductDetailDto?> Handle(
        GetProductByIdQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.Products
            .AsNoTracking()
            .Where(p => p.Id == request.Id)
            .Select(p => new ProductDetailDto
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
```

#### Bước 2: Thêm action vào Controller

**File:** `WebDemo.Api/Controllers/ProductsController.cs`

```csharp
using WebDemo.Application.Features.Products.Queries.GetProductById;

// Thêm vào trong class ProductsController:
[HttpGet("{id}")]
public async Task<IActionResult> GetById(int id)
{
    var result = await _mediator.Send(new GetProductByIdQuery(id));
    if (result is null)
        return NotFound();
    return Ok(result);
}
```

> **Không cần đụng gì khác.** MediatR tự scan và tìm Handler.

---

### Ví dụ: Thêm `DELETE /api/products/{id}`

#### Bước 1: Tạo Command

**File:** `WebDemo.Application/Features/Products/Commands/DeleteProduct/DeleteProductCommand.cs`

```csharp
using MediatR;
using WebDemo.Application.Common.Interfaces;

namespace WebDemo.Application.Features.Products.Commands.DeleteProduct;

public record DeleteProductCommand(int Id) : IRequest<bool>;

public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public DeleteProductCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(DeleteProductCommand request, CancellationToken ct)
    {
        var product = await _context.Products
            .FindAsync(new object[] { request.Id }, ct);

        if (product is null) return false;

        _context.Products.Remove(product);
        await _context.SaveChangesAsync(ct);
        return true;
    }
}
```

#### Bước 2: Thêm vào Controller

```csharp
using WebDemo.Application.Features.Products.Commands.DeleteProduct;

[HttpDelete("{id}")]
public async Task<IActionResult> Delete(int id)
{
    var deleted = await _mediator.Send(new DeleteProductCommand(id));
    if (!deleted) return NotFound();
    return NoContent();
}
```

---

## 7. Hướng dẫn thêm Entity mới

### Ví dụ: Thêm Entity `Category`

#### Bước 1: Tạo Entity trong Domain

**File:** `WebDemo.Domain/Entities/Category.cs`

```csharp
using WebDemo.Domain.Common;

namespace WebDemo.Domain.Entities;

public class Category : BaseEntity
{
    public string Name { get; private set; } = null!;

    private Category() { } // EF Core cần

    public static Category Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new Exceptions.DomainException("Category name is required");

        return new Category { Name = name };
    }

    public void Update(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new Exceptions.DomainException("Category name is required");
        Name = name;
    }
}
```

#### Bước 2: Thêm DbSet vào Interface

**File:** `WebDemo.Application/Common/Interfaces/IApplicationDbContext.cs`

```csharp
public interface IApplicationDbContext
{
    DbSet<Product> Products { get; }
    DbSet<Category> Categories { get; }  // ← thêm dòng này
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
```

#### Bước 3: Thêm DbSet vào DbContext

**File:** `WebDemo.Infrastructure/Persistence/ApplicationDbContext.cs`

```csharp
public DbSet<Product> Products => Set<Product>();
public DbSet<Category> Categories => Set<Category>();  // ← thêm
```

#### Bước 4: Tạo EF Configuration

**File:** `WebDemo.Infrastructure/Persistence/Configurations/CategoryConfiguration.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebDemo.Domain.Entities;

namespace WebDemo.Infrastructure.Persistence.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.Property(c => c.Name)
            .HasMaxLength(100)
            .IsRequired();
    }
}
```

#### Bước 5: Tạo Migration

```bash
cd WebDemo.Infrastructure
dotnet ef migrations add AddCategoryTable --startup-project ../WebDemo.Api
dotnet ef database update --startup-project ../WebDemo.Api
```

#### Bước 6: Tạo Command/Query cho Category

Tạo thư mục `WebDemo.Application/Features/Categories/` và làm tương tự như `Products`.

#### Bước 7: Tạo Controller

**File:** `WebDemo.Api/Controllers/CategoriesController.cs`

```csharp
[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly IMediator _mediator;
    public CategoriesController(IMediator mediator) => _mediator = mediator;

    // Thêm actions...
}
```

---

## 8. Cài đặt & chạy dự án

### Yêu cầu

- .NET 8 SDK
- PostgreSQL (hoặc dùng Neon như config sẵn trong `appsettings.json`)

### Khởi chạy

```bash
# 1. Clone / mở project
cd c:\HocTap\WebDemoCA

# 2. Restore packages
dotnet restore

# 3. Chạy migration (nếu DB chưa có bảng)
dotnet ef database update --project WebDemo.Infrastructure --startup-project WebDemo.Api

# 4. Chạy API
dotnet run --project WebDemo.Api
```

### Swagger UI

Mở trình duyệt tại: `https://localhost:{port}/swagger`

### Endpoints hiện có

| Method | URL | Mô tả |
|--------|-----|-------|
| `POST` | `/api/products` | Tạo sản phẩm mới |
| `GET` | `/api/products` | Lấy danh sách tất cả sản phẩm |

### Ví dụ request POST

```json
POST /api/products
Content-Type: application/json

{
  "name": "Táo Fuji",
  "price": 25000
}
```

**Response thành công (200):**
```json
1
```

**Response lỗi validation (400):**
```json
{
  "errors": {
    "Name": ["'Name' must not be empty."],
    "Price": ["'Price' must be greater than '0'."]
  }
}
```

---

## 🗺️ Sơ đồ phụ thuộc giữa các project

```
WebDemo.Api
    ├── depends on → WebDemo.Application
    └── depends on → WebDemo.Infrastructure

WebDemo.Application
    └── depends on → WebDemo.Domain

WebDemo.Infrastructure
    ├── depends on → WebDemo.Application (interface)
    └── depends on → WebDemo.Domain (entities)

WebDemo.Domain
    └── (không phụ thuộc ai — thuần túy C#)
```

> 💡 **Tip:** Khi thêm tính năng mới, luôn bắt đầu từ **Domain** → **Application** → **Infrastructure** → **API**. Đây là thứ tự đúng của Clean Architecture.
