# 1. Chọn base image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5000

# 2. Build app
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "./GiaLaiOCOP.Api.csproj"
RUN dotnet publish "./GiaLaiOCOP.Api.csproj" -c Release -o /app/publish

# 3. Chạy app
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "GiaLaiOCOP.Api.dll"]
