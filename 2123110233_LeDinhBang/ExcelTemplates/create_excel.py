import csv
from openpyxl import Workbook

# Dữ liệu 5 cuốn sách mới với tên khác
data = [
    ["Id", "Title", "Slug", "Isbn", "CategoryId", "PublisherId", "PageCount", "WeightGram", "Language", "CoverType", "OriginalPrice", "SalePrice", "Description", "IsActive", "IsFeatured", "PublishedDate", "QtyAvailable", "MinThreshold", "WarehouseLocation", "ImageUrls", "AuthorId"],
    ["", "Trên Sao Bạn Sẽ Thấy", "tren-sao-ban-se-thay", "978604000036", "78FA9154-C860-4CAF-BE71-38903BBC8BB9", "C2E48B0E-3CBA-4012-BEA5-3444A6C85AC7", 280, 350, "vi", "Paperback", 125000, 105000, "Hành trình tìm lại chính mình qua những trang viết nhẹ nhàng", True, True, "2026-05-08", 120, 10, "Ke E1", "https://res.cloudinary.com/dpiolhoq6/image/upload/v1778214195/bookstore/products/71CjT0N23ML._AC_UF1000%2C1000_QL80__1961f6b55cc54d1cb5f73368cd6a3a30.jpg", "FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF"],
    ["", "Rừng Na Uy", "rung-na-uy", "978604000037", "1753F838-F425-4587-8293-0A70B866F7FF", "C2E48B0E-3CBA-4012-BEA5-3444A6C85AC7", 350, 400, "vi", "Paperback", 180000, 155000, "Tiểu thuyết tình yêu nổi tiếng của Haruki Murakami", True, True, "2026-05-15", 85, 8, "Ke E1", "https://res.cloudinary.com/dpiolhoq6/image/upload/v1778214195/bookstore/products/71CjT0N23ML._AC_UF1000%2C1000_QL80__1961f6b55cc54d1cb5f73368cd6a3a30.jpg", "FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF"],
    ["", "Bắt Trẻ Đồng Xanh", "bat-tre-dong-xanh", "978604000038", "1753F838-F425-4587-8293-0A70B866F7FF", "C2E48B0E-3CBA-4012-BEA5-3444A6C85AC7", 320, 380, "vi", "Paperback", 160000, 135000, "Kiệt tác văn học Mỹ của J.D. Salinger", True, False, "2026-05-20", 95, 10, "Ke E2", "https://res.cloudinary.com/dpiolhoq6/image/upload/v1778214195/bookstore/products/71CjT0N23ML._AC_UF1000%2C1000_QL80__1961f6b55cc54d1cb5f73368cd6a3a30.jpg", "FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF"],
    ["", "Cuốn Theo Chiều Gió", "cuon-theo-chieu-gio", "978604000039", "1753F838-F425-4587-8293-0A70B866F7FF", "C2E48B0E-3CBA-4012-BEA5-3444A6C85AC7", 1200, 1400, "vi", "Hardcover", 450000, 390000, "Tiểu thuyết tình yêu bất hủ thời Nội chiến Mỹ", True, True, "2026-06-01", 50, 5, "Ke E2", "https://res.cloudinary.com/dpiolhoq6/image/upload/v1778214195/bookstore/products/71CjT0N23ML._AC_UF1000%2C1000_QL80__1961f6b55cc54d1cb5f73368cd6a3a30.jpg", "FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF"],
    ["", "Sapiens: Lược Sử Loài Người", "sapiens-luoc-su-loai-nguoi", "978604000040", "78FA9154-C860-4CAF-BE71-38903BBC8BB9", "C2E48B0E-3CBA-4012-BEA5-3444A6C85AC7", 500, 600, "vi", "Paperback", 250000, 220000, "Hành trình của nhân loại từ quá khứ đến tương lai", True, True, "2026-06-10", 75, 8, "Ke E3", "https://res.cloudinary.com/dpiolhoq6/image/upload/v1778214195/bookstore/products/71CjT0N23ML._AC_UF1000%2C1000_QL80__1961f6b55cc54d1cb5f73368cd6a3a30.jpg", "FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF"],
]

# Tạo file Excel
wb = Workbook()
ws = wb.active
ws.title = "Books"

# Ghi dữ liệu vào Excel
for row in data:
    ws.append(row)

# Lưu file Excel
wb.save("books_new.xlsx")
print("✅ Đã tạo file books_new.xlsx (Excel file)")

# Tạo file CSV
with open('books_new.csv', 'w', newline='', encoding='utf-8-sig') as f:
    writer = csv.writer(f)
    for row in data:
        # Chuyển True/False thành string cho CSV
        csv_row = []
        for cell in row:
            if cell is True:
                csv_row.append("TRUE")
            elif cell is False:
                csv_row.append("FALSE")
            else:
                csv_row.append(cell)
        writer.writerow(csv_row)

print("✅ Đã tạo file books_new.csv (CSV file)")
print("\n📚 Danh sách 5 sản phẩm mới:")
print("1. Trên Sao Bạn Sẽ Thấy")
print("2. Rừng Na Uy")
print("3. Bắt Trẻ Đồng Xanh")
print("4. Cuốn Theo Chiều Gió")
print("5. Sapiens: Lược Sử Loài Người")