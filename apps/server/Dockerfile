FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY server.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish server.csproj -c Release -o /app/publish /p:UseAppHost=false -p:RunNSwag=False

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ARG ENVIRONMENT=Release
ENV ASPNETCORE_ENVIRONMENT=${ENVIRONMENT}

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "server.dll"]
