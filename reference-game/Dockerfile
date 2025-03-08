#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["reference-game/ReferenceGame.csproj", "reference-game/"]
COPY ["host/Host.csproj", "host/"]
RUN dotnet restore "reference-game/ReferenceGame.csproj"
COPY . .
WORKDIR "/src/reference-game"
RUN dotnet build "ReferenceGame.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ReferenceGame.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ReferenceGame.dll"]