FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /source

COPY ["src/TicketGuard.Api/TicketGuard.Api.csproj", "src/TicketGuard.Api/"]
COPY ["src/TicketGuard.Domain/TicketGuard.Domain.csproj", "src/TicketGuard.Domain/"]
COPY ["src/TicketGuard.Infrastructure/TicketGuard.Infrastructure.csproj", "src/TicketGuard.Infrastructure/"]
COPY ["src/TicketGuard.Application/TicketGuard.Application.csproj", "src/TicketGuard.Application/"]
COPY ["Directory.Build.props", "./"]

RUN dotnet restore "src/TicketGuard.Api/TicketGuard.Api.csproj"

COPY . .

RUN dotnet publish "src/TicketGuard.Api/TicketGuard.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR "/app"

COPY --from=build /app/publish .

USER $APP_UID

ENTRYPOINT ["dotnet", "TicketGuard.Api.dll"]