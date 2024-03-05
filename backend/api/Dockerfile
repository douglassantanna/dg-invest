FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build

WORKDIR /src

COPY . .

RUN dotnet restore "api.csproj"

COPY . .

RUN dotnet build "api.csproj" -c Release -o /app

RUN dotnet publish "api.csproj" -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS runtime

WORKDIR /app

COPY --from=build /app .

EXPOSE 80

# Define the entry point for the application
ENTRYPOINT ["dotnet", "api.dll"]
