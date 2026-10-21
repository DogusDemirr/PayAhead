FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source
COPY global.json Directory.Build.props ./
COPY src/PayAhead.Domain/*.csproj src/PayAhead.Domain/
COPY src/PayAhead.Application/*.csproj src/PayAhead.Application/
COPY src/PayAhead.Infrastructure/*.csproj src/PayAhead.Infrastructure/
COPY src/PayAhead.Api/*.csproj src/PayAhead.Api/
RUN dotnet restore src/PayAhead.Api/PayAhead.Api.csproj
COPY src/ ./src/
RUN dotnet publish src/PayAhead.Api/PayAhead.Api.csproj --no-restore -c Release -o /app/publish /p:UseAppHost=false
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
USER app
ENV ASPNETCORE_HTTP_PORTS=8080
ENV DOTNET_EnableDiagnostics=0
STOPSIGNAL SIGTERM
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 CMD ["dotnet", "PayAhead.Api.dll", "--health-probe"]
EXPOSE 8080
ENTRYPOINT ["dotnet", "PayAhead.Api.dll"]
