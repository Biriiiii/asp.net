# Sử dụng .NET SDK 8.0 để build code
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy toàn bộ source code vào container
COPY . .

# Chạy lệnh Build và Publish
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

# Sử dụng .NET Runtime 8.0 để chạy app (nhẹ hơn)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# 🚨 CHÚ Ý: Đổi tên file .dll này cho ĐÚNG với tên Project của bạn
ENTRYPOINT ["dotnet", "2123110233_LeDinhBang.dll"]