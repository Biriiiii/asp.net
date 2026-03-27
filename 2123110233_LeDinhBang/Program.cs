using _2123110233_LeDinhBang.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Text.Json.Serialization; // Thêm thư viện này cho cấu hình JSON

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// 1. THÊM DÒNG NÀY: Khai báo dịch vụ Controllers (Bắt buộc phải có cho Web API)
// Kèm theo cấu hình IgnoreCycles để tránh lỗi vòng lặp vô tận khi Sách trỏ tới Danh mục, Danh mục trỏ ngược lại Sách
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

builder.Services.AddAuthorization();
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

// 2. Lệnh này giờ đã có thể hoạt động hoàn hảo nhờ khai báo AddControllers() ở trên
app.MapControllers();

app.Run();