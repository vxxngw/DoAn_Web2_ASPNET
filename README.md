# Việc Mở · Quản lý tuyển dụng và kết nối việc làm

Đồ án Lập trình web 2 dùng C#, ASP.NET Core Web API trên .NET 10, EF Core Code First và SQL Server. Frontend tiếng Việt dùng HTML, CSS và JavaScript, được phục vụ cùng API từ thư mục [wwwroot](wwwroot).

Contributor push lên nhánh riêng và mở pull request vào `main`. Người quản lý repository thực hiện merge. [Quy trình đóng góp và cách bật bảo vệ nhánh](CONTRIBUTING.md)

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

Khi khởi tạo tài khoản admin demo lần đầu, hệ thống cũng tạo tài khoản ứng viên mẫu. Nếu admin sau đó sửa email hoặc xóa ứng viên này, lần khởi động tiếp theo giữ quyết định đó. Mật khẩu được băm trước khi ghi.

### Nạp thêm 500 bản ghi để kiểm thử

Chạy lệnh sau trong thư mục project. Lệnh ghi dữ liệu vào `RecruitmentWeb2Demo` rồi kết thúc. Nếu web đang chạy ở profile demo, tải lại trang để thấy dữ liệu.

```powershell
dotnet run --launch-profile demo -- --seed-test-data
```

Bộ mẫu có 30 nhà tuyển dụng (`ntd001@seed500.test` đến `ntd030@seed500.test`), 100 ứng viên (`uv001@seed500.test` đến `uv100@seed500.test`), 200 tin và 170 đơn kèm lời nhắn. Các tài khoản này dùng mật khẩu demo `DemoData500!`. Mỗi đơn lưu một bản của [CV mẫu](Data/Samples/cv-mau.pdf). EF Core ghi toàn bộ trong một giao dịch. Chạy lại lệnh sẽ bỏ qua bộ đã nạp, kể cả khi bạn đã xét đơn hoặc chỉnh sửa tin.

Để thử hai phía của cùng một đơn, đăng nhập bằng `uv001@seed500.test` và mở **Đơn đã nộp**. Đơn cho vị trí **Lập trình viên Backend C#** tại **Công nghệ Mộc Lan** ban đầu ở trạng thái **Mới nộp**. Đăng nhập bằng `ntd001@seed500.test` ở cửa sổ riêng tư, mở tin này trong **Tin của tôi**, tải CV và xét đơn.

Bộ seed có cả khoảng lương theo tháng và mức **Thỏa thuận**. Bốn công ty mẫu có logo riêng là Công nghệ Bình Minh, Công nghệ Mộc Lan, Công nghệ Sao Mai và Dịch vụ Bình Minh. Trang chủ xếp hạng công ty theo số tin đang tuyển.

Nếu đã có dữ liệu mẫu, chạy `dotnet run --launch-profile demo -- --enrich-test-data` để bổ sung lương và logo còn trống. Với SQL Server của đồ án, dùng `--launch-profile https` để chọn `DefaultConnection`. Lệnh cập nhật các tin khớp email nhà tuyển dụng, tiêu đề và mô tả gốc của bộ mẫu. Có thể chạy lại sau khi đã chỉnh lương, thay logo hoặc xóa tài khoản.

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
| Quản trị viên (`Admin`) | Sửa họ tên, email đăng nhập, tên công ty; khóa/mở khóa hoặc xóa vĩnh viễn ứng viên và nhà tuyển dụng. Chỉnh sửa, đóng/mở, xóa mọi tin và xem thống kê tuyển dụng. |

Một tài khoản có một vai trò. Tên công ty gắn với tài khoản nhà tuyển dụng. Mỗi ứng viên được nộp một lần cho mỗi tin. Thời điểm đóng nhận hồ sơ là thời điểm sớm hơn giữa lúc chủ tin đóng và hạn nộp. Chủ tin có thể chỉnh sửa và mở lại tin đã đóng khi hạn nộp còn ở tương lai. Nếu chủ tin xóa tin đã nhận đơn, tin chuyển sang lưu trữ và lịch sử đơn vẫn xem được trong Báo cáo.

### Quản trị tài khoản, tin và báo cáo

