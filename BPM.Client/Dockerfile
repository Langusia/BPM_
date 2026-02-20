FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["BPM.Client/BPM.Client.csproj", "BPM.Client/"]
COPY ["Core.BPM/Core.BPM.csproj", "Core.BPM/"]
RUN dotnet restore "BPM.Client/BPM.Client.csproj"
COPY . .
WORKDIR "/src/BPM.Client"
RUN dotnet build "BPM.Client.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "BPM.Client.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BPM.Client.dll"]
