Add-Type -AssemblyName System.IO.Compression.FileSystem

$ErrorActionPreference = "Stop"
$outPath = Join-Path (Get-Location) "LeDinhBang_2123110233_BookStore_ASP.NET.docx"
$tmp = Join-Path $env:TEMP ("bookstore-report-" + [guid]::NewGuid())
New-Item -ItemType Directory -Path $tmp | Out-Null
New-Item -ItemType Directory -Path (Join-Path $tmp "_rels") | Out-Null
New-Item -ItemType Directory -Path (Join-Path $tmp "word") | Out-Null
New-Item -ItemType Directory -Path (Join-Path $tmp "word\_rels") | Out-Null

function X([string]$s) { [Security.SecurityElement]::Escape($s) }
function P([string]$text, [string]$style = "Normal", [string]$jc = "left", [bool]$bold = $false) {
    $b = if ($bold) { "<w:b/>" } else { "" }
    $sz = switch ($style) {
        "Title" { "32" }
        "Heading1" { "28" }
        "Heading2" { "26" }
        "Heading3" { "24" }
        default { "24" }
    }
    return "<w:p><w:pPr><w:pStyle w:val=`"$style`"/><w:jc w:val=`"$jc`"/><w:spacing w:after=`"120`"/></w:pPr><w:r><w:rPr>$b<w:sz w:val=`"$sz`"/></w:rPr><w:t xml:space=`"preserve`">$(X $text)</w:t></w:r></w:p>"
}
function PageBreak() { return "<w:p><w:r><w:br w:type=`"page`"/></w:r></w:p>" }
function Row([string[]]$cells, [bool]$header = $false) {
    $tc = ""
    foreach ($c in $cells) {
        $tc += "<w:tc><w:tcPr><w:tcW w:w=`"2400`" w:type=`"dxa`"/></w:tcPr>$(P $c "Normal" "left" $header)</w:tc>"
    }
    return "<w:tr>$tc</w:tr>"
}
function Table($rows) {
    $xml = "<w:tbl><w:tblPr><w:tblStyle w:val=`"TableGrid`"/><w:tblW w:w=`"0`" w:type=`"auto`"/><w:tblBorders><w:top w:val=`"single`" w:sz=`"4`"/><w:left w:val=`"single`" w:sz=`"4`"/><w:bottom w:val=`"single`" w:sz=`"4`"/><w:right w:val=`"single`" w:sz=`"4`"/><w:insideH w:val=`"single`" w:sz=`"4`"/><w:insideV w:val=`"single`" w:sz=`"4`"/></w:tblBorders></w:tblPr>"
    for ($i = 0; $i -lt $rows.Count; $i++) { $xml += Row $rows[$i] ($i -eq 0) }
    return $xml + "</w:tbl>"
}

$body = ""
$body += P "TRƯỜNG CAO ĐẲNG CÔNG THƯƠNG TP. HCM" "Title" "center" $true
$body += P "KHOA CÔNG NGHỆ THÔNG TIN" "Title" "center" $true
$body += P "BÁO CÁO" "Title" "center" $true
$body += P "ASP.NET" "Title" "center" $true
$body += P "ĐỀ TÀI: HỆ THỐNG QUẢN LÝ NHÀ SÁCH BOOKSTORE" "Title" "center" $true
$body += P "GIẢNG VIÊN HƯỚNG DẪN: HUỲNH TẤN PHÁT" "Normal" "center" $true
$body += P "SINH VIÊN THỰC HIỆN: LÊ ĐÌNH BẰNG" "Normal" "center" $true
$body += P "MSSV: 2123110233 - LỚP: CCQ2311G" "Normal" "center" $true
$body += P "Tp. Hồ Chí Minh, tháng 5 năm 2026" "Normal" "center"
$body += PageBreak

$body += P "BẢNG PHÂN CÔNG CÔNG VIỆC CỦA TỪNG THÀNH VIÊN" "Heading1" "center" $true
$body += Table @(
    @("Stt","Họ và tên","MSSV","Nội dung công việc","Kết quả thực hiện"),
    @("1","Lê Đình Bằng","2123110233","Phân tích yêu cầu, thiết kế cơ sở dữ liệu, xây dựng ASP.NET Core Web API, viết báo cáo.","100%")
)
$body += P "Nhận xét của giảng viên:" "Normal"
$body += P "Ngày ... tháng ... năm ..." "Normal" "right"
$body += P "Giảng viên" "Normal" "right" $true
$body += PageBreak

