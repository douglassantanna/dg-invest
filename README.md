# Cryptocurrency Portfolio Tracker

## Overview

Welcome to the Cryptocurrency Portfolio Tracker! This application allows users to manage and analyze their cryptocurrency investments, featuring real-time tracking, detailed transaction logging, and comprehensive statistics. Built on Azure, the solution leverages Azure Web Apps, Static Web Apps, Azure SQL Database, and Azure Blob Storage to deliver a scalable and robust application.

## Features

- **Real-Time Portfolio Tracking**: Monitor cryptocurrency investments with up-to-date information.
- **Detailed Transaction Logging**: Record every transaction within the portfolio.
- **Comprehensive Statistics**: View detailed statistics including current values, percentage changes, and average prices.
- **Azure Integration**: Utilizes Azure Web App, Static Web App, and Azure SQL Database for reliable performance.
- **Logging with Azure Blob Storage**: Logs application activities and errors to Azure Blob Storage for efficient monitoring and troubleshooting.
- **CI/CD Pipeline**: Automated build and deployment process using Azure DevOps or GitHub Actions.

## Architecture

- **Frontend**: Angular application hosted as a Static Web App on Azure.
- **Backend**: .NET Web API running on Azure Web App, integrated with Azure Blob Storage for logging.
- **Database**: Azure SQL Database for storing transaction records and user data.
- **Logging**: Azure Blob Storage for storing application logs.
- **CI/CD**: Continuous integration and deployment pipelines for streamlined development and release.

## Getting Started

To build and run the application locally using Docker Compose, follow these steps:

### Prerequisites

- **Docker**: Install Docker for building and running containers.
- **Node.js and npm**: Required for building and running the Angular frontend.

### Setup

1. **Clone the Repository**

   ```bash
   git clone https://github.com/your-username/crypto-portfolio-tracker.git
   cd crypto-portfolio-tracker
   ```
2. **In development, please wait**
