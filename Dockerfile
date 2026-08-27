# Stage 1: Build Tailwind CSS
FROM node:22-alpine AS tailwind
WORKDIR /src/Boodschap
COPY sources/Boodschap/package.json sources/Boodschap/package-lock.json ./
RUN npm ci --ignore-scripts
COPY sources/Boodschap/tailwind.config.js ./
COPY sources/Boodschap/Styles/ ./Styles/
COPY sources/Boodschap/Components/ ./Components/
COPY sources/Features/ ../Features/
COPY sources/Shared/ ../Shared/
COPY sources/Boodschap/wwwroot/ ./wwwroot/
RUN npm run build:css

# Stage 2: Build and publish .NET application
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_COMMIT
WORKDIR /src
COPY sources/Directory.Build.props sources/
COPY sources/Boodschap/Boodschap.csproj sources/Boodschap/
COPY sources/Shared/Boodschap.Shared.csproj sources/Shared/
COPY sources/Features/Authentication/Boodschap.Features.Authentication.csproj sources/Features/Authentication/
COPY sources/Features/ShoppingLists/Boodschap.Features.ShoppingLists.csproj sources/Features/ShoppingLists/
COPY sources/Features/Nutrition/Boodschap.Features.Nutrition.csproj sources/Features/Nutrition/
COPY sources/Features/Recipes/Boodschap.Features.Recipes.csproj sources/Features/Recipes/
COPY sources/Features/Updates/Boodschap.Features.Updates.csproj sources/Features/Updates/
RUN dotnet restore sources/Boodschap/Boodschap.csproj
COPY sources/ sources/
# Overwrite with the Tailwind-compiled CSS
COPY --from=tailwind /src/Boodschap/wwwroot/app.css sources/Boodschap/wwwroot/app.css
RUN dotnet publish sources/Boodschap/Boodschap.csproj -c Release -o /app/publish --no-restore

# Stage 3: Final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ConnectionStrings__Boodschap=Data Source=/app/App_Data/boodschap.db
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Boodschap.dll"]
