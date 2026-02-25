# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Bangkok.sln ./
COPY Bangkok.Api/Bangkok.Api.csproj Bangkok.Api/
COPY Bangkok.Application/Bangkok.Application.csproj Bangkok.Application/
COPY Bangkok.Domain/Bangkok.Domain.csproj Bangkok.Domain/
COPY Bangkok.Infrastructure/Bangkok.Infrastructure.csproj Bangkok.Infrastructure/

RUN dotnet restore Bangkok.Api/Bangkok.Api.csproj

COPY Bangkok.Api/ Bangkok.Api/
COPY Bangkok.Application/ Bangkok.Application/
COPY Bangkok.Domain/ Bangkok.Domain/
COPY Bangkok.Infrastructure/ Bangkok.Infrastructure/

RUN dotnet publish Bangkok.Api/Bangkok.Api.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Listen on 8080 for Linux/VPS
ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

# Non-root user
RUN adduser --disabled-password --gecos "" --uid 1000 appuser \
    && mkdir -p /app/Logs \
    && chown -R appuser:appuser /app

USER appuser

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Bangkok.Api.dll"]