Trong **Quản trị → Tài khoản người dùng**, chọn **Sửa** để đổi thông tin hoặc **Xóa** để xem số tin và đơn liên quan. Xóa ứng viên sẽ xóa các đơn và CV họ đã gửi. Xóa nhà tuyển dụng sẽ xóa mọi tin của họ cùng các đơn và CV của những tin đó. Trong **Tin tuyển dụng → Quản lý tin**, admin cũng có thể xóa riêng một tin cùng các đơn của tin. Sau khi admin xác nhận, API xóa các bản ghi liên quan trong một giao dịch SQL Server. API bảo vệ các tài khoản có vai trò admin.

Trong **Quản trị → Thống kê**, chọn **Ngày**, **Tuần**, **Tháng** hoặc **Toàn thời gian**. Chọn một ngày để xem lại kỳ chứa ngày đó, tính theo UTC+7. Tuần bắt đầu từ thứ Hai. Tin đăng tính theo `CreatedAt`, đơn nộp theo `SubmittedAt`, đơn chấp nhận/từ chối theo `ReviewedAt`. Migration `AddApplicationReviewTime` thêm cột thời điểm xét duyệt. Quyết định cũ chưa có ngày được tính vào toàn thời gian và được ghi chú trên báo cáo. Báo cáo chỉ đếm các bản ghi hiện còn trong database.

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

4. Áp dụng các migration trong [Migrations](Migrations). EF tạo bảng, cập nhật cấu trúc và ghi phiên bản vào `__EFMigrationsHistory`.

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
| `Users` | `Id` (PK), `Email`, `PasswordHash`, `FullName`, `Role`, `CompanyName`, `IsLocked`, `AvatarImage` | Email được chuẩn hóa về chữ thường và có chỉ mục duy nhất. Ảnh đại diện là dữ liệu nhị phân tùy chọn. |
| `Jobs` | `Id` (PK), `EmployerId` (FK tới Users), `Title`, `Description`, `Location`, `Industry`, `Deadline`, `IsOpen`, `CreatedAt`, `RowVersion` | Ngành nghề là tùy chọn. EF dùng `rowversion` để phát hiện thay đổi đồng thời. |
| `SavedJobs` | Khóa ghép `CandidateId, JobId`, thời điểm `SavedAt` | Mỗi ứng viên chỉ lưu một lần cho mỗi tin. Xóa tài khoản hoặc tin sẽ xóa lượt lưu tương ứng. |
| `Applications` | `Id` (PK), `JobId` (FK tới Jobs), `CandidateId` (FK tới Users), `Message`, `CvPdf`, `Status`, `SubmittedAt`, `ReviewedAt` | Cặp `(JobId, CandidateId)` duy nhất. Khi admin xóa tin, API xóa đơn trước để bảo đảm khóa ngoại. |

Một ứng viên có thể ứng tuyển nhiều tin và một tin có nhiều ứng viên. Bảng `Applications` biểu diễn quan hệ nhiều–nhiều này và lưu dữ liệu riêng của mỗi lần ứng tuyển. EF lưu vai trò, trạng thái dưới dạng chuỗi, thời gian bằng `datetimeoffset`, nội dung PDF bằng `varbinary(max)`. Repository chỉ đọc cột PDF khi có yêu cầu tải CV.

## Danh sách API

Các yêu cầu và kết quả thông thường dùng JSON. Các thao tác tải tệp lên dùng `multipart/form-data`. API lấy danh tính người thao tác từ trường `sub` trong JWT.

