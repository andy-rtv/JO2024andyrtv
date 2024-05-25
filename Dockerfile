# Utilise l'image ASP.NET Core runtime comme base
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

# Utilise l'image .NET SDK pour build l'application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["JO2024andyrtv.csproj", "./"]
RUN dotnet restore "./JO2024andyrtv.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "JO2024andyrtv.csproj" -c Release -o /app/build

# Publie l'application dans un dossier spécifique
FROM build AS publish
RUN dotnet publish "JO2024andyrtv.csproj" -c Release -o /app/publish

# Crée l'image finale basée sur l'image ASP.NET Core runtime
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY entrypoint.sh .
ENTRYPOINT ["./entrypoint.sh"]
