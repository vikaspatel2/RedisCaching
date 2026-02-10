# -------- BUILD STAGE --------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files only (no solution file)
COPY RedisCaching/*.csproj RedisCaching/

# Restore dependencies for the project
RUN dotnet restore RedisCaching/RedisCaching.csproj

# Copy all source files
COPY . .

WORKDIR /src/RedisCaching

# Build and publish the app
RUN dotnet publish -c Release -o /app/publish

# -------- RUNTIME STAGE --------
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

# Copy published app from build stage
COPY --from=build /app/publish .

# Expose port 80
EXPOSE 80

# Run the app
ENTRYPOINT ["dotnet", "RedisCaching.dll"]
