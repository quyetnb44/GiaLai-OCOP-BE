# =========================
# 1️⃣ Build stage
# =========================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project file và restore dependencies
COPY ["GiaLaiOCOP.Api.csproj", "./"]
RUN dotnet restore "GiaLaiOCOP.Api.csproj"

# Copy toàn bộ source code và publish
COPY . .
RUN dotnet publish "GiaLaiOCOP.Api.csproj" -c Release -o /app/publish

# =========================
# 2️⃣ Runtime stage
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:9.0-bullseye AS runtime
WORKDIR /app

# Cài thư viện cần thiết cho PostgreSQL
RUN apt-get update && apt-get install -y libpq-dev && rm -rf /var/lib/apt/lists/*

# Copy app từ stage build
COPY --from=build /app/publish .

# Mở port
EXPOSE 5000

# Start app
ENTRYPOINT ["dotnet", "GiaLaiOCOP.Api.dll"]