$body += P "MỤC LỤC" "Heading1" "center" $true
$toc = @(
"LỜI CẢM ƠN",
"CHƯƠNG 1: TỔNG QUAN DỰ ÁN",
"1.1. GIỚI THIỆU ĐỀ TÀI",
"1.2. PHÂN TÍCH YÊU CẦU HỆ THỐNG",
"1.3. MÔ HÌNH PHÁT TRIỂN VÀ USE CASE",
"1.4. QUY TẮC NGHIỆP VỤ QUAN TRỌNG",
"CHƯƠNG 2: CÔNG NGHỆ VÀ KIẾN TRÚC HỆ THỐNG",
"2.1. KIẾN TRÚC TỔNG THỂ",
"2.2. CÔNG NGHỆ BACKEND",
"2.3. CƠ SỞ DỮ LIỆU VÀ HẠ TẦNG",
"CHƯƠNG 3: ĐẶC TẢ CHỨC NĂNG VÀ THIẾT KẾ HỆ THỐNG",
"3.1. Đăng ký / Đăng nhập",
"3.2. Quản lý Sản phẩm và Import/Export Excel",
"3.3. Quản lý Danh mục, Tác giả, Nhà xuất bản",
"3.4. Giỏ hàng, Đặt hàng và Thanh toán",
"3.5. Khuyến mãi, Đánh giá, Kho và Dashboard",
"3.6. Đặc tả API REST",
"CHƯƠNG 4: TỔNG KẾT",
"TÀI LIỆU THAM KHẢO"
)
foreach ($t in $toc) { $body += P $t "Normal" }
$body += PageBreak

$sections = @(
@("LỜI CẢM ƠN","Em xin chân thành cảm ơn thầy Huỳnh Tấn Phát đã tận tình hướng dẫn, truyền đạt kiến thức và tạo điều kiện để em hoàn thành đề tài ASP.NET này. Trong quá trình thực hiện, em đã vận dụng kiến thức về ASP.NET Core Web API, Entity Framework Core, SQL Server, JWT Authentication và thiết kế hệ thống theo mô hình nhiều lớp. Do thời gian và kinh nghiệm còn hạn chế, báo cáo không tránh khỏi thiếu sót. Em rất mong nhận được góp ý của thầy cô để đề tài được hoàn thiện hơn."),
@("CHƯƠNG 1: TỔNG QUAN DỰ ÁN",""),
@("1.1. GIỚI THIỆU ĐỀ TÀI","Đề tài xây dựng hệ thống quản lý nhà sách BookStore dưới dạng ASP.NET Core Web API. Hệ thống hỗ trợ quản lý sản phẩm sách, danh mục, tác giả, nhà xuất bản, tồn kho, giỏ hàng, đơn hàng, thanh toán, khuyến mãi, đánh giá sản phẩm và quản trị người dùng. API được thiết kế phục vụ frontend hoặc ứng dụng mobile có thể kết nối độc lập."),
@("1.1.1. Đặt vấn đề","Các nhà sách trực tuyến cần quản lý số lượng đầu sách lớn, nhiều tác giả, nhiều nhà xuất bản, tồn kho thay đổi liên tục và quy trình đặt hàng phát sinh thường xuyên. Nếu quản lý thủ công sẽ dễ sai lệch giá bán, tồn kho, trạng thái đơn hàng và khó theo dõi doanh thu. Vì vậy hệ thống BookStore được xây dựng nhằm số hóa quy trình bán sách và quản trị vận hành."),
@("1.1.2. Mục tiêu đề tài","Xây dựng API bán sách có phân quyền rõ ràng; quản lý sản phẩm, danh mục, tác giả, nhà xuất bản; hỗ trợ import/export Excel để cập nhật dữ liệu hàng loạt; xử lý giỏ hàng, đơn hàng, thanh toán COD/VNPay; quản lý voucher, flash sale, review, nhập hàng và dashboard thống kê."),
@("1.1.3. Phạm vi hệ thống","Phạm vi bao gồm backend ASP.NET Core Web API, cơ sở dữ liệu SQL Server, JWT Authentication, Swagger UI, Cloudinary upload ảnh, SMTP email, VNPay payment gateway và các module nghiệp vụ chính của nhà sách."),
@("1.1.4. Đối tượng sử dụng","Hệ thống phục vụ Guest, Customer, Staff, ContentManager, Marketing, Admin và SuperAdmin. Mỗi nhóm có quyền truy cập khác nhau thông qua Authorize Roles trong controller."),
@("1.2. PHÂN TÍCH YÊU CẦU HỆ THỐNG",""),
@("1.2.1. Yêu cầu chức năng","Hệ thống cho phép đăng ký, đăng nhập, refresh token, quên mật khẩu, quản trị người dùng; xem, tìm kiếm, lọc và quản lý sách; cập nhật tồn kho; import/export Excel; thêm sản phẩm vào giỏ; đặt hàng; xác nhận thanh toán; quản lý voucher, flash sale, đánh giá, nhà cung cấp và phiếu nhập hàng."),
@("1.2.2. Yêu cầu phi chức năng","API cần bảo mật bằng JWT, phân quyền theo vai trò, trả dữ liệu JSON camelCase, xử lý lỗi tập trung qua ExceptionMiddleware, hỗ trợ Swagger để kiểm thử, tối ưu truy vấn bằng Entity Framework Core và đảm bảo dễ mở rộng theo kiến trúc nhiều lớp."),
@("1.3. MÔ HÌNH PHÁT TRIỂN VÀ USE CASE","Dự án được tổ chức theo hướng Clean Architecture đơn giản: API nhận request, Application xử lý nghiệp vụ và DTO, Domain chứa entity/interface, Infrastructure thao tác dữ liệu. Quy trình phát triển gồm phân tích yêu cầu, thiết kế database, lập trình module, kiểm thử API trên Swagger/Postman và hoàn thiện báo cáo."),
@("1.4. QUY TẮC NGHIỆP VỤ QUAN TRỌNG","Slug sản phẩm là duy nhất; xóa sản phẩm là xóa mềm thông qua IsActive; tồn kho được kiểm soát qua Inventory; khách có thể có giỏ hàng bằng sessionId và khi đăng nhập có thể merge giỏ; voucher kiểm tra hạn dùng và số lần sử dụng; đánh giá chỉ hợp lệ khi người dùng đã mua hàng và đơn đã giao; file import sản phẩm phải là .xlsx.")
)
foreach ($s in $sections) { $body += P $s[0] "Heading1" "left" $true; if ($s[1]) { $body += P $s[1] } }

