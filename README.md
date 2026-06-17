![calendariq](https://github.com/user-attachments/assets/b91a9bf6-0ca7-4387-aad5-9a3625f3a66b)
## Description

CalendarIQ is a web-based calendar application that integrates artificial intelligence to automate scheduling and event management. Originally built with a Node.js and Express backend, the application has been migrated to an enterprise-grade ASP.NET Core Web API using .NET 10, PostgreSQL, and Entity Framework Core. The React 19 frontend remains unchanged, interacting with the .NET backend through strict API contract parity.

## Problem Statement

Traditional calendar applications rely heavily on manual data entry, requiring users to explicitly fill out forms for every event, title, date, and category. This manual process introduces several inefficiencies:

* **High Cognitive Load**: Users must manually review their schedules to detect overlaps or find optimal windows for new tasks.
* **Friction in Event Discovery**: Finding local events and manually transferring details (location, timing, descriptions) into a personal calendar creates a disjointed experience.
* **Inflexible Interfaces**: Standard interfaces do not support hands-free operations or unstructured text inputs, limiting accessibility and speed.

CalendarIQ resolves these issues by embedding an asynchronous AI orchestration layer directly into the scheduling pipeline. It converts natural language and voice commands into structured calendar data, performs automated conflict resolution, and provides context-aware time recommendations based on historic user patterns.

## Features

### AI Assistant & Language Processing

* **Natural Language Processing**
* **Voice Command Interface**
* **Automated Conflict Resolution**

#### AI Performance

* Achieved **84% intent classification accuracy** on the evaluation test set.
* Improved performance through iterative prompt engineering, validation workflows, and testing against real user prompts.

### Calendar & Event Management

* **Multi-View Interface**
* **Smart Categorization**
* **Local Event Aggregation**

### Analytics & Security

* **Pattern Metrics**
* **Secure Session Handling**

## Technology Stack & Architecture

### Backend Archetype (.NET 10 / C#)

The backend was rewritten from Node.js to C# to achieve higher type safety, maintainability, and architectural scalability. The system utilizes the following design choices and frameworks:

* **ASP.NET Core Web API**: Structured around a Controller-based architecture to map clearly to the legacy Express routing logic while maintaining clean separation of concerns.
* **Entity Framework Core (EF Core)**: Applied as the Object-Relational Mapper (ORM) to handle PostgreSQL database interactions via strongly typed LINQ queries, replacing the Sequelize configurations.
* **Asynchronous Programming**: Built entirely on asynchronous execution patterns (`async/await`) to process parallel requests and mitigate latency during external AI API calls.
* **Dependency Injection (DI)**: Leverages native IoC containers to manage object lifecycles across services (e.g., Singleton for in-memory conversation state, Scoped for HTTP requests and database context tracking).
* **Custom Middleware Validation**: Intercepts requests to enforce authentication checks using standard JWT Bearer validation adapted to support incoming `x-auth-token` headers from the legacy UI configuration.

### Frontend

* **Core Framework**: React 19 (Hooks, Context API, React Router)
* **Styling & Visualization**: Tailwind CSS 4, Chart.js
* **Utilities**: Day.js, Axios, React Speech Recognition

## Installation

### Prerequisites

* .NET 10 SDK
* Node.js (v16 or higher)
* PostgreSQL database instance

### Backend Setup

1. Clone the repository and navigate to the backend directory:
```bash
cd backend/CalendarIQ.Api

```


2. Restore NuGet dependencies:
```bash
dotnet restore

```


3. Apply Entity Framework database migrations:
```bash
dotnet ef database update

```


4. Run the ASP.NET Core application:
```bash
dotnet run

```



### Frontend Setup

1. Navigate to the frontend directory:
```bash
cd ../frontend

```


2. Install node dependencies:
```bash
npm install

```


3. Launch the development server:
```bash
npm run dev

```



## Usage

Open `http://localhost:5173` in your browser.

### API Endpoint Mapping

The .NET backend exposes identical API contracts to ensure full compatibility with the existing frontend:

* `POST /api/auth/register` | `POST /api/auth/login` — Account lifecycle operations.
* `GET | POST | PUT | DELETE /api/events` — Structured event management.
* `POST /api/chat` — Dispatches user prompts to the AI orchestrator to resolve intents like `create_event`, `event_overlap`, or `time_suggestions`.
* `GET /api/local-events/{city}` — Pulls external event data based on geographical parameters.

## Configuration

### Backend Setup (`appsettings.Development.json`)

Configure your environment properties in the main project root folder:

```json
{
  "Jwt": {
    "Secret": "your_secure_jwt_secret_key_here"
  },
  "AI": {
    "GroqApiKey": "your_groq_api_key",
    "GroqModel": "llama-3.1-8b-instant",
    "GeminiApiKey": "your_gemini_api_key"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=127.0.0.1;Port=5432;Database=calendariq;Username=postgres;Password=your_password"
  }
}

```

### Frontend Setup (`.env`)

```env
VITE_API_URL=http://localhost:5000/api

```

## License

© 2025 Universitatea „Alexandru Ioan Cuza" din Iași (UAIC)

This project is the intellectual property of the university and the student developer, produced for academic research objectives. All rights reserved.
