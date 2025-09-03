# Etapa de build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar los csproj y restaurar dependencias
COPY Application/Application.csproj ./Application/
COPY Infrastructure/Infrastructure.csproj ./Infrastructure/
COPY Domain/Domain.csproj ./Domain/
COPY UserLibraryBackEndApi/UserLibraryBackEndApi.csproj ./UserLibraryBackEndApi/

RUN dotnet restore ./UserLibraryBackEndApi/UserLibraryBackEndApi.csproj

# Copiar el resto del código
COPY Application ./Application
COPY Infrastructure ./Infrastructure
COPY Domain ./Domain
COPY UserLibraryBackEndApi ./UserLibraryBackEndApi

WORKDIR /src/UserLibraryBackEndApi

RUN dotnet publish -c Release -o /app/publish

# Etapa final
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# Copiar recursos estáticos y el certificado
# COPY UserLibraryBackEndApi/cert.pfx /app/
COPY UserLibraryBackEndApi/wwwroot /app/wwwroot

EXPOSE 80
ENV ASPNETCORE_URLS=http://+:80
ENTRYPOINT ["dotnet", "UserLibraryBackEndApi.dll"]
