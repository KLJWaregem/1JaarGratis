FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY EenJaarGratis.csproj .
RUN dotnet restore EenJaarGratis.csproj

COPY . .
RUN dotnet publish EenJaarGratis.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Base image ships a non-root "app" user (uid/gid in $APP_UID) for exactly this purpose,
# so use that instead of adduser/useradd, which aren't present on every base OS variant.
RUN mkdir -p /app/data && chown -R $APP_UID:$APP_UID /app

COPY --from=build --chown=$APP_UID:$APP_UID /app/publish .

USER $APP_UID

ENV ASPNETCORE_URLS=http://+:8080 \
    ConnectionStrings__Default="Data Source=/app/data/eenjaargratis.db"

EXPOSE 8080
VOLUME /app/data

ENTRYPOINT ["dotnet", "EenJaarGratis.dll"]