$body += Table @(
@("Actor","Mô tả","Quyền hạn chính"),
@("Guest","Người truy cập chưa đăng nhập","Xem sản phẩm, danh mục, thêm giỏ theo session"),
@("Customer","Người dùng đã có tài khoản","Đặt hàng, quản lý địa chỉ, thanh toán, đánh giá"),
@("Staff","Nhân viên vận hành","Cập nhật tồn kho, xuất dữ liệu sản phẩm"),
@("ContentManager","Quản trị nội dung sách","Thêm/sửa sách, danh mục, tác giả, nhà xuất bản, import Excel"),
@("Marketing","Nhân sự marketing","Quản lý voucher và flash sale"),
@("Admin/SuperAdmin","Quản trị hệ thống","Quản lý người dùng, phân quyền, đơn hàng, cấu hình hệ thống")
)

$body += PageBreak
$body += P "CHƯƠNG 2: CÔNG NGHỆ VÀ KIẾN TRÚC HỆ THỐNG" "Heading1" "left" $true
$body += P "2.1. KIẾN TRÚC TỔNG THỂ" "Heading2" "left" $true
$body += P "Project được chia thành bốn nhóm chính: API chứa controller, middleware, extension và Program.cs; Application chứa service, interface và DTO; Domain chứa entity, enum, repository interface; Infrastructure chứa DbContext, repository và seeder. Cách tách lớp này giúp controller mỏng, nghiệp vụ tập trung trong service và thao tác dữ liệu được đóng gói qua repository."
$body += P "2.2. CÔNG NGHỆ BACKEND" "Heading2" "left" $true
$body += Table @(
@("Công nghệ","Vai trò trong hệ thống"),
@("ASP.NET Core 8 Web API","Xây dựng REST API, controller, middleware, DI container"),
@("Entity Framework Core 8.0.13","ORM kết nối SQL Server, migration, DbContext"),
@("SQL Server","Lưu trữ dữ liệu sách, người dùng, đơn hàng, kho, khuyến mãi"),
@("JWT Bearer Authentication","Xác thực access token và phân quyền role"),
@("Swagger/Swashbuckle","Tài liệu và kiểm thử API trực tiếp"),
@("CloudinaryDotNet","Upload và quản lý ảnh sản phẩm"),
@("VNPay service","Xử lý thanh toán trực tuyến và callback/IPN"),
@("SMTP Email","Gửi xác minh email, đặt lại mật khẩu, xác nhận đơn hàng")
)
$body += P "2.3. CƠ SỞ DỮ LIỆU VÀ HẠ TẦNG" "Heading2" "left" $true
$body += P "Hệ thống dùng AppDbContext cho nghiệp vụ bán hàng và AuthDbContext cho xác thực. Các bảng chính gồm Users, Roles, UserSessions, Products, Categories, Authors, Publishers, Inventories, Carts, Orders, Payments, Shipments, Vouchers, FlashSales, Reviews, Suppliers và PurchaseOrders. Program.cs cấu hình CORS, Swagger, JWT, ExceptionMiddleware, static files và seed dữ liệu khi ứng dụng khởi động."