| Method | Đường dẫn | Nhận | Trả khi thành công | Quyền |
|---|---|---|---|---|
| POST | `/api/auth/register` | `email, password, fullName, role`, thêm `companyName` cho Employer | 201, thông tin tài khoản | Công khai |
| POST | `/api/auth/login` | `email, password` | 200, `accessToken, expiresAt, user` | Công khai |
| GET | `/api/auth/me` | JWT | 200, thông tin tài khoản | Đã đăng nhập |
| PUT | `/api/auth/me` | `fullName`, thêm `companyName` cho nhà tuyển dụng | 200, hồ sơ đã cập nhật | Chính chủ |
| PUT | `/api/auth/me/password` | `currentPassword`, `newPassword` (8–128 ký tự), `confirmNewPassword` khớp với mật khẩu mới | 204 | Chính chủ, mọi vai trò |
| PUT | `/api/auth/me/avatar` | Form có trường `image` dạng JPEG hoặc PNG, tối đa 256 KiB | 200, hồ sơ kèm `avatarDataUrl` | Chính chủ |
| PUT | `/api/companies/me/logo` | Form có trường `image` dạng JPEG hoặc PNG, tối đa 1 MiB | 204, lưu hoặc thay logo công ty của tài khoản | Employer |
| GET | `/api/companies/{id}/logo` | Mã nhà tuyển dụng | 200, ảnh JPEG/PNG. Trả 404 nếu chưa có logo hoặc công ty bị khóa | Công khai |
| GET | `/api/admin/users` | `role` (tùy chọn), `keyword` (tên, email hoặc công ty), `page, pageSize` | 200, danh sách tài khoản và tổng số khớp bộ lọc. Trang quản trị phân trang riêng cho bảng ứng viên và bảng nhà tuyển dụng. | Admin |
| GET | `/api/admin/users/{id}` | Mã tài khoản | 200, thông tin hồ sơ | Admin |
| PUT | `/api/admin/users/{id}` | `fullName, email`, thêm `companyName` cho nhà tuyển dụng | 204 | Admin, đối tượng là ứng viên/nhà tuyển dụng |
| DELETE | `/api/admin/users/{id}` | Mã tài khoản | 204, đã xóa tài khoản cùng dữ liệu liên quan | Admin, đối tượng là ứng viên/nhà tuyển dụng |
| GET | `/api/admin/users/{id}/deletion-impact` hoặc `/api/admin/jobs/{id}/deletion-impact` | Mã tài khoản hoặc tin | 200, `{jobs, applications}` tại lúc xem | Admin |
| GET | `/api/admin/statistics` | `period=Day/Week/Month/All` (mặc định Day), `date=YYYY-MM-DD` (mặc định hôm nay) | 200, khoảng ngày, số tin đăng, đơn nộp, số chấp nhận/từ chối và số kết quả thiếu ngày | Admin |
| PATCH | `/api/admin/users/{id}/lock` | `{"isLocked":true}` hoặc `false` trong cùng trường | 204 | Admin, đối tượng là ứng viên/nhà tuyển dụng |
| GET | `/api/admin/jobs` | Bộ tham số tìm tin | 200, một trang tin ở mọi trạng thái | Admin |
| GET | `/api/jobs` | Bộ tham số tìm tin | 200, một trang tin đang nhận đơn | Công khai |
| GET | `/api/jobs/top-companies` | — | 200, tối đa 4 công ty có nhiều tin đang nhận đơn nhất, gồm mã nhà tuyển dụng, tên, số tin và `hasCompanyLogo` | Công khai |
| GET | `/api/jobs/industries` | — | 200, các ngành có tin đang tuyển và số tin theo ngành, sắp giảm dần | Công khai |
| GET | `/api/saved-jobs` | `page, pageSize` | 200, việc đã lưu của ứng viên, mới lưu trước | Candidate |
| PUT | `/api/saved-jobs/{jobId}` | Mã tin đang nhận hồ sơ | 204, đã lưu. Gửi lại vẫn chỉ có một lượt lưu | Candidate |
| DELETE | `/api/saved-jobs/{jobId}` | Mã tin | 204, đã bỏ lưu | Candidate |
| GET | `/api/jobs/{id}` | Mã tin | 200, chi tiết tin | Công khai với tin đang nhận đơn. Chủ tin và admin xem được cả tin đã đóng, hết hạn hoặc lưu trữ |
| GET | `/api/jobs/mine` | Bộ tham số tìm tin | 200, một trang tin chưa lưu trữ của mình | Employer |
| POST | `/api/jobs` | Nội dung tin | 201, `{id}` và header `Location` | Employer |
| PUT | `/api/jobs/{id}` | Mã tin, toàn bộ nội dung tin, `rowVersion` từ lần đọc gần nhất nếu có | 204 | Chủ tin hoặc Admin |
| DELETE | `/api/jobs/{id}` | Mã tin | 204 | Chủ tin xóa hẳn tin chưa có đơn hoặc lưu trữ tin đã nhận đơn. Admin được xóa cả tin, đơn và CV |
| PUT | `/api/auth/me/candidate-profile` | JSON gồm `isProfilePublic`, `professionalTitle`, `profileSummary` | 200, tài khoản đã cập nhật | Ứng viên |
| GET | `/api/employer/candidates` và `/api/employer/candidates/{id}` | Từ khóa và phân trang cho danh sách, mã ứng viên cho chi tiết | 200, hồ sơ gồm tên, email, vị trí mong muốn và giới thiệu | Nhà tuyển dụng, chỉ xem được hồ sơ chủ động bật công khai |
| GET | `/api/employer/applications` | `keyword, jobId, status, page, pageSize` | 200, danh sách đơn của công ty | Nhà tuyển dụng |
| GET | `/api/employer/report` | `page, pageSize` | 200, tổng đơn, số tin theo trạng thái và số đơn từng tin | Nhà tuyển dụng |
| POST | `/api/jobs/with-jd` | Form gồm thông tin chính của tin và tệp `Pdf` | 201, mã tin | Nhà tuyển dụng |
| PUT | `/api/jobs/{id}/with-jd` | Cùng form đăng tin, có thể kèm `rowVersion` | 204 | Chủ tin hoặc admin |
| GET | `/api/jobs/{id}/jd` | Mã tin | 200, tệp PDF | Người xem được tin |
| POST | `/api/jobs/{jobId}/applications` | Form gồm `cv` (tệp), `message` (lời nhắn) | 201, `{id}` và header `Location` | Candidate |
| GET | `/api/applications/mine` | Bộ tham số lọc đơn | 200, một trang đơn của mình | Candidate |
| GET | `/api/jobs/{jobId}/applications` | Mã tin, bộ tham số lọc đơn | 200, một trang đơn của tin | Chủ tin |
| GET | `/api/applications/{id}` | Mã đơn | 200, chi tiết đơn | Người nộp hoặc chủ tin |
| GET | `/api/applications/{id}/cv` | Mã đơn | 200, tệp `cv-{id}.pdf` | Người nộp hoặc chủ tin |
| PATCH | `/api/applications/{id}/status` | `{"status":"Accepted"}` hoặc `{"status":"Rejected"}` | 204 | Chủ tin |

