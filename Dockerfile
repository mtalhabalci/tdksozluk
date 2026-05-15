# Use the .NET 9 SDK image for building the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build-env
WORKDIR /AspnetCoreMvcStarter

# Install Node.js 20 (gulp/webpack frontend build için)
RUN curl -fsSL https://deb.nodesource.com/setup_20.x | bash - \
    && apt-get install -y nodejs \
    && rm -rf /var/lib/apt/lists/*

# Copy everything
COPY . ./

# Frontend assets (gulp): node_modules + production build
RUN npm install --legacy-peer-deps \
    && npm run build:prod \
    && npm run build:prod:css \
    && npm run build:prod:fonts

# Restore as distinct layers
RUN dotnet restore

# Copy the entire project and build
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /AspnetCoreMvcStarter
COPY --from=build-env /AspnetCoreMvcStarter/out ./

# Expose port 5050 for the application
EXPOSE 80

# Set the entry point for the container
ENTRYPOINT ["dotnet", "AspnetCoreMvcStarter.dll"]

# Display a message in the terminal during build
RUN echo  -----------------------------------------------
RUN echo  Application is running at http://localhost:5050
RUN echo  -----------------------------------------------
