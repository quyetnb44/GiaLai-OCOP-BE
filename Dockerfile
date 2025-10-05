# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj + restore dependencies
COPY ["GiaLaiOCOP.Api/GiaLaiOCOP.Api.csproj", "GiaLaiOCOP.Api/"]
WORKDIR /src/GiaLaiOCOP.Api
RUN dotnet restore "GiaLaiOCOP.Api.csproj"

# Copy toàn bộ code và publish
COPY . .
RUN dotnet publish "GiaLaiOCOP.Api.csproj" -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Fix globalization issue
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
RUN apt-get update && apt-get install -y libc6 libicu67 --no-install-recommends && rm -rf /var/lib/apt/lists/*

# Copy output từ build stage
COPY --from=build /app/publish .

# Mở port
EXPOSE 5000

# Chạy app
ENTRYPOINT ["dotnet", "GiaLaiOCOP.Api.dll"]
