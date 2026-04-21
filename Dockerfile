FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
ARG SERVICE_NAME
ARG SERVICE_PATH
WORKDIR /src

# Copy solution and nuget config
COPY nuget.config ./
COPY PaymentGateway.sln ./

# Copy all csproj files preserving directory structure
COPY src/aspire/PaymentGateway.ServiceDefaults/PaymentGateway.ServiceDefaults.csproj src/aspire/PaymentGateway.ServiceDefaults/
COPY src/aspire/PaymentGateway.AppHost/PaymentGateway.AppHost.csproj src/aspire/PaymentGateway.AppHost/
COPY src/shared/PaymentGateway.Domain/PaymentGateway.Domain.csproj src/shared/PaymentGateway.Domain/
COPY src/shared/PaymentGateway.Contracts/PaymentGateway.Contracts.csproj src/shared/PaymentGateway.Contracts/
COPY src/services/PaymentGateway.Identity.Api/PaymentGateway.Identity.Api.csproj src/services/PaymentGateway.Identity.Api/
COPY src/services/PaymentGateway.Billing.Api/PaymentGateway.Billing.Api.csproj src/services/PaymentGateway.Billing.Api/
COPY src/services/PaymentGateway.Gateway.Api/PaymentGateway.Gateway.Api.csproj src/services/PaymentGateway.Gateway.Api/
COPY src/services/PaymentGateway.Backoffice.Api/PaymentGateway.Backoffice.Api.csproj src/services/PaymentGateway.Backoffice.Api/
COPY tests/PaymentGateway.Identity.Api.Tests/PaymentGateway.Identity.Api.Tests.csproj tests/PaymentGateway.Identity.Api.Tests/
COPY tests/PaymentGateway.Billing.Api.Tests/PaymentGateway.Billing.Api.Tests.csproj tests/PaymentGateway.Billing.Api.Tests/
COPY tests/PaymentGateway.Gateway.Api.Tests/PaymentGateway.Gateway.Api.Tests.csproj tests/PaymentGateway.Gateway.Api.Tests/
COPY tests/PaymentGateway.Backoffice.Api.Tests/PaymentGateway.Backoffice.Api.Tests.csproj tests/PaymentGateway.Backoffice.Api.Tests/
COPY tests/PaymentGateway.Integration.Tests/PaymentGateway.Integration.Tests.csproj tests/PaymentGateway.Integration.Tests/

# Restore only the target service (pulls in transitive deps)
RUN dotnet restore "src/services/${SERVICE_NAME}/${SERVICE_NAME}.csproj"

# Copy full source
COPY src/ src/

# Publish
RUN dotnet publish "src/services/${SERVICE_NAME}/${SERVICE_NAME}.csproj" \
    -c Release -o /app/publish --no-restore

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
ARG SERVICE_NAME
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Development
ENV ASPNETCORE_HTTP_PORTS=8080
ENV DLL_NAME=${SERVICE_NAME}.dll

EXPOSE 8080
ENTRYPOINT ["sh", "-c", "dotnet $DLL_NAME"]
