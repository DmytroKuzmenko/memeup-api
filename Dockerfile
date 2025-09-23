# ---------- base (runtime) ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://0.0.0.0:8080

# ---------- dev (hot reload via dotnet watch) ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS dev
WORKDIR /app
# Restore чтобы ускорить watch при старте (опционально)
COPY ./src/Memeup.Api/*.csproj ./src/Memeup.Api/
RUN dotnet restore ./src/Memeup.Api/Memeup.Api.csproj
WORKDIR /app/src/Memeup.Api
# Команду для dev переопределим в docker-compose (dotnet watch)

# ---------- build (publish) ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ./src/Memeup.Api/*.csproj ./Memeup.Api/
RUN dotnet restore ./Memeup.Api/Memeup.Api.csproj
COPY ./src/Memeup.Api/ ./Memeup.Api/
WORKDIR /src/Memeup.Api
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# ---------- final (prod image) ----------
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
# Папка для загрузок (будет передана volume, но на всякий случай создадим)
RUN mkdir -p /app/uploads
VOLUME ["/app/uploads"]
ENTRYPOINT ["dotnet", "Memeup.Api.dll"]
