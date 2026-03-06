FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release

# Install Node.js for frontend build
RUN apt-get update && apt-get install -y curl && \
    curl -fsSL https://deb.nodesource.com/setup_22.x | bash - && \
    apt-get install -y nodejs && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /src
COPY ["BPM.Client/BPM.Client.csproj", "BPM.Client/"]
COPY ["BPM.Core/BPM.Core.csproj", "BPM.Core/"]
COPY ["BPM.UI/BPM.UI.csproj", "BPM.UI/"]
RUN dotnet restore "BPM.Client/BPM.Client.csproj"
COPY . .

# Build React frontend
WORKDIR "/src/BPM.UI/frontend"
RUN npm install
RUN npm run build

# Build .NET
WORKDIR "/src/BPM.Client"
RUN dotnet build "BPM.Client.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR "/src/BPM.Client"
RUN dotnet publish "BPM.Client.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BPM.Client.dll"]
