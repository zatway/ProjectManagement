# ---------- Базовый образ ----------
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
# Отключаем приглашение к телеметрии
ENV DOTNET_NOLOGO=true

# ---------- Сборка ----------
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

# Копируем только csproj и восстанавливаем зависимости (кэширование)
COPY ["Api/Api.csproj", "Api/"]
RUN dotnet restore "Api/Api.csproj" --disable-parallel

# Копируем всё остальное и собираем
COPY . .
WORKDIR "/src/Api"
RUN dotnet build "Api.csproj" -c Release --no-restore -o /app/build

# ---------- Публикация ----------
FROM build AS publish
RUN dotnet publish "Api.csproj" -c Release --no-restore -o /app/publish /p:UseAppHost=false

# ---------- Финальный образ ----------
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Права на выполнение (на всякий случай)
RUN chmod +x Api.dll

ENTRYPOINT ["dotnet", "Api.dll"]