FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source
COPY global.json Directory.Build.props ./
COPY src/ ./src/
RUN dotnet publish src/PayAhead.Api/PayAhead.Api.csproj -c Release -o /app/publish
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
USER app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "PayAhead.Api.dll"]