$body += PageBreak
$body += P "CHƯƠNG 3: ĐẶC TẢ CHỨC NĂNG VÀ THIẾT KẾ HỆ THỐNG" "Heading1" "left" $true
$features = @(
@("3.1. Chức năng Đăng ký / Đăng nhập","AuthController cung cấp API register, login, refresh token, logout, logout-all, gửi email xác minh, quên mật khẩu và đặt lại mật khẩu. AuthService xử lý băm mật khẩu, sinh JWT, quản lý refresh token và phiên đăng nhập."),
@("3.2. Chức năng Quản lý Sản phẩm và Import/Export Excel","ProductsController hỗ trợ lấy danh sách có phân trang/lọc, xem chi tiết theo Id hoặc slug, tạo, cập nhật, xóa mềm, cập nhật tồn kho, export Excel và import Excel. Khi import, nếu có Id thì cập nhật; nếu không có Id thì dựa vào Slug để tạo mới hoặc cập nhật."),
@("3.3. Chức năng Quản lý Danh mục, Tác giả, Nhà xuất bản","CategoriesController quản lý cây danh mục và danh mục cha-con. AuthorsController và PublishersController hỗ trợ tìm kiếm, thêm, sửa, xóa dữ liệu tác giả và nhà xuất bản phục vụ hồ sơ sản phẩm sách."),
@("3.4. Chức năng Giỏ hàng, Đặt hàng và Thanh toán","CartController hỗ trợ thêm, cập nhật, xóa item và merge giỏ hàng khi người dùng đăng nhập. OrderControllers xử lý tạo đơn, xem lịch sử, cập nhật trạng thái, gán vận chuyển, hoàn tiền. PaymentsController xử lý callback, VNPay confirm, return và IPN."),
@("3.5. Chức năng Khuyến mãi, Đánh giá, Kho và Dashboard","PromotionControllers gồm voucher và flash sale. ReviewController quản lý đánh giá sản phẩm. WarehouseControllers quản lý nhà cung cấp, phiếu nhập hàng và nhận hàng. DashboardController cung cấp số liệu tổng quan cho quản trị.")
)
foreach ($f in $features) { $body += P $f[0] "Heading2" "left" $true; $body += P $f[1] }

$body += P "3.6. Các Use Case chính" "Heading2" "left" $true
$body += Table @(
@("Mã UC","Tên Use Case","Actor","Mức độ ưu tiên"),
@("UC-01","Đăng ký / Đăng nhập","Guest, Customer","Cao"),
@("UC-02","Xem và tìm kiếm sách","Tất cả","Cao"),
@("UC-03","Quản lý sản phẩm sách","Admin, ContentManager","Cao"),
@("UC-04","Import/Export sản phẩm Excel","Admin, ContentManager, Staff","Cao"),
@("UC-05","Quản lý giỏ hàng","Guest, Customer","Cao"),
@("UC-06","Đặt hàng và thanh toán","Customer","Cao"),
@("UC-07","Quản lý voucher/flash sale","Admin, Marketing","Trung bình"),
@("UC-08","Quản lý kho và phiếu nhập","Admin, Staff","Cao"),
@("UC-09","Quản trị người dùng","Admin, SuperAdmin","Cao")
)

