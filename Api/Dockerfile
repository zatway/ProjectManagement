# ---------- Base ----------
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_NOLOGO=true
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1

FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

# Копируем .sln и ВСЕ .csproj файлы (это важно для правильного кэширования!)
COPY ProjectManagement.sln .
COPY Api/Api.csproj ./Api/
COPY Application/Application.csproj ./Application/
COPY Domain/Domain.csproj ./Domain/
COPY Infrastructure/Infrastructure.csproj ./Infrastructure/
# Test не копируем — он не нужен для публикации Api

RUN dotnet restore ProjectManagement.sln

COPY . .

WORKDIR /src/Api
RUN dotnet build "Api.csproj" -c Release --no-restore -o /app/build

FROM build AS publish
WORKDIR /src/Api
RUN dotnet publish "Api.csproj" -c Release --no-restore -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

RUN chmod +x Api.dll

ENTRYPOINT ["dotnet", "Api.dll"]