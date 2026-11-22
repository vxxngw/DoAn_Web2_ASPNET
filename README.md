# Việc Mở · Quản lý tuyển dụng và kết nối việc làm

Đồ án Lập trình web 2 dùng C#, ASP.NET Core Web API trên .NET 10, EF Core Code First và SQL Server. Frontend tiếng Việt dùng HTML, CSS và JavaScript, được phục vụ cùng API từ thư mục [wwwroot](wwwroot).

## Chạy thử cả hệ thống

Trên Windows có .NET 10 SDK và SQL Server Express LocalDB, mở PowerShell trong thư mục project và chạy lệnh sau.

```powershell
dotnet run --launch-profile demo
```

Mở [Việc Mở](http://localhost:5000) hoặc [Swagger của bản demo](http://localhost:5000/swagger). Lần đầu, profile này áp dụng migration lên database riêng `RecruitmentWeb2Demo` và thêm sáu tin từ các công ty giả lập nếu database chưa có tài khoản. Những lần sau sử dụng lại dữ liệu đã lưu. Khóa JWT của profile demo được tạo trong bộ nhớ khi khởi động nên cần đăng nhập lại sau khi khởi động lại. Nhấn `Ctrl+C` trong terminal để dừng.

### Tài khoản đăng nhập demo

Mở [trang đăng nhập](http://localhost:5000/#/login) của profile `demo`.

| Vai trò | Email | Mật khẩu demo | Trang sau đăng nhập |
|---|---|---|---|
| Ứng viên (client) | `client@viecmo.test` | `ClientDemo2026!` | Tìm việc |
| Quản trị viên (admin) | `admin@viecmo.test` | `AdminDemo2026!` | Quản trị → Tài khoản người dùng |

Profile demo tạo các tài khoản còn thiếu cả khi database đã có dữ liệu. Mật khẩu được băm trước khi ghi. Nhà tuyển dụng đăng ký từ giao diện để đăng tin và nhận đơn cho công ty của mình.

### Trình bày qua giao diện

1. Lọc việc theo địa điểm, tìm một phần tên công ty rồi đổi thứ tự sắp xếp. Bấm tên vị trí để mở chi tiết.
2. Chọn **Nhà tuyển dụng**, tạo tài khoản với tên công ty của bạn. Trong **Tin của tôi**, chọn **Đăng tin mới** và nhập một hạn nộp trong tương lai. Ghi lại tên vị trí vừa đăng.
3. Mở một cửa sổ riêng tư, chọn **Đăng ký** với vai trò **Ứng viên**. Tìm tin vừa đăng, chọn một CV PDF, nhập lời nhắn rồi bấm **Gửi hồ sơ**. Màn hình chi tiết đơn hiển thị **Mới nộp**.
4. Quay lại cửa sổ nhà tuyển dụng, vào **Tin của tôi → Xem hồ sơ → Xem đơn**. Tải CV, chọn **Chấp nhận** hoặc **Từ chối** rồi xác nhận. Ở cửa sổ ứng viên, mở lại **Đơn đã nộp** để xem kết quả.
5. Mở **Chỉnh sửa tin**, bỏ chọn **Mở nhận hồ sơ** và lưu. Tin rời khỏi danh sách công khai. Ứng viên vẫn xem được đơn đã gửi. Tin chưa có hồ sơ có thể xóa sau khi xác nhận.

Các công ty được tạo sẵn dùng để minh họa tìm kiếm và ứng tuyển. Hãy tạo nhà tuyển dụng của bạn ở bước 2 để thực hành toàn bộ luồng xét đơn. Với database đồ án của riêng bạn, dùng profile HTTPS và các bước cấu hình bên dưới.

## Vai trò và nghiệp vụ

| Người dùng | Quyền |
|---|---|
| Khách | Tìm việc, lọc theo địa điểm, xem chi tiết tin đang mở và còn hạn. |
| Ứng viên (`Candidate`) | Nộp CV PDF cùng lời nhắn, xem lại CV và theo dõi kết quả của đơn mình đã nộp. |
| Nhà tuyển dụng (`Employer`) | Đăng tin trực tiếp, sửa, đóng hoặc xóa tin của mình. Xem hồ sơ và quyết định kết quả cho từng đơn thuộc tin đó. |
| Quản trị viên (`Admin`) | Tra cứu và khóa/mở khóa ứng viên, nhà tuyển dụng. Xem mọi tin, chỉnh sửa, đóng/mở hoặc xóa tin chưa có đơn. |

Một tài khoản có một vai trò. Tên công ty gắn với tài khoản nhà tuyển dụng. Mỗi ứng viên được nộp một lần cho mỗi tin. Thời điểm đóng nhận hồ sơ là thời điểm sớm hơn giữa lúc chủ tin đóng và hạn nộp. Tin có đơn được giữ lại để ứng viên tra cứu kết quả.

Đơn bắt đầu ở `Submitted` (Mới nộp). Chủ tin có thể chuyển đơn một lần sang `Accepted` (Chấp nhận) hoặc `Rejected` (Từ chối). CV tối đa 5 MiB (5.242.880 byte). API kiểm tra phần mở rộng `.pdf`, kích thước và dấu hiệu `%PDF-` ở đầu tệp. API đặt tên tải về theo mã đơn.

## Chạy trên Windows

Mở PowerShell tại thư mục chứa [Recruitment.Api.csproj](Recruitment.Api.csproj). Máy cần .NET 10 SDK và một instance SQL Server đang hoạt động. Cấu hình đi kèm sử dụng SQL Server Express LocalDB với tài khoản Windows hiện tại.

1. Cài các package đã khai báo trong project và công cụ EF theo đúng phiên bản trong [.config/dotnet-tools.json](.config/dotnet-tools.json).

   ```powershell
   dotnet --version
   dotnet restore
   dotnet tool restore
   ```

   `Microsoft.EntityFrameworkCore.SqlServer` cung cấp trình kết nối SQL Server cho EF. `Microsoft.EntityFrameworkCore.Design` hỗ trợ tạo migration. `Microsoft.AspNetCore.Authentication.JwtBearer` xác thực JWT. `Swashbuckle.AspNetCore` tạo tài liệu và giao diện Swagger. Project ghim bản vá `System.Security.Cryptography.Pkcs` cho phụ thuộc của trình kết nối SQL Server. `PasswordHasher<User>` có sẵn trong ASP.NET Core. [Tài liệu EF Core cho SQL Server](https://learn.microsoft.com/en-us/ef/core/providers/sql-server/)

2. Tạo khóa ký JWT riêng cho máy. Các lệnh này dùng được trong Windows PowerShell. User Secrets lưu giá trị ở hồ sơ người dùng. Profile khởi chạy của project đặt môi trường là `Development` để đọc giá trị đó.

   ```powershell
   $keyBytes = New-Object byte[] 48
   $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
   $rng.GetBytes($keyBytes)
   $rng.Dispose()
   dotnet user-secrets set "Jwt:Key" ([Convert]::ToBase64String($keyBytes))
   ```

   [appsettings.json](appsettings.json) chứa `Jwt:Issuer`, `Jwt:Audience` và `Jwt:LifetimeMinutes` (mặc định 60 phút). Ở môi trường triển khai, có thể cung cấp khóa qua biến môi trường `Jwt__Key`. API kiểm tra cấu hình khi khởi động.

3. Chọn instance. Chuỗi mẫu mặc định là `Server=(localdb)\MSSQLLocalDB;Database=RecruitmentWeb2;Trusted_Connection=True;TrustServerCertificate=True`. Với LocalDB, dùng lệnh sau.

   ```powershell
   sqllocaldb start MSSQLLocalDB
   ```

   Nếu dùng SQL Server Express với instance tên `SQLEXPRESS`, thay bước khởi động LocalDB bằng cách trỏ API đến instance đó. Thay tên máy và instance nếu cần. Tài khoản chạy migration cần có quyền tạo database hoặc cập nhật schema trên database đã tạo.

   ```powershell
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" 'Server=.\SQLEXPRESS;Database=RecruitmentWeb2;Trusted_Connection=True;TrustServerCertificate=True'
   ```

4. Áp dụng migration `InitialCreate` đã có trong [Migrations](Migrations). EF tạo bảng và ghi migration vào `__EFMigrationsHistory`.

   ```powershell
   dotnet ef migrations list
   dotnet ef database update
   ```

   Khi tự sửa model, tạo migration mang tên mô tả thay đổi, đọc hai phương thức `Up` và `Down` rồi chạy `dotnet ef database update`. Ví dụ, sau khi thêm cột lương vào model, dùng `dotnet ef migrations add AddJobSalary`. Có thể kiểm tra model còn thay đổi chưa có migration bằng `dotnet ef migrations has-pending-model-changes`.

5. Tin cậy chứng chỉ HTTPS cho máy phát triển, rồi chạy API.

   ```powershell
   dotnet dev-certs https --trust
   dotnet run --launch-profile https
   ```

   Mở [giao diện web](https://localhost:7148), [Swagger UI](https://localhost:7148/swagger) hoặc [tài liệu OpenAPI JSON](https://localhost:7148/swagger/v1/swagger.json). Cổng nằm trong [Properties/launchSettings.json](Properties/launchSettings.json). Swagger được bật ở môi trường `Development`. [Swagger UI trong ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/using-openapi-documents?view=aspnetcore-10.0)

## Thiết kế database

Migration `AddUserLock` thêm cột `Users.IsLocked` với mặc định `false` cho dữ liệu hiện có. Khi tài khoản bị khóa, đăng nhập bị từ chối và JWT đang dùng mất quyền truy cập API bảo vệ. Tin của nhà tuyển dụng bị khóa ẩn khỏi danh sách công khai và ngừng nhận hồ sơ cho đến khi tài khoản được mở khóa và tin còn hạn. Quyền đọc CV vẫn thuộc về ứng viên nộp đơn và chủ tin.

| Bảng | Khóa và các cột nghiệp vụ | Ràng buộc |
|---|---|---|
| `Users` | `Id` (PK), `Email`, `PasswordHash`, `FullName`, `Role`, `CompanyName` | Email được chuẩn hóa về chữ thường và có chỉ mục duy nhất. Tên công ty bắt buộc khi đăng ký nhà tuyển dụng. |
| `Jobs` | `Id` (PK), `EmployerId` (FK tới Users), `Title`, `Description`, `Location`, `Deadline`, `IsOpen`, `CreatedAt`, `RowVersion` | Một nhà tuyển dụng có nhiều tin. EF dùng `rowversion` để phát hiện có thao tác khác vừa thay đổi tin trong lúc request đang xử lý. |
| `Applications` | `Id` (PK), `JobId` (FK tới Jobs), `CandidateId` (FK tới Users), `Message`, `CvPdf`, `Status`, `SubmittedAt` | Cặp `(JobId, CandidateId)` duy nhất. Khóa ngoại ngăn xóa tin khi đã có đơn. |

Một ứng viên có thể ứng tuyển nhiều tin và một tin có nhiều ứng viên. Bảng `Applications` biểu diễn quan hệ nhiều–nhiều này và lưu dữ liệu riêng của mỗi lần ứng tuyển. EF lưu vai trò, trạng thái dưới dạng chuỗi, thời gian bằng `datetimeoffset`, nội dung PDF bằng `varbinary(max)`. Repository chỉ đọc cột PDF khi có yêu cầu tải CV.

## Danh sách API

Các body và kết quả thông thường dùng JSON. Riêng thao tác nộp đơn gửi `multipart/form-data` để mang tệp, và tải CV trả `application/pdf`. API lấy danh tính người thao tác từ trường `sub` trong JWT.

| Method | Đường dẫn | Nhận | Trả khi thành công | Quyền |
|---|---|---|---|---|
| POST | `/api/auth/register` | `email, password, fullName, role`, thêm `companyName` cho Employer | 201, thông tin tài khoản | Công khai |
| POST | `/api/auth/login` | `email, password` | 200, `accessToken, expiresAt, user` | Công khai |
| GET | `/api/auth/me` | JWT | 200, thông tin tài khoản | Đã đăng nhập |
| GET | `/api/admin/users` | `keyword, page, pageSize` | 200, một trang tài khoản và trạng thái khóa | Admin |
| PATCH | `/api/admin/users/{id}/lock` | `{"isLocked":true}` hoặc `false` trong cùng trường | 204 | Admin, đối tượng là ứng viên/nhà tuyển dụng |
| GET | `/api/admin/jobs` | Bộ tham số tìm tin | 200, một trang tin ở mọi trạng thái | Admin |
| GET | `/api/jobs` | Bộ tham số tìm tin | 200, một trang tin đang nhận đơn | Công khai |
| GET | `/api/jobs/{id}` | Mã tin | 200, chi tiết tin | Công khai với tin đang nhận đơn. Chủ tin và Admin xem được tin đóng hoặc hết hạn |
| GET | `/api/jobs/mine` | Bộ tham số tìm tin | 200, một trang tin của mình ở mọi trạng thái | Employer |
| POST | `/api/jobs` | Nội dung tin | 201, `{id}` và header `Location` | Employer |
| PUT | `/api/jobs/{id}` | Mã tin, toàn bộ nội dung tin, `rowVersion` từ lần đọc gần nhất nếu có | 204 | Chủ tin hoặc Admin |
| DELETE | `/api/jobs/{id}` | Mã tin | 204 nếu chưa có đơn | Chủ tin hoặc Admin |
| POST | `/api/jobs/{jobId}/applications` | Form gồm `cv` (tệp), `message` (lời nhắn) | 201, `{id}` và header `Location` | Candidate |
| GET | `/api/applications/mine` | Bộ tham số lọc đơn | 200, một trang đơn của mình | Candidate |
| GET | `/api/jobs/{jobId}/applications` | Mã tin, bộ tham số lọc đơn | 200, một trang đơn của tin | Chủ tin |
| GET | `/api/applications/{id}` | Mã đơn | 200, chi tiết đơn | Người nộp hoặc chủ tin |
| GET | `/api/applications/{id}/cv` | Mã đơn | 200, tệp `cv-{id}.pdf` | Người nộp hoặc chủ tin |
| PATCH | `/api/applications/{id}/status` | `{"status":"Accepted"}` hoặc `{"status":"Rejected"}` | 204 | Chủ tin |

- **Nội dung tin** gồm `title, description, location, deadline, isOpen`. Gửi hạn theo ISO 8601 có múi giờ, ví dụ `2030-06-30T17:00:00+07:00`. Tin đang mở cần hạn ở tương lai. Gửi `isOpen: false` khi đóng tin.
- **Bộ tham số tìm tin** gồm `keyword` (một phần tiêu đề hoặc tên công ty), `location`, `sort` (`Newest` mặc định, `Deadline` gần nhất trước, `Title` từ A đến Z), `page`, `pageSize`. Ví dụ `/api/jobs?keyword=Backend&location=HCM&sort=Deadline&page=1&pageSize=5`.
- **Bộ tham số lọc đơn** gồm `status`, `jobId`, `page`, `pageSize`. Giao diện ứng viên dùng `/api/applications/mine?jobId=12` để kiểm tra tài khoản đã ứng tuyển tin 12 chưa. Danh sách đơn sắp theo ngày nộp mới nhất. Mọi danh sách phân trang có cấu trúc `{items, page, pageSize, totalCount}`. Mặc định trang 1, 10 dòng, tối đa 100 dòng/trang.
- **Thông tin tài khoản** gồm mã, email, họ tên, vai trò và tên công ty. **Chi tiết tin** thêm mã tin, mã nhà tuyển dụng, công ty và ngày tạo vào nội dung tin. **Chi tiết đơn** gồm mã đơn, mã và tiêu đề tin, mã, tên, email ứng viên, lời nhắn, trạng thái và ngày nộp.

### Đọc kết quả lỗi

| HTTP | Ví dụ |
|---|---|
| 400 | Thiếu tên công ty của Employer, email sai định dạng, trang bằng 0, tệp sai định dạng, hoặc tin mở có hạn trong quá khứ. |
| 401 | Sai email/mật khẩu, thiếu token ở API cần đăng nhập hoặc token hết hạn. |
| 403 | Ứng viên đăng tin, nhà tuyển dụng nộp đơn, nhà tuyển dụng sửa tin của người khác. |
| 404 | Không tìm thấy tin, hoặc người gọi tra cứu đơn/CV riêng của người khác. |
| 409 | Email trùng, nộp lần hai, tin đã đóng/hết hạn, xóa tin đã có đơn, xử lý lại đơn đã có kết quả hoặc lưu từ phiên bản tin cũ. |
| 413 | CV vượt 5 MiB hoặc toàn bộ form nộp đơn vượt 6 MiB. |

Lỗi nghiệp vụ trả đối tượng `ProblemDetails` với `title` và `status`. Với lỗi kiểm tra DTO, đọc thêm `errors` để biết trường nào cần sửa. Phản hồi 204 có body rỗng.

## Kiểm thử tự động backend và Swagger UI

Trên Windows cần .NET 10 SDK, SQL Server Express LocalDB, Node.js từ 20 và Chrome hoặc Edge. Mở PowerShell ở thư mục gốc và chạy lệnh sau.

```powershell
.\Tests\Run-Tests.ps1
```

Dùng `.\Tests\Run-Tests.ps1 -Browser msedge` nếu máy dùng Edge. Script khôi phục các package kiểm thử, build cấu hình `Integration` và chạy xUnit. Lần đầu cần kết nối NuGet và npm. Package, tệp tạm và dữ liệu kiểm thử được đặt dưới thư mục `Tests` trên ổ chứa project.

Mỗi lần chạy, [ApiFixture](Tests/ApiFixture.cs) mở Kestrel trên cổng loopback ngẫu nhiên, tạo một database LocalDB mới, áp dụng migration, kiểm tra tài khoản có từ schema cũ rồi dọn database sau khi chạy. Không cần bật server demo trước.

| Nhóm | Điều được kiểm tra |
|---|---|
| [Tài khoản và quản trị](Tests/AccountTests.cs) | Kiểm tra đầu vào, vai trò được tự đăng ký, email trùng, băm mật khẩu, JWT, khóa và mở lại tài khoản. |
| [Tin tuyển dụng](Tests/JobTests.cs) | Tìm kiếm, sắp xếp, phân trang, thống kê công khai, hạn nộp, quyền sở hữu và xung đột phiên bản. |
| [Đơn và CV](Tests/ApplicationTests.cs) | Định dạng, kích thước file/form, nộp đồng thời, xét đồng thời, quyền đọc và đối chiếu byte của CV tải về. |
| [Swagger](Tests/SwaggerTests.cs) | Đối chiếu tài liệu với 19 thao tác HTTP và kiểm tra yêu cầu đăng nhập. [Kịch bản trình duyệt](Tests/swagger-ui.cjs) bấm **Try it out**, **Execute**, **Authorize**, nộp PDF, thử nộp trùng và tải lại chính tệp đó. |

## Kịch bản bảo vệ trên Swagger

Với database riêng dùng profile HTTPS, áp dụng migrations rồi tạo admin bằng lệnh cục bộ dưới đây trong PowerShell 7. Dùng email mới và nhập mật khẩu riêng khi được hỏi. Lệnh kết thúc sau khi tạo tài khoản. API đăng ký công khai chỉ nhận `Candidate` và `Employer`.

```powershell
$env:AdminSetup__Email = Read-Host "Email admin"
$env:AdminSetup__Password = Read-Host "Mật khẩu admin" -MaskInput
try { dotnet run --launch-profile https -- --create-admin }
finally { Remove-Item Env:AdminSetup__Email, Env:AdminSetup__Password }
```

1. Mở `POST /api/auth/register` → **Try it out**. Tạo hai tài khoản bằng hai body mẫu bên dưới. Mật khẩu trong ví dụ dành cho dữ liệu demo.

   ```json
   {"email":"hr@example.com","password":"Demo-Hr-2026","fullName":"Nguyễn An","role":"Employer","companyName":"Công ty Sao Mai"}
   ```

   ```json
   {"email":"lan@example.com","password":"Demo-Lan-2026","fullName":"Trần Lan","role":"Candidate"}
   ```

2. Gọi `POST /api/auth/login` bằng email và mật khẩu của nhà tuyển dụng. Sao chép giá trị `accessToken`, bấm **Authorize** và dán trực tiếp token. Swagger tự thêm tiền tố `Bearer`. Gọi `GET /api/auth/me` để xác nhận tài khoản đang dùng.

3. Gọi `POST /api/jobs` với body sau. Chọn hạn còn ở tương lai khi chạy. Ghi lại `id` trong kết quả 201 để dùng làm `jobId`. Gọi `GET /api/jobs?keyword=Backend` để thấy tin hiển thị ngay.

   ```json
   {
     "title": "Thực tập sinh Backend C#",
     "description": "Xây dựng API và viết truy vấn SQL cùng nhóm tuyển dụng.",
     "location": "TP.HCM",
     "deadline": "2030-06-30T17:00:00+07:00",
     "isOpen": true
   }
   ```

4. Đăng nhập bằng tài khoản ứng viên. Trong **Authorize**, bấm **Logout** rồi thay bằng token của ứng viên. Mở `POST /api/jobs/{jobId}/applications`, nhập mã tin, chọn tệp PDF, nhập lời nhắn và bấm **Execute**. Ghi lại mã đơn. Thử nộp lại để thấy lỗi 409.
5. Dùng lại token của nhà tuyển dụng. Mở danh sách đơn của tin, tải CV theo mã đơn rồi gọi `PATCH /api/applications/{id}/status` với `{"status":"Accepted"}`. Chuyển về token của ứng viên và gọi `GET /api/applications/mine?status=Accepted` để xem kết quả.
6. Để trình bày phân quyền, thử `POST /api/jobs` bằng token ứng viên (403). Đăng ký một Employer khác rồi thử tải CV của đơn vừa tạo (404). Dùng lại token chủ tin và thử xóa tin (409), sau đó dùng `PUT` với đầy đủ nội dung tin và `isOpen: false`. Tin biến mất khỏi danh sách công khai nhưng vẫn có trong danh sách của chủ tin.

## Đọc code theo luồng xử lý

| Chức năng | Controller → Service → Repository | Điều cần giải thích |
|---|---|---|
| Đăng ký, đăng nhập | [AuthController](Controllers/AuthController.cs) → [AuthService](Services/AuthService.cs) → [UserRepository](Repositories/UserRepository.cs) | Service dùng `PasswordHasher` khi đăng ký, kiểm tra mật khẩu khi đăng nhập rồi tạo JWT có mã người dùng, vai trò và hạn dùng. |
| Quản lý tin | [JobsController](Controllers/JobsController.cs) → [JobService](Services/JobService.cs) → [JobRepository](Repositories/JobRepository.cs) | Controller kiểm tra vai trò. Service kiểm tra người đăng và hạn nhận đơn. Repository dùng LINQ để tạo truy vấn SQL. |
| Nộp và xét đơn | [ApplicationsController](Controllers/ApplicationsController.cs) → [ApplicationService](Services/ApplicationService.cs) → [ApplicationRepository](Repositories/ApplicationRepository.cs) | Service kiểm tra CV, gán người nộp từ JWT và trạng thái ban đầu. Repository kiểm tra tin rồi ghi đơn và CV trong cùng transaction. Khi xét đơn, lệnh cập nhật chỉ khớp đơn còn ở trạng thái mới và thuộc tin của người gọi. |

Cả ba repository nhận [AppDbContext](Data/AppDbContext.cs). EF Core chuyển truy vấn sang SQL và ánh xạ kết quả về đối tượng C#. Các lớp trong [Models](Models) biểu diễn dữ liệu lưu trữ. Các lớp trong [DTOs](DTOs) mô tả những trường client được gửi và được nhận.

- **Interface và Dependency Injection.** `IJobRepository` mô tả các thao tác dữ liệu mà service cần. [Program.cs](Program.cs) nối interface với `JobRepository` bằng `AddScoped`. ASP.NET Core truyền đối tượng qua constructor và dùng chung một `AppDbContext` trong mỗi request.
- **Kiểm tra đầu vào.** `[Required]`, `[EmailAddress]`, `[MaxLength]` và `[Range]` đặt ngay trên DTO. `[ApiController]` tự trả 400 khi DTO không hợp lệ. Quy tắc phụ thuộc chủ sở hữu hoặc trạng thái hiện tại được kiểm tra trong luồng nghiệp vụ.
- **LINQ và async/await.** Trong [JobRepository](Repositories/JobRepository.cs), `Where` lọc, `OrderBy` sắp xếp và `Select` chọn cột. [PagingExtensions](Repositories/PagingExtensions.cs) thực hiện `CountAsync`, `Skip`, `Take`, `ToListAsync`. `Task<T>` biểu diễn kết quả sẽ có sau khi SQL trả lời. `await` chờ kết quả để request tiếp tục mà không giữ luồng chờ I/O.
- **Các kiểu C# thường gặp.** `enum` giới hạn các lựa chọn như `UserRole`. `record` biểu diễn DTO kết quả gọn. `int?` cho phép có hoặc thiếu một giá trị. `CancellationToken` được truyền xuống lệnh SQL để có thể dừng tác vụ khi request bị hủy.
- **Tính nhất quán.** Chỉ mục duy nhất trong SQL Server bảo vệ quy tắc mỗi ứng viên một đơn cho mỗi tin ngay cả khi bấm nộp hai lần đồng thời. Transaction giữ trạng thái của tin ổn định trong lúc ghi đơn. Điều kiện trạng thái trong lệnh xét đơn giúp hai yêu cầu đến cùng lúc chỉ có một quyết định được ghi.

## Đọc code frontend

| Tệp | Trách nhiệm |
|---|---|
| [index.html](wwwroot/index.html), [styles.css](wwwroot/styles.css) | Khung trang, thanh điều hướng, hộp xác nhận và bố cục cho máy tính, điện thoại. |
| [app.js](wwwroot/js/app.js) | Chọn màn hình theo phần sau dấu `#` trong URL. Nút Back của trình duyệt quay lại màn hình trước. Khi đổi màn hình, `AbortController` hủy các yêu cầu đọc cũ. |
| [api.js](wwwroot/js/api.js) | Gọi API bằng `fetch`, thêm JWT Bearer và đọc lỗi HTTP. Token được giữ trong `sessionStorage` của tab. Khi tải lại trang, frontend dùng `/api/auth/me` để lấy danh tính. |
| [account.js](wwwroot/js/account.js) | Đăng ký và đăng nhập. Form nhà tuyển dụng yêu cầu thêm tên công ty. |
| [admin.js](wwwroot/js/admin.js) | Danh sách tài khoản, tìm kiếm theo tên hoặc email, xác nhận khóa và mở khóa. Trang quản trị tin dùng chung form sửa tin và gửi JWT của Admin. |
| [jobs.js](wwwroot/js/jobs.js) | Hiển thị danh sách và chi tiết tin. Khi ứng tuyển, `FormData` gửi tệp và lời nhắn tới controller. |
| [employer.js](wwwroot/js/employer.js) | Tạo, sửa, đóng và xóa tin. Form sửa gửi `rowVersion` đã đọc cùng tin. Nếu một tab khác vừa lưu, API trả 409 và người dùng có thể tải lại phiên bản mới. |
| [applications.js](wwwroot/js/applications.js) | Lọc đơn, xem chi tiết, tải CV có xác thực và gửi quyết định xét hồ sơ. |
| [ui.js](wwwroot/js/ui.js) | Định dạng ngày, phân trang, xác nhận thao tác và mã hóa ký tự trước khi đưa nội dung người dùng nhập vào HTML. |

Ví dụ khi bấm **Gửi hồ sơ**, form trong `jobs.js` gọi `api.js`, trình duyệt gửi HTTP POST đến `ApplicationsController`, sau đó `ApplicationService` kiểm tra CV và `ApplicationRepository` ghi vào SQL Server. Frontend nhận mã đơn từ HTTP 201 và chuyển sang trang chi tiết. Quyết định cho phép thao tác luôn được kiểm tra ở API.

Để đóng gói giao diện và API cùng nhau, chạy `dotnet publish -c Release`. .NET sao chép `wwwroot` vào thư mục publish. Frontend dùng đường dẫn tương đối cùng origin cho các API nên có thể giữ nguyên mã JavaScript khi đổi cổng chạy.

Hình nền trang tìm việc nằm ở [career-steps.webp](wwwroot/assets/career-steps.webp), tạo bằng ImageGen tích hợp từ mô tả bậc thang xanh cobalt, cổng vòm xanh nhạt và quả cầu cam. Nội dung chữ và ô tìm kiếm được dựng bằng HTML.

#   D o A n _ W e b 2 _ A S P N E T  
 