- **Nội dung tin** gồm `title, description, location, deadline, isOpen`. Gửi hạn theo ISO 8601 có múi giờ, ví dụ `2030-06-30T17:00:00+07:00`. Tin đang mở cần hạn ở tương lai. Gửi `isOpen: false` khi đóng tin.
- **JD đính kèm** có thể là đường dẫn HTTP/HTTPS qua trường JSON `jdUrl` hoặc tệp PDF tối đa 5 MiB qua form. Khi sửa tin, bỏ qua các trường JD để giữ bản cũ, gửi `removeJd: true` trong JSON để gỡ bản đính kèm.
- **Hồ sơ công khai** mặc định tắt. Ứng viên điều chỉnh trong **Hồ sơ và tài khoản → Hoàn chỉnh hồ sơ**. Khi họ tắt, hồ sơ rời danh bạ. Các CV đã nộp vẫn được quản lý theo từng đơn.
- **Mức lương** dùng số nguyên VNĐ/tháng. Với khoảng lương, gửi `"isSalaryNegotiable": false, "salaryMin": 12000000, "salaryMax": 18000000`. Hai số tiền phải dương và mức tối đa phải bằng hoặc cao hơn mức tối thiểu. Khi chọn **Thỏa thuận**, chỉ gửi `"isSalaryNegotiable": true` và bỏ hai số tiền. Tin cũ hiển thị **Chưa cung cấp**. API cập nhật giữ mức lương đang lưu nếu client bỏ qua cả ba trường.
- Nhà tuyển dụng tải ảnh ở **Hồ sơ và tài khoản → Logo công ty**. Logo hiển thị trên thẻ công ty, thẻ việc làm, trang chi tiết tin và danh sách **Việc đã lưu**. Công ty chưa có logo dùng chữ viết tắt.
- Có thể thêm `industry` khi tạo hoặc sửa tin. Lọc theo đúng ngành bằng `GET /api/jobs?industry=...`. Trang chủ hiển thị tối đa sáu ngành có nhiều tin công khai còn hạn nhất. Khi ứng viên gửi JWT, danh sách và chi tiết tin trả thêm trạng thái `isSaved`.
- **Bộ tham số tìm tin** gồm `keyword` (một phần tiêu đề hoặc tên công ty), `location`, `employerId`, `sort`, `page`, `pageSize`. `sort` hỗ trợ `Newest` (mặc định), `Deadline` (gần nhất trước), `Title` (A đến Z) và `Featured` (nhiều đơn nhất trước, nếu bằng nhau ưu tiên tin mới). Ví dụ `/api/jobs?keyword=Backend&location=HCM&sort=Deadline&page=1&pageSize=5`.
- **Bộ tham số lọc đơn** gồm `status`, `jobId`, `page`, `pageSize`. Giao diện ứng viên dùng `/api/applications/mine?jobId=12` để kiểm tra tài khoản đã ứng tuyển tin 12 chưa. Danh sách đơn sắp theo ngày nộp mới nhất. Mọi danh sách phân trang có cấu trúc `{items, page, pageSize, totalCount}`. Mặc định trang 1, 10 dòng, tối đa 100 dòng/trang.
- **Thông tin tài khoản** gồm mã, email, họ tên, vai trò và tên công ty. **Chi tiết tin** thêm mã tin, mã nhà tuyển dụng, công ty và ngày tạo vào nội dung tin. **Chi tiết đơn** gồm mã đơn, mã và tiêu đề tin, mã, tên, email ứng viên, lời nhắn, trạng thái và ngày nộp.

