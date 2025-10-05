# 1️⃣ Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project file và restore
COPY ["GiaLaiOCOP.Api.csproj", "./"]
RUN dotnet restore "GiaLaiOCOP.Api.csproj"

# Copy toàn bộ source code và publish
COPY . .
RUN dotnet publish "GiaLaiOCOP.Api.csproj" -c Release -o /app

# 2️⃣ Runtime stage
# Chọn base image bookworm-slim thay vì bullseye
FROM mcr.microsoft.com/dotnet/aspnet:9.0-bookworm-slim AS runtime
WORKDIR /app

# Nếu cần globalization (ví dụ hiển thị ngày tháng, số), giữ FALSE
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Cài đặt libicu phiên bản đúng với Debian Bookworm
RUN apt-get update \
    && apt-get install -y libicu72 --no-install-recommends \
    && rm -rf /var/lib/apt/lists/*

# Copy app từ stage build
COPY --from=build /app .

# Mở port cho Render
EXPOSE 5000

# Start app
ENTRYPOINT ["dotnet", "GiaLaiOCOP.Api.dll"]