$body += P "3.7. Đặc tả API REST" "Heading2" "left" $true
$body += Table @(
@("Nhóm API","Endpoint tiêu biểu","Chức năng"),
@("Auth","POST /api/auth/register, POST /api/auth/login","Đăng ký, đăng nhập, cấp token"),
@("Products","GET /api/Products, POST /api/Products, GET /api/Products/export-excel","Quản lý sách và dữ liệu Excel"),
@("Categories","GET /api/Categories/tree","Lấy cây danh mục"),
@("Cart","GET /api/cart, POST /api/cart/items","Quản lý giỏ hàng"),
@("Orders","POST /api/orders, PUT /api/orders/{id}/status","Tạo và quản lý đơn hàng"),
@("Payments","POST /api/payments/callback, GET /api/payments/vnpay-ipn","Xác nhận thanh toán"),
@("Vouchers","POST /api/vouchers/validate","Kiểm tra mã giảm giá"),
@("Flash Sales","GET /api/flash-sales/active","Lấy chương trình flash sale đang chạy"),
@("Images","POST /api/images/upload","Upload ảnh lên Cloudinary"),
@("Dashboard","GET /api/dashboard","Thống kê quản trị")
)

$body += P "3.8. Test case tiêu biểu" "Heading2" "left" $true
$body += Table @(
@("TC ID","Tên Test","Điều kiện đầu vào","Bước thực hiện","Kết quả mong đợi"),
@("TC-AUTH-01","Đăng nhập thành công","Email/password hợp lệ","Gửi POST /api/auth/login","Trả về accessToken, refreshToken và thông tin user"),
@("TC-PROD-01","Tạo sách mới","Admin/ContentManager có token","Gửi POST /api/Products","Sách được tạo, trả HTTP 201"),
@("TC-EXCEL-01","Export sản phẩm","Tài khoản có quyền export","Gọi GET /api/Products/export-excel","Tải file .xlsx đúng cấu trúc"),
@("TC-EXCEL-02","Import sản phẩm","File .xlsx hợp lệ","Gửi multipart POST /api/Products/import-excel","Trả totalRows, created, updated, failed"),
@("TC-CART-01","Thêm vào giỏ","Sản phẩm tồn tại, số lượng hợp lệ","Gửi POST /api/cart/items","Giỏ hàng cập nhật tổng tiền"),
@("TC-ORDER-01","Tạo đơn hàng","Giỏ hàng có sản phẩm","Gửi POST /api/orders","Đơn hàng được tạo, trừ/tạm giữ tồn kho"),
@("TC-VOUCHER-01","Validate voucher","Voucher còn hiệu lực","Gửi POST /api/vouchers/validate","Trả số tiền giảm giá hợp lệ"),
@("TC-WH-01","Nhận hàng nhập kho","Phiếu nhập đã tạo","Gửi request receive","Tồn kho sản phẩm tăng đúng số lượng")
)

$body += PageBreak
$body += P "CHƯƠNG 4: TỔNG KẾT" "Heading1" "left" $true
$body += P "4.1. Tổng quan quá trình thực hiện" "Heading2" "left" $true
$body += P "Trong quá trình thực hiện đề tài, em đã phân tích yêu cầu nghiệp vụ nhà sách, thiết kế mô hình dữ liệu, xây dựng các entity, repository, service, DTO và controller tương ứng. Hệ thống được cấu hình theo ASP.NET Core 8, Entity Framework Core, SQL Server và JWT Authentication."
$body += P "4.2. Các công việc đã thực hiện" "Heading2" "left" $true
$body += P "Đã xây dựng module xác thực, quản lý sản phẩm, danh mục, tác giả, nhà xuất bản, giỏ hàng, đơn hàng, thanh toán, khuyến mãi, đánh giá, kho, dashboard, upload ảnh và import/export Excel."
$body += P "4.3. Kết quả đạt được" "Heading2" "left" $true
$body += P "Hệ thống có cấu trúc rõ ràng, dễ mở rộng, hỗ trợ phân quyền theo vai trò, có Swagger để kiểm thử API và có khả năng tích hợp frontend độc lập. Các nghiệp vụ quan trọng của một website bán sách trực tuyến đã được triển khai ở phía backend."
$body += P "4.4. Hạn chế" "Heading2" "left" $true
$body += P "Một số chức năng như OAuth, OTP SMS hoặc giao diện frontend hoàn chỉnh còn có thể phát triển thêm. Cần bổ sung test tự động, logging nâng cao và triển khai thực tế để đánh giá hiệu năng trong môi trường nhiều người dùng."
$body += P "4.5. Hướng phát triển" "Heading2" "left" $true
$body += P "Có thể phát triển frontend React/Angular, bổ sung recommendation sách, tìm kiếm toàn văn, quản lý vận chuyển thực tế, báo cáo doanh thu nâng cao, unit test/integration test và CI/CD để triển khai lên cloud."
$body += P "TÀI LIỆU THAM KHẢO" "Heading1" "left" $true
$body += P "1. Microsoft Docs - ASP.NET Core Web API."
$body += P "2. Microsoft Docs - Entity Framework Core."
$body += P "3. Microsoft Docs - Authentication and Authorization in ASP.NET Core."
$body += P "4. Swagger / OpenAPI Documentation."
$body += P "5. Cloudinary .NET SDK Documentation."

