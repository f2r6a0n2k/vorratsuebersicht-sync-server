FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/Vorratsuebersicht.SyncServer/Vorratsuebersicht.SyncServer.csproj .
RUN dotnet restore

COPY src/Vorratsuebersicht.SyncServer/ .
RUN dotnet publish -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=build /app .
EXPOSE 5191
ENV ASPNETCORE_URLS=http://0.0.0.0:5191
ENV Server__DatabasePath=/data/vorratsuebersicht.db

VOLUME /data
ENTRYPOINT ["./Vorratsuebersicht.SyncServer"]
