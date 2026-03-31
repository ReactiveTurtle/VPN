FROM node:22-bookworm-slim AS web-build
WORKDIR /src/src/VpnPortal.Web
COPY src/VpnPortal.Web/package.json src/VpnPortal.Web/package-lock.json ./
RUN npm ci
COPY src/VpnPortal.Web/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS publish
WORKDIR /src
COPY . .
RUN dotnet restore src/VpnPortal.Api/VpnPortal.Api.csproj
RUN dotnet publish src/VpnPortal.Api/VpnPortal.Api.csproj -c Release -o /out/api --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /out/api ./
COPY --from=web-build /src/src/VpnPortal.Web/dist/VpnPortal.Web/browser/ ./wwwroot/
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "VpnPortal.Api.dll"]
