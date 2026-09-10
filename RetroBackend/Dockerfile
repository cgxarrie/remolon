# Stage 1: build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY *.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

# Stage 2: runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p /var/dataprotection-keys \
    && chown -R $APP_UID:$APP_UID /var/dataprotection-keys
ENV ASPNETCORE_URLS=http://+:8080
ENV DataProtection__KeysDirectory=/var/dataprotection-keys
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "RetroBackend.dll"]
