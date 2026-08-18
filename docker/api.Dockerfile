FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/OncoBridge.Domain/OncoBridge.Domain.csproj src/OncoBridge.Domain/
COPY src/OncoBridge.Application/OncoBridge.Application.csproj src/OncoBridge.Application/
COPY src/OncoBridge.Interop.Fhir/OncoBridge.Interop.Fhir.csproj src/OncoBridge.Interop.Fhir/
COPY src/OncoBridge.Infrastructure/OncoBridge.Infrastructure.csproj src/OncoBridge.Infrastructure/
COPY src/OncoBridge.Api/OncoBridge.Api.csproj src/OncoBridge.Api/

RUN dotnet restore src/OncoBridge.Api/OncoBridge.Api.csproj

COPY src/OncoBridge.Domain/ src/OncoBridge.Domain/
COPY src/OncoBridge.Application/ src/OncoBridge.Application/
COPY src/OncoBridge.Interop.Fhir/ src/OncoBridge.Interop.Fhir/
COPY src/OncoBridge.Infrastructure/ src/OncoBridge.Infrastructure/
COPY src/OncoBridge.Api/ src/OncoBridge.Api/

RUN dotnet publish src/OncoBridge.Api/OncoBridge.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

COPY --from=build /app ./

USER $APP_UID

EXPOSE 8080

ENTRYPOINT ["dotnet", "OncoBridge.Api.dll"]
