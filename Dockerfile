FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/JobPilot.Api/JobPilot.Api.csproj src/JobPilot.Api/
RUN dotnet restore src/JobPilot.Api/JobPilot.Api.csproj
COPY src/JobPilot.Api src/JobPilot.Api
RUN dotnet publish src/JobPilot.Api/JobPilot.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "JobPilot.Api.dll"]
