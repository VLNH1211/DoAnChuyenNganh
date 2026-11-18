# Stage 1: Build & Publish ASP.NET Core 8.0 API
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Sao chép file csproj và khôi phục các gói NuGet (tận dụng Docker Layer Cache)
COPY ["ĐACN/EcommerceSecurityApi/EcommerceSecurityApi.csproj", "EcommerceSecurityApi/"]
RUN dotnet restore "EcommerceSecurityApi/EcommerceSecurityApi.csproj"

# Sao chép toàn bộ mã nguồn dự án và biên dịch bản Release
COPY ["ĐACN/EcommerceSecurityApi/", "EcommerceSecurityApi/"]
WORKDIR "/src/EcommerceSecurityApi"
RUN dotnet publish "EcommerceSecurityApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime image nhẹ, tối ưu cho Render Linux
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Cấu hình mặc định chạy trên Render với SQLite (tự động đọc biến PORT của Render trong Program.cs)
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DatabaseProvider=Sqlite
ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "EcommerceSecurityApi.dll"]
