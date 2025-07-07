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
COPY ./UserLibraryBackEndApi/covers /app/covers

COPY ./UserLibraryBackEndApi/wwwroot /app/wwwroot

WORKDIR /src/UserLibraryBackEndApi
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 80
ENV ASPNETCORE_URLS=http://+:80
ENTRYPOINT ["dotnet", "UserLibraryBackEndApi.dll"]