### Đọc kết quả lỗi

| HTTP | Ví dụ |
|---|---|
| 400 | Thiếu tên công ty của Employer, email sai định dạng, trang bằng 0, tệp sai định dạng, hoặc tin mở có hạn trong quá khứ. |
| 401 | Sai email/mật khẩu, thiếu token ở API cần đăng nhập hoặc token hết hạn. |
| 403 | Ứng viên đăng tin, nhà tuyển dụng nộp đơn, nhà tuyển dụng sửa tin của người khác. |
| 404 | Không tìm thấy tin, hoặc người gọi tra cứu đơn/CV riêng của người khác. |
| 409 | Email trùng, nộp lần hai, tin đã đóng/hết hạn, nhà tuyển dụng sửa tin đã lưu trữ, xử lý lại đơn đã có kết quả hoặc lưu từ phiên bản tin cũ. |
| 413 | CV vượt 5 MiB hoặc toàn bộ form nộp đơn vượt 6 MiB. |

Lỗi nghiệp vụ trả đối tượng `ProblemDetails` với `title` và `status`. Với lỗi kiểm tra DTO, đọc thêm `errors` để biết trường nào cần sửa. Phản hồi 204 có body rỗng.

## Kiểm thử tự động backend và Swagger UI

Trên Windows cần .NET 10 SDK, SQL Server hoặc LocalDB, Node.js từ 20 và Chrome hoặc Edge. Mở PowerShell ở thư mục gốc và chạy lệnh sau.

```powershell
.\Tests\Run-Tests.ps1
```

Dùng `.\Tests\Run-Tests.ps1 -Browser msedge` nếu máy dùng Edge. Script khôi phục các package kiểm thử, build cấu hình `Integration` và chạy xUnit. Lần đầu cần kết nối NuGet và npm. Package và tệp tạm được đặt dưới thư mục `Tests` trên ổ chứa project.

Mỗi lần chạy, [ApiFixture](Tests/ApiFixture.cs) mở Kestrel trên cổng loopback ngẫu nhiên, tạo database `RecruitmentTests_...`, áp dụng migration rồi dọn database sau khi chạy. Mặc định bộ test dùng LocalDB. Để dùng SQL Server trên máy, đặt `$env:RECRUITMENT_TEST_SQL_SERVER = 'localhost'` trước khi chạy. Tài khoản Windows cần có quyền tạo database thử trên instance đó.

