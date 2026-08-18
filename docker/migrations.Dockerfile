FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY global.json Directory.Build.props Directory.Packages.props ./
COPY .config/dotnet-tools.json .config/
COPY src/OncoBridge.Domain/OncoBridge.Domain.csproj src/OncoBridge.Domain/
COPY src/OncoBridge.Application/OncoBridge.Application.csproj src/OncoBridge.Application/
COPY src/OncoBridge.Infrastructure/OncoBridge.Infrastructure.csproj src/OncoBridge.Infrastructure/

RUN dotnet tool restore
RUN dotnet restore src/OncoBridge.Infrastructure/OncoBridge.Infrastructure.csproj

COPY src/OncoBridge.Domain/ src/OncoBridge.Domain/
COPY src/OncoBridge.Application/ src/OncoBridge.Application/
COPY src/OncoBridge.Infrastructure/ src/OncoBridge.Infrastructure/

RUN dotnet ef migrations bundle \
    --project src/OncoBridge.Infrastructure \
    --startup-project src/OncoBridge.Infrastructure \
    --configuration Release \
    --output /app/efbundle \
    --force

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime

WORKDIR /app

COPY --from=build /app/efbundle ./efbundle

USER $APP_UID

ENTRYPOINT ["/bin/sh", "-c", "exec ./efbundle --connection \"$ConnectionStrings__OncoBridge\""]
