# Etapa 1: Build del frontend
FROM node:20.11.1-alpine AS frontend-build
WORKDIR /app
COPY userlibrary-frontend/package*.json ./
RUN npm install
COPY userlibrary-frontend/ ./
RUN npm run build

# Etapa 2: Build del backend
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Application/Application.csproj ./Application/
COPY Infrastructure/Infrastructure.csproj ./Infrastructure/
COPY Domain/Domain.csproj ./Domain/
COPY UserLibraryBackEndApi/UserLibraryBackEndApi.csproj ./UserLibraryBackEndApi/

RUN dotnet restore ./UserLibraryBackEndApi/UserLibraryBackEndApi.csproj

COPY Application ./Application
COPY Infrastructure ./Infrastructure
COPY Domain ./Domain
COPY UserLibraryBackEndApi ./UserLibraryBackEndApi

# Copia las portadas (si quieres persistencia, usa volumen en docker-compose)
COPY ./UserLibraryBackEndApi/wwwroot/covers /app/wwwroot/covers

# Copia el build del frontend generado en la etapa 1
COPY --from=frontend-build /app/build ./UserLibraryBackEndApi/wwwroot

WORKDIR /src/UserLibraryBackEndApi
RUN dotnet publish -c Release -o /app/publish

# Etapa final: runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 80
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Docker
ENTRYPOINT ["dotnet", "UserLibraryBackEndApi.dll"]