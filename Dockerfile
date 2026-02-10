# Use the official .NET SDK image for build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["MusicGen.Api/MusicGen.Api.csproj", "MusicGen.Api/"]
COPY ["MusicGen.Core/MusicGen.Core.csproj", "MusicGen.Core/"]
RUN dotnet restore "MusicGen.Api/MusicGen.Api.csproj"

# Copy the rest of the source code
COPY . .
WORKDIR "/src/MusicGen.Api"
RUN dotnet build "MusicGen.Api.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "MusicGen.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Install native dependencies for SkiaSharp on Linux
USER root
RUN apt-get update && apt-get install -y --no-install-recommends \
    libfontconfig1 \
    libfreetype6 \
    && rm -rf /var/lib/apt/lists/*

# Create directory for generated songs and set permissions for the 'app' user
RUN mkdir -p wwwroot/songs && chown -R app:app wwwroot

COPY --from=publish /app/publish .

# Switch to non-root user
USER app

ENTRYPOINT ["dotnet", "MusicGen.Api.dll"]
