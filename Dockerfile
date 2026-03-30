# Stage 1: Build Tailwind CSS
FROM node:22-alpine AS tailwind
WORKDIR /src
COPY package.json ./
RUN npm install
COPY tailwind.config.js ./
COPY Styles/ ./Styles/
COPY Components/ ./Components/
COPY wwwroot/ ./wwwroot/
RUN npm run build:css

# Stage 2: Build and publish .NET application
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Boodschap.csproj ./
RUN dotnet restore
COPY . .
# Overwrite with the Tailwind-compiled CSS
COPY --from=tailwind /src/wwwroot/app.css ./wwwroot/app.css
RUN dotnet publish -c Release -o /app/publish --no-restore

# Stage 3: Final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Boodschap.dll"]