| Nhóm | Điều được kiểm tra |
|---|---|
| [Tài khoản và quản trị](Tests/AccountTests.cs) | Kiểm tra đầu vào, vai trò được tự đăng ký, email trùng, băm mật khẩu, JWT, khóa và mở lại tài khoản. |
| [Tin tuyển dụng](Tests/JobTests.cs) | Tìm kiếm, sắp xếp, phân trang, thống kê công khai, hạn nộp, quyền sở hữu và xung đột phiên bản. |
| [Đơn và CV](Tests/ApplicationTests.cs) | Định dạng, kích thước file/form, nộp đồng thời, xét đồng thời, quyền đọc và đối chiếu byte của CV tải về. |
| [Quản trị](Tests/AdminTests.cs) | Sửa hồ sơ, email trùng, xóa vật lý và phạm vi dữ liệu liên quan, biên ngày/tuần/tháng theo giờ Việt Nam, quyết định cũ thiếu ngày. |
| [Swagger](Tests/SwaggerTests.cs) | Đối chiếu tài liệu với các thao tác HTTP và kiểm tra yêu cầu đăng nhập. [Kịch bản trình duyệt](Tests/swagger-ui.cjs) bấm **Try it out**, **Execute**, **Authorize**, nộp PDF, thử nộp trùng và tải lại chính tệp đó. |

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
6. Để trình bày phân quyền, thử `POST /api/jobs` bằng token ứng viên (403). Đăng ký một Employer khác rồi thử tải CV của đơn vừa tạo (404). Dùng lại token chủ tin để gỡ tin đã nhận đơn (204). Mở `GET /api/employer/report` và `GET /api/employer/applications` để xem tin đã lưu trữ cùng lịch sử đơn. Khi tìm việc công khai, tin đã gỡ trả về 404.

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
| [account.js](wwwroot/js/account.js) | Đăng ký, đăng nhập và menu cá nhân. Trang **Hồ sơ và tài khoản** (`#/profile`) cho phép sửa thông tin, đổi mật khẩu và tải ảnh đại diện cùng một nơi. Ảnh JPG/PNG tối đa 5 MiB được cắt vuông, thu nhỏ còn 256 × 256 trước khi lưu. Đường dẫn cũ `#/account/settings` chuyển đến trang này. |
| [admin.js](wwwroot/js/admin.js) | Tra cứu, chỉnh sửa, khóa/mở khóa, xác nhận xóa tài khoản và xem báo cáo theo kỳ. Trang quản trị tin dùng chung form sửa tin và gửi JWT của Admin. |
| [jobs.js](wwwroot/js/jobs.js) | Hiển thị danh sách và chi tiết tin. Khi ứng tuyển, `FormData` gửi tệp và lời nhắn tới controller. |
| [employer.js](wwwroot/js/employer.js) | Tạo, sửa, đóng và xóa tin. Form sửa gửi `rowVersion` đã đọc cùng tin. Nếu một tab khác vừa lưu, API trả 409 và người dùng có thể tải lại phiên bản mới. |
| [employer-workspace.js](wwwroot/js/employer-workspace.js) | Danh bạ hồ sơ công khai, danh sách đơn của công ty và báo cáo tuyển dụng. |
| [applications.js](wwwroot/js/applications.js) | Lọc đơn, xem chi tiết, tải CV có xác thực và gửi quyết định xét hồ sơ. |
| [ui.js](wwwroot/js/ui.js) | Định dạng ngày, phân trang, xác nhận thao tác và mã hóa ký tự trước khi đưa nội dung người dùng nhập vào HTML. |

Ví dụ khi bấm **Gửi hồ sơ**, form trong `jobs.js` gọi `api.js`, trình duyệt gửi HTTP POST đến `ApplicationsController`, sau đó `ApplicationService` kiểm tra CV và `ApplicationRepository` ghi vào SQL Server. Frontend nhận mã đơn từ HTTP 201 và chuyển sang trang chi tiết. Quyết định cho phép thao tác luôn được kiểm tra ở API.

Để đóng gói giao diện và API cùng nhau, chạy `dotnet publish -c Release`. .NET sao chép `wwwroot` vào thư mục publish. Frontend dùng đường dẫn tương đối cùng origin cho các API nên có thể giữ nguyên mã JavaScript khi đổi cổng chạy.

Hình nền trang tìm việc nằm ở [career-steps.webp](wwwroot/assets/career-steps.webp), tạo bằng ImageGen tích hợp từ mô tả bậc thang xanh cobalt, cổng vòm xanh nhạt và quả cầu cam. Nội dung chữ và ô tìm kiếm được dựng bằng HTML.

# DoAn_Web2_ASPNET
