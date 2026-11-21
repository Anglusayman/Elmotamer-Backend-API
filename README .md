# Elmotamer – Backend API

The backend (Web API) of a mobile/web application built to **organize a church conference**. Built with **ASP.NET Core**, with **AI-powered lesson analysis** (Google Gemini) and **Firebase Admin SDK** integration, secured with **JWT authentication**.

## Features

- **JWT Authentication:** secure, token-based access to protected endpoints.
- **AI lesson analysis:** integrates with the Google Gemini API to analyze lesson content.
- **Firebase Admin SDK:** initialized safely from a credentials file; the API keeps running even if the file is missing.
- **Swagger UI:** interactive API documentation with built-in support for entering a JWT token.
- **CORS enabled:** ready to be consumed by web and mobile frontends.
- **Static files:** serves files from `wwwroot` directly through the API.

## Tech Stack

- **Language:** C#
- **Framework:** ASP.NET Core Web API
- **Database:** SQL Server with Entity Framework Core
- **Authentication:** JWT Bearer
- **External services:** Google Gemini API (via typed `HttpClient`), Firebase Admin SDK
- **Documentation:** Swagger (Swashbuckle)
- **IDE:** Visual Studio

## Architecture Highlights

- **Dependency Injection** for services (`ILessonAnalysisService`).
- **Typed `HttpClient`** for calling the Gemini API, with a custom timeout for long AI responses.
- **Separation of concerns:** controllers for endpoints, services for business logic, and a DbContext for data access.

## Project Structure

```
├── Controllers/     # API endpoints
├── DbContext/       # Entity Framework Core database context
├── Services/        # Business logic (Gemini lesson analysis)
├── wwwroot/         # Static files served by the API
└── Program.cs       # App startup, services, and middleware configuration
```

## Getting Started

### Requirements

- Visual Studio (with the ASP.NET workload) or the .NET SDK
- A SQL Server database
- A Google Gemini API key
- A Firebase service account file (optional, only needed for Firebase features)

### Configuration

For security reasons, `appsettings.json` and any file containing secrets (including `firebase-config.json`) are **not included** in this repository.

1. Create an `appsettings.json` file in the project root:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER,1433;Database=YOUR_DATABASE;User Id=YOUR_USER;Password=YOUR_PASSWORD;"
  },
  "Jwt": {
    "Key": "YOUR_LONG_SECRET_KEY_AT_LEAST_64_CHARACTERS"
  }
}
```

2. Provide your **Gemini API key** in the configuration used by `LessonAnalysisService`.
3. (Optional) Download a service account key from your Firebase project and save it as `firebase-config.json` next to the app's executable.

### Run

1. Open the project in Visual Studio.
2. Add your configuration files as shown above.
3. Press **F5** to run, or use `dotnet run` from the terminal.
4. Open `/swagger` in your browser to explore and test the API. Use the **Authorize** button to paste your token as `Bearer {token}`.

## My Role

I developed the backend of this project: the API endpoints, the database layer, authentication, and the integration with external services.

## Author

Backend Developer – C#, C++, SQL
