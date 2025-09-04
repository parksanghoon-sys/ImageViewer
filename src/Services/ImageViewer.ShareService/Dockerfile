# Share Service - Simple Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Services/ImageViewer.ShareService/ImageViewer.ShareService.csproj", "src/Services/ImageViewer.ShareService/"]
COPY ["src/Core/ImageViewer.Domain/ImageViewer.Domain.csproj", "src/Core/ImageViewer.Domain/"]
COPY ["src/Core/ImageViewer.Application/ImageViewer.Application.csproj", "src/Core/ImageViewer.Application/"]
COPY ["src/Core/ImageViewer.Contracts/ImageViewer.Contracts.csproj", "src/Core/ImageViewer.Contracts/"]
COPY ["src/Infrastructure/ImageViewer.Infrastructure/ImageViewer.Infrastructure.csproj", "src/Infrastructure/ImageViewer.Infrastructure/"]

RUN dotnet restore "src/Services/ImageViewer.ShareService/ImageViewer.ShareService.csproj"

COPY . .
WORKDIR "/src/src/Services/ImageViewer.ShareService"
RUN dotnet publish "ImageViewer.ShareService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ImageViewer.ShareService.dll"]