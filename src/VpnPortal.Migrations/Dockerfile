FROM mcr.microsoft.com/dotnet/sdk:10.0 AS publish
WORKDIR /src
COPY . .
RUN dotnet restore src/VpnPortal.Migrations/VpnPortal.Migrations.csproj
RUN dotnet publish src/VpnPortal.Migrations/VpnPortal.Migrations.csproj -c Release -o /out/migrations --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /out/migrations ./
ENTRYPOINT ["dotnet", "VpnPortal.Migrations.dll"]
