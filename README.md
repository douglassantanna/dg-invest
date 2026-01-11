### Overview
This project is a full cryptocurrency investment manager and analytics suite that I’ve been sculpting over the last two years. It brings together real-time price tracking, detailed transaction logging, and rich statistical insight to help you keep a steady hand in the stormy seas of digital assets.

---
### Getting started
#### Prerequisites
You can run the project either directly on your machine or inside Docker.
#### Option 1: Local Environment
- .NET 8
- Node.js v18+
- Angular v18
- SQL Server
- Azure Functions Core Tools
- EF Core tools
- **Azurite** (for local Azure Blob + Queue storage emulation)
Azurite can be installed as a VS Code extension or via npm.
#### Option 2: Docker Environment
- Docker Desktop

---
### Configuration
Before running the API you need to configure the settings file:

`dg-invest/api/api/appsettings.json`

```json
{
  "AzureStorageSettings": {
    "ConnectionString": "YourConnectionStringHere",
    "ContainerName": "YourContainerNameHere",
    "WelcomeEmailQueue": "welcomeemail"
  },
  "CoinMarketCapSettings": {
    "ApiKey": "YourCoinMarketCapApiKeyHere",
    "BaseUrl": "https://pro-api.coinmarketcap.com",
    "Header": "X-CMC_PRO_API_KEY"
  },
  "JWTSettings": {
    "ExpiryMinutes": 120,
    "Issuer": "http://my-local-host.com",
    "Secret": "YourJwtSecretHere"
  },
  "RateLimiterSettings": {
    "RequestsPermitLimit": 320,
    "WindowLimitInMinutes": 10
  },
  "RunMigrations": false,
  "Serilog": {
    "Enrich": [
      "FromLogContext",
      "WithMachineName",
      "WithThreadId"
    ],
    "MinimumLevel": {
      "Default": "Error",
      "Override": {
        "Microsoft": "Error",
        "System": "Error"
      }
    },
    "Using": [
      "Serilog.Sinks.AzureBlobStorage"
    ],
    "WriteTo": [
      {
        "Name": "AzureBlobStorage",
        "Args": {
          "connectionString": "YourSerilogConnectionStringHere",
          "restrictedToMinimumLevel": "Information",
          "storageContainerName": "logs",
          "storageFileName": "log-{yyyy}-{MM}-{dd}.json"
        }
      }
    ]
  },
  "ConnectionStrings": {
    "DefaultConnection": "YourSqlAzureConnectionStringHere"
  }
}
```

---
#### Running with Docker
1. Clone the repository
```bash
git clone https://github.com/douglassantanna/dg-invest.git
cd dg-invest
```
2. Run the application
 ```bash
docker compose up
```

Docker will launch:
- SQL Server
- API Service
- Angular frontend

Once everything settles, open:
https://localhost:4200

Use these credentials:
- email: admin@admin.com
- password: admin123

---
#### Running everything locally (standalone)
#### 1. Installation steps
Install SQL Server Express, Azure Functions Core Tools, Azurite (VS Code extension or npm), .NET 8, EF Core tools, Node.js V18+, NPM and Angular CLI V18
#### 2. Set up the database
1. Create a database named `dg-invest` in SQL Server
2. Copy its connection string into `appsettings.json` under `ConnectionString:Default`
#### 3. Apply EF migrations
```bash
cd dg-invest/api/api
dotnet ef database update
``` 
#### 4. Start Azurite (local Azure storage emulator)
1. In VS Code, open the command palette and type `Azurite: Start`
2. Azurite will print its local connections string in the output panel. Copy that string. For reference, check [Documentation](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite)
3. Add the Azurite connection string into the `appsettings.json` under `AzureStorageSettings:ConnectionString`
#### 5. Run the API
```bash
cd dg-invest/api/api
dotnet run
```
#### 6. Start the Azure Functions
```bash
cd dg-invest/api/functions
func start
```
#### 7. Start the Angular frontend
```bash
cd dg-invest/web-app
ng s -o
```

The frontend launches automatically. Access the application using the credentials
- email: admin@admin.com
- password: admin123

---
### Project structure
```bash
dg-invest/
│
├── .github/
│   └── workflows/               # CI/CD pipelines
│
├── api/
│   ├── api/                     # .NET 8 Web API
│   ├── functions/               # Azure Functions (background workers)
│   ├── unit-tests/              # Automated tests
│   ├── dg-invest.api.sln        # Solution file
│   └── Dockerfile               # API + Functions Docker image
│
├── web-app/
│   ├── cypress/                 # E2E tests
│   ├── src/                     # Angular 18 application
│   ├── Dockerfile               # Frontend Docker image
│   └── package.json
│
├── docker-compose.yml           # Full stack environment
└── README.md
```
