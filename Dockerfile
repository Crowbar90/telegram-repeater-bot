# ── Build stage ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY TelegramRepeaterBot.csproj .
RUN dotnet restore

COPY src/ ./src/
RUN dotnet publish -c Release -o /app/publish --no-restore

# ── Runtime stage ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app

# Run as non-root
RUN addgroup --system botgroup && adduser --system --ingroup botgroup botuser
USER botuser

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "TelegramRepeaterBot.dll"]
