# Đóng góp vào đồ án

Mỗi thay đổi vào `main` đi qua một pull request (PR). Contributor push commit lên nhánh làm việc của mình và mở PR. Người quản lý repository trên GitHub kiểm tra thay đổi và thực hiện merge.

## Contributor gửi thay đổi

Ví dụ dưới đây minh họa một thay đổi tài liệu thử API bằng Swagger.

1. Từ thư mục đồ án, lấy phiên bản mới nhất của `main` và tạo nhánh làm việc.

   ```powershell
   git fetch origin
   git switch --no-track -c docs/huong-dan-swagger origin/main
   ```

2. Chỉnh sửa tệp, xem lại phần đã sửa, rồi commit và push nhánh đó. Thay `README.md` bằng các tệp thuộc công việc của bạn.

   ```powershell
   git status
   git diff
   git add -- README.md
   git commit -m "Bổ sung hướng dẫn thử API bằng Swagger"
   git push -u origin docs/huong-dan-swagger
   ```

3. Trên GitHub, chọn **Compare & pull request**. Kiểm tra **base** là `main` và **compare** là `docs/huong-dan-swagger`. Ghi ngắn gọn thay đổi và cách đã kiểm thử, bấm **Create pull request**, rồi gửi PR cho người quản lý xem.
4. Khi nhận góp ý, sửa trên cùng nhánh, commit rồi chạy `git push`. GitHub tự cập nhật PR đang mở. Người quản lý xem lại và merge khi thay đổi đã đạt yêu cầu.

## Người quản lý bật bảo vệ cho main

Các bước này cần quyền quản trị repository GitHub. Contributor cần quyền ghi để push nhánh làm việc. Giữ quyền quản trị và quyền chỉnh sửa ruleset cho người phụ trách repository.

1. Mở **Settings → Rules → Rulesets → New ruleset → New branch ruleset**. Đặt tên `Bao ve main` và chọn **Enforcement status → Active**. [Mở cài đặt của đồ án](https://github.com/vxxngw/DoAn_Web2_ASPNET/settings/rules)
2. Ở **Target branches**, chọn **Add target → Include by pattern**, nhập đúng `main`. Nhánh làm việc của contributor nằm ngoài phạm vi quy tắc này.
3. Ở **Bypass list**, thêm vai trò quản trị repository (**Repository admin**) và chọn **For pull requests only**. Chỉ tài khoản được giao merge mới giữ vai trò này. Để các vai trò contributor như **Write** ngoài danh sách. [Hướng dẫn cấu hình của GitHub](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-rulesets/creating-rulesets-for-a-repository)
4. Trong **Branch rules**, bật **Restrict updates** và **Require a pull request before merging**. Giữ **Restrict deletions** và **Block force pushes** được bật. Bấm **Create**. Quy tắc **Restrict updates** giới hạn người có thể cập nhật `main`, kể cả khi cập nhật bằng nút merge của PR. [Các quy tắc nhánh trên GitHub](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-rulesets/available-rules-for-rulesets)
5. Kiểm tra trên một PR thực tế bằng tài khoản contributor. Tài khoản này push được nhánh riêng, mở và cập nhật được PR, nhưng GitHub chặn thao tác merge vào `main`. Đăng nhập bằng tài khoản quản lý để xem diff và hoàn tất merge qua PR.

GitHub Free hỗ trợ branch ruleset cho repository công khai. Với repository riêng tư, tính năng này có trên GitHub Pro, Team hoặc Enterprise Cloud tùy loại tài khoản. [Điều kiện sử dụng ruleset](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-rulesets/creating-rulesets-for-a-repository)
