# API Gateway - Simple Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Gateways/ImageViewer.ApiGateway/ImageViewer.ApiGateway.csproj", "src/Gateways/ImageViewer.ApiGateway/"]

RUN dotnet restore "src/Gateways/ImageViewer.ApiGateway/ImageViewer.ApiGateway.csproj"

COPY . .
WORKDIR "/src/src/Gateways/ImageViewer.ApiGateway"
RUN dotnet publish "ImageViewer.ApiGateway.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ImageViewer.ApiGateway.dll"]