$document = @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
  <w:body>
    $body
    <w:sectPr><w:pgSz w:w="11906" w:h="16838"/><w:pgMar w:top="1440" w:right="1134" w:bottom="1440" w:left="1701" w:header="708" w:footer="708" w:gutter="0"/></w:sectPr>
  </w:body>
</w:document>
"@

$styles = @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:styles xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
<w:style w:type="paragraph" w:default="1" w:styleId="Normal"><w:name w:val="Normal"/><w:rPr><w:rFonts w:ascii="Times New Roman" w:hAnsi="Times New Roman"/><w:sz w:val="24"/></w:rPr></w:style>
<w:style w:type="paragraph" w:styleId="Title"><w:name w:val="Title"/><w:rPr><w:rFonts w:ascii="Times New Roman" w:hAnsi="Times New Roman"/><w:b/><w:sz w:val="32"/></w:rPr></w:style>
<w:style w:type="paragraph" w:styleId="Heading1"><w:name w:val="heading 1"/><w:basedOn w:val="Normal"/><w:next w:val="Normal"/><w:qFormat/><w:rPr><w:rFonts w:ascii="Times New Roman" w:hAnsi="Times New Roman"/><w:b/><w:sz w:val="28"/></w:rPr></w:style>
<w:style w:type="paragraph" w:styleId="Heading2"><w:name w:val="heading 2"/><w:basedOn w:val="Normal"/><w:next w:val="Normal"/><w:qFormat/><w:rPr><w:rFonts w:ascii="Times New Roman" w:hAnsi="Times New Roman"/><w:b/><w:sz w:val="26"/></w:rPr></w:style>
<w:style w:type="paragraph" w:styleId="Heading3"><w:name w:val="heading 3"/><w:basedOn w:val="Normal"/><w:next w:val="Normal"/><w:qFormat/><w:rPr><w:rFonts w:ascii="Times New Roman" w:hAnsi="Times New Roman"/><w:b/><w:sz w:val="24"/></w:rPr></w:style>
<w:style w:type="table" w:styleId="TableGrid"><w:name w:val="Table Grid"/><w:tblPr><w:tblBorders><w:top w:val="single" w:sz="4"/><w:left w:val="single" w:sz="4"/><w:bottom w:val="single" w:sz="4"/><w:right w:val="single" w:sz="4"/><w:insideH w:val="single" w:sz="4"/><w:insideV w:val="single" w:sz="4"/></w:tblBorders></w:tblPr></w:style>
</w:styles>
"@

Set-Content -LiteralPath (Join-Path $tmp "[Content_Types].xml") -Value '<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/><Override PartName="/word/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml"/></Types>' -Encoding UTF8
Set-Content -LiteralPath (Join-Path $tmp "_rels\.rels") -Value '<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/></Relationships>' -Encoding UTF8
Set-Content -LiteralPath (Join-Path $tmp "word\_rels\document.xml.rels") -Value '<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rIdStyles" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/></Relationships>' -Encoding UTF8
Set-Content -LiteralPath (Join-Path $tmp "word\document.xml") -Value $document -Encoding UTF8
Set-Content -LiteralPath (Join-Path $tmp "word\styles.xml") -Value $styles -Encoding UTF8

if (Test-Path $outPath) { Remove-Item -LiteralPath $outPath -Force }
[IO.Compression.ZipFile]::CreateFromDirectory($tmp, $outPath)
Write-Output $outPath
