# Stage 1: build the React app
FROM node:22 AS web
WORKDIR /web
COPY web/package.json web/package-lock.json ./
RUN npm ci
COPY web/ ./
RUN npm run build

# Stage 2: build the API
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/JobPilot.Api/JobPilot.Api.csproj src/JobPilot.Api/
RUN dotnet restore src/JobPilot.Api/JobPilot.Api.csproj
COPY src/JobPilot.Api src/JobPilot.Api
RUN dotnet publish src/JobPilot.Api/JobPilot.Api.csproj -c Release -o /app --no-restore

# Stage 3: small runtime image with the API and the built web app in wwwroot
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
COPY --from=web /web/dist ./wwwroot
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "JobPilot.Api.dll"]
