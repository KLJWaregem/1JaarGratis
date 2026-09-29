FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY EenJaarGratis.csproj .
RUN dotnet restore EenJaarGratis.csproj

COPY . .
RUN dotnet publish EenJaarGratis.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN adduser --disabled-password --home /app --gecos '' appuser \
    && mkdir -p /app/data \
    && chown -R appuser:appuser /app

COPY --from=build --chown=appuser:appuser /app/publish .

USER appuser

ENV ASPNETCORE_URLS=http://+:8080 \
    ConnectionStrings__Default="Data Source=/app/data/eenjaargratis.db"

EXPOSE 8080
VOLUME /app/data

ENTRYPOINT ["dotnet", "EenJaarGratis.dll"]
