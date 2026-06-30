# syntax=docker/dockerfile:1

# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution + project files first for restore-layer caching.
COPY DsmrHub.slnx ./
COPY DsmrHub/DsmrHub.csproj DsmrHub/
COPY DsmrHub.Application/DsmrHub.Application.csproj DsmrHub.Application/
COPY DsmrHub.Domain/DsmrHub.Domain.csproj DsmrHub.Domain/
COPY DsmrHub.Infrastructure/DsmrHub.Infrastructure.csproj DsmrHub.Infrastructure/
COPY DsmrHub.Tests/DsmrHub.Tests.csproj DsmrHub.Tests/
RUN dotnet restore DsmrHub/DsmrHub.csproj

# Copy the rest of the source and publish (framework-dependent).
COPY . .
RUN dotnet publish DsmrHub/DsmrHub.csproj -c Release -o /app/publish

# ---- Runtime stage ----
# The aspnet image (not the smaller runtime image) is required: DsmrHub uses Microsoft.NET.Sdk.Web,
# which references the Microsoft.AspNetCore.App shared framework that the bare runtime image lacks.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Run as the non-root user shipped in the base image.
USER $APP_UID

COPY --from=build /app/publish .

# Read-only web dashboard (DashboardOptions.Port; override with -e DashboardOptions__Port=...).
EXPOSE 8080

# Supply real config via environment variables. The P1 meter is read from a serial port, so on Linux
# point ComPort at the passed-through device and run with `--device`, e.g.
#   docker run --device=/dev/ttyUSB0 -e DsmrOptions__ComPort=/dev/ttyUSB0 -p 8080:8080 dsmrhub
# To smoke-test without hardware, run the built-in synthetic simulator:
#   docker run -e DsmrOptions__UseSimulator=true -p 8080:8080 dsmrhub
ENTRYPOINT ["dotnet", "DsmrHub.dll"]
