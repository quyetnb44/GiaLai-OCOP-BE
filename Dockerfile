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
RUN dotnet publish "GiaLaiOCOP.Api.csproj" -c Release -o /app

# =========================
# 2️⃣ Runtime stage
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:9.0-bullseye-slim AS runtime
WORKDIR /app

# Cài các thư viện cần thiết cho .NET runtime và System.Drawing.Common
RUN apt-get update && apt-get install -y --no-install-recommends \
        libicu70 \
        libssl3 \
        libkrb5-3 \
        libcurl4 \
        libgdiplus \
        ca-certificates \
    && rm -rf /var/lib/apt/lists/*

# Nếu bạn muốn sử dụng Globalization (format ngày tháng, số, tiền tệ)
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Copy app từ stage build
COPY --from=build /app .

# Mở port cho Render
EXPOSE 5000

# Start ứng dụng
ENTRYPOINT ["dotnet", "GiaLaiOCOP.Api.dll"]
