# 1️⃣ Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["GiaLaiOCOP.Api.csproj", "./"]
RUN dotnet restore "GiaLaiOCOP.Api.csproj"
COPY . .
RUN dotnet publish "GiaLaiOCOP.Api.csproj" -c Release -o /app/publish

# 2️⃣ Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENTRYPOINT ["dotnet", "GiaLaiOCOP.Api.dll"]
