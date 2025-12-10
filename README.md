# LoadHitch

**Modern Logistics Marketplace Platform**

A comprehensive web application that connects shippers with independent truck drivers, streamlining the freight matching process and maximizing operational efficiency.

**[► View Live Application](https://loadhitch.azurewebsites.net)**

---

## Table of Contents
1. [Overview](#overview)
2. [Key Features](#key-features)
3. [Technology Stack](#technology-stack)
4. [Getting Started](#getting-started)
5. [Deployment](#deployment)
6. [Testing](#testing)
7. [Demo Credentials](#demo-credentials)
8. [License](#license)

---

## Overview

LoadHitch is an enterprise-grade logistics platform designed to solve the inefficiencies in the freight industry. By providing a transparent marketplace where shippers can post loads and drivers can showcase their availability, LoadHitch eliminates empty miles and reduces operational costs for all stakeholders.

### Business Value

- **For Shippers:** Instant access to a network of verified drivers with real-time availability
- **For Drivers:** Maximize revenue by minimizing deadhead miles and finding optimal routes
- **For Logistics Managers:** Centralized dashboard for monitoring all shipments and fleet operations

## Key Features

### Authentication & Security
-   🔐 **Multi-Role Authentication:** Secure registration and login for Customers, Drivers, and Administrators
-   🔑 **OAuth 2.0 Integration:** Streamlined Google Sign-In for enhanced user convenience
-   🛡️ **JWT Token-Based Security:** Industry-standard authentication with refresh token support

### Core Functionality
-   📦 **Load Management:** Complete CRUD operations for shippers to post, edit, and manage freight listings
-   🚛 **Fleet Management:** Driver dashboard for managing truck availability, routes, and schedules
-   🤝 **Smart Booking System:** Automated matching and booking workflow between loads and trucks
-   🗺️ **Interactive Mapping:** Real-time location tracking and route visualization with Mapbox integration
-   📊 **Analytics Dashboard:** Comprehensive metrics and reporting for administrators

### Platform Capabilities
-   📱 **Fully Responsive Design:** Optimized experience across desktop, tablet, and mobile devices
-   ⚡ **Real-Time Updates:** Live status changes and notifications using Blazor Server
-   🌍 **Geographic Optimization:** Location-based load matching for Cape Town and surrounding areas
-   📧 **Automated Notifications:** Email alerts for booking confirmations and status updates

## Technology Stack

### Frontend
-   **Framework:** .NET 8 Blazor Server
-   **UI Components:** Bootstrap 5, Blazor Component Library
-   **Mapping:** Mapbox GL JS
-   **Real-time Communication:** SignalR

### Backend
-   **Runtime:** ASP.NET Core 8.0
-   **API Architecture:** RESTful APIs with MVC Controllers
-   **Authentication:** ASP.NET Core Identity + OAuth 2.0
-   **Authorization:** Role-based access control (RBAC)

### Database & ORM
-   **Database:** PostgreSQL 14+ (Azure Database for PostgreSQL)
-   **ORM:** Entity Framework Core 8.0
-   **Migrations:** Code-first with EF Core Migrations

### DevOps & Deployment
-   **Cloud Platform:** Microsoft Azure
-   **Hosting:** Azure App Service (Linux)
-   **CI/CD:** GitHub Actions
-   **Monitoring:** Azure Application Insights

### Quality Assurance
-   **Unit Testing:** xUnit
-   **Component Testing:** bUnit
-   **E2E Testing:** Playwright
-   **Code Coverage:** Coverlet

---

## Testing

### Comprehensive Test Suite

LoadHitch includes a robust testing framework covering unit, integration, and end-to-end scenarios.

### Unit & Integration Tests

Run all tests with the unified test runner:

```powershell
# Run all tests (unit + integration)
.\scripts\run-tests.ps1 -All

# Run only unit tests
.\scripts\run-tests.ps1 -Unit

# Run only integration tests
.\scripts\run-tests.ps1 -Integration
```

### End-to-End Testing with Playwright

**Initial Setup:**

```powershell
cd t12Project.Playwright
dotnet tool restore
dotnet tool run playwright install
```

**Run E2E Tests:**

```powershell
# Start app and run tests
.\scripts\run-e2e.ps1

# Use existing running app
.\scripts\run-e2e.ps1 -NoStart

# Custom configuration
.\scripts\run-e2e.ps1 -BaseUrl 'https://localhost:7218' -InstallBrowsers
```

### Test Artifacts & Debugging

Test runs generate comprehensive artifacts in `./artifacts/`:

- **Test Results:** `unit-tests.trx`, `e2e-test-results.trx`
- **Logs:** `unit-tests.log`, `test-output.log`
- **Playwright Traces:** Interactive debugging with `playwright show-trace ./artifacts/playwright/trace-*.zip`
- **Screenshots & Videos:** Visual regression testing artifacts
- **Triage Bundles:** Automatic failure investigation packages (`triage-YYYYMMDD-HHMMSS.zip`)

### Coverage Reports

Generate code coverage reports:

```bash
dotnet test --collect:"XPlat Code Coverage"
```

---



## Deployment

### Automated Deployment to Azure

The repository includes a PowerShell deployment script for streamlined Azure deployment:

```powershell
.\deploy.ps1
```

The script handles:
- ✅ Azure CLI authentication verification
- ✅ Application build and publish (Release configuration)
- ✅ Artifact cleanup and optimization
- ✅ Zip package creation
- ✅ Azure Web App deployment
- ✅ Application restart and health check

### Manual Deployment Steps

1. **Build and publish:**
   ```bash
   dotnet publish -c Release -o ./publish
   ```

2. **Deploy to Azure:**
   ```bash
   az webapp deploy --name loadhitch --resource-group LoadHitch --src-path ./deploy.zip --type zip
   ```

3. **Restart the application:**
   ```bash
   az webapp restart --name loadhitch --resource-group LoadHitch
   ```

### Post-Deployment Configuration

After deployment, configure the following in Azure Portal:

1. **Application Settings** → Add connection strings and JWT configuration
2. **Authentication** → Configure Google OAuth redirect URIs
3. **Monitoring** → Enable Application Insights for production monitoring
4. **Custom Domain** (optional) → Configure SSL certificate and custom domain

---



## Getting Started

### Prerequisites

Ensure you have the following installed:
-   [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (version 8.0 or later)
-   [PostgreSQL 14+](https://www.postgresql.org/download/)
-   IDE: [Visual Studio 2022](https://visualstudio.microsoft.com/vs/) or [Visual Studio Code](https://code.visualstudio.com/) with C# extension

### Local Development Setup

1.  **Clone the repository:**
    ```bash
    git clone https://github.com/BrightonDube/LoadHitchZa.git
    cd LoadHitchZa
    ```

2.  **Configure Database Connection:**
    
    Create or update `appsettings.Development.json`:
    ```json
    {
      "ConnectionStrings": {
        "DefaultConnection": "Host=localhost;Database=loadhitch_dev;Username=your_username;Password=your_password"
      },
      "Jwt": {
        "SigningKey": "your-secure-signing-key-min-32-chars",
        "Issuer": "https://localhost:7218",
        "Audience": "loadhitch-api"
      }
    }
    ```

3.  **Initialize Database:**
    ```bash
    dotnet restore
    dotnet ef database update
    ```
    
    This will create the database schema and seed initial data (admin user, sample drivers, and loads).

4.  **Run the Application:**
    ```bash
    dotnet run
    ```
    
    The application will be available at:
    - HTTPS: `https://localhost:7218`
    - HTTP: `http://localhost:5000`

### Environment Variables

For production deployment, configure these environment variables:

```bash
ConnectionStrings__DefaultConnection="Server=your-server;Database=your-db;..."
Jwt__SigningKey="your-production-signing-key"
Jwt__Issuer="https://your-domain.com"
Jwt__Audience="loadhitch-api"
Authentication__Google__ClientId="your-google-client-id"
Authentication__Google__ClientSecret="your-google-client-secret"
ASPNETCORE_ENVIRONMENT="Production"
```

---

## Demo Credentials

Use these pre-configured accounts to explore the application:

### Administrator Access
```
Email:    admin@loadhitch.com
Password: Admin@123456!
Access:   Full system administration, user management, analytics
```

### Driver Accounts
```
Email:    driver1@loadhitch.com
Password: Driver@123456!
Fleet:    Flatbed truck (T001)

Email:    driver2@loadhitch.com
Password: Driver@123456!
Fleet:    Box truck (T002)

Email:    driver3@loadhitch.com
Password: Driver@123456!
Fleet:    Tanker truck (T003)

Email:    driver4@loadhitch.com
Password: Driver@123456!
Fleet:    Refrigerated truck (T004)
```

### Customer Accounts
```
Email:    customer1@loadhitch.com
Password: Customer@123456!
Loads:    Electronics shipment, Steel beams

Email:    customer2@loadhitch.com
Password: Customer@123456!
Loads:    Liquid chemicals

Email:    customer3@loadhitch.com
Password: Customer@123456!
Loads:    Frozen food, Furniture
```

---

## Architecture

### Application Structure

```
LoadHitchZa/
├── Components/           # Blazor components and pages
│   ├── Pages/           # Routable pages (Driver, Customer, Admin dashboards)
│   ├── Layout/          # Application layout and navigation
│   └── Shared/          # Reusable UI components
├── Controllers/         # API controllers
├── Data/               # DbContext and data access
├── Models/             # Domain models and DTOs
├── Services/           # Business logic and services
├── Migrations/         # EF Core database migrations
└── wwwroot/           # Static assets (CSS, JS, images)
```

### Database Schema

Key entities:
- **Users** (ApplicationUser with role-based access)
- **Loads** (Freight listings with status tracking)
- **Trucks** (Driver fleet information)
- **Bookings** (Load-to-truck assignments)
- **ActivityLogs** (Audit trail)
- **RefreshTokens** (JWT token management)

---

## Contributing

We welcome contributions! Please follow these guidelines:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

### Code Standards
- Follow C# coding conventions
- Write unit tests for new features
- Update documentation as needed
- Ensure all tests pass before submitting PR

---

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.

---

## Support & Contact

For questions, issues, or feature requests:
- **GitHub Issues:** [https://github.com/BrightonDube/LoadHitchZa/issues](https://github.com/BrightonDube/LoadHitchZa/issues)
- **Live Demo:** [https://loadhitch.azurewebsites.net](https://loadhitch.azurewebsites.net)

---

**Built with ❤️ using .NET 8 and Blazor Server**
