FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5004

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution file and restore dependencies
COPY ["ImageViewer.sln", "./"]
COPY ["src/Services/ImageViewer.NotificationService/ImageViewer.NotificationService.csproj", "src/Services/ImageViewer.NotificationService/"]
COPY ["src/Core/ImageViewer.Contracts/ImageViewer.Contracts.csproj", "src/Core/ImageViewer.Contracts/"]
COPY ["src/Core/ImageViewer.Domain/ImageViewer.Domain.csproj", "src/Core/ImageViewer.Domain/"]
COPY ["src/Core/ImageViewer.Application/ImageViewer.Application.csproj", "src/Core/ImageViewer.Application/"]
COPY ["src/Infrastructure/ImageViewer.Infrastructure/ImageViewer.Infrastructure.csproj", "src/Infrastructure/ImageViewer.Infrastructure/"]

RUN dotnet restore "src/Services/ImageViewer.NotificationService/ImageViewer.NotificationService.csproj"

# Copy source files
COPY . .

WORKDIR "/src/src/Services/ImageViewer.NotificationService"
RUN dotnet build "ImageViewer.NotificationService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ImageViewer.NotificationService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ImageViewer.NotificationService.dll"]