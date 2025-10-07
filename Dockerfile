### Dockerfile
# Dùng hình ảnh chính thức của .NET SDK để build ứng dụng
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Sao chép file csproj và restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Sao chép toàn bộ source code và build ứng dụng
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Dùng hình ảnh .NET Runtime để chạy ứng dụng
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "GiaLaiOCOP.Api.dll"]