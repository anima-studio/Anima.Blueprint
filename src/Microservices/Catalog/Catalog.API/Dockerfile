FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
ARG SERVICE_NAME=Catalog
WORKDIR /src

COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]

COPY ["src/Orchestration/ServiceDefaults/ServiceDefaults.csproj", "src/Orchestration/ServiceDefaults/"]
COPY ["src/Microservices/${SERVICE_NAME}/${SERVICE_NAME}.API/${SERVICE_NAME}.API.csproj", "src/Microservices/${SERVICE_NAME}/${SERVICE_NAME}.API/"]

RUN dotnet restore "src/Microservices/${SERVICE_NAME}/${SERVICE_NAME}.API/${SERVICE_NAME}.API.csproj"

COPY . .
WORKDIR "/src/src/Microservices/${SERVICE_NAME}/${SERVICE_NAME}.API"
RUN dotnet build "${SERVICE_NAME}.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
ARG SERVICE_NAME=Catalog
RUN dotnet publish "${SERVICE_NAME}.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ARG SERVICE_NAME=Catalog
ENTRYPOINT dotnet "${SERVICE_NAME}.API.dll"
