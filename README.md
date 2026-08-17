# ecjc 

## Quickstart

### 1. Prerequisites

Ensure you have the following installed on your machine:
- **Node.js** 
- **.NET SDK** v10+
- **Docker** (for local PostgreSQL)

### 2. Initial Setup

Install npm dependencies and restore .NET local tools (`nswag`, `dotnet-ef`):

```bash
# Install Node dependencies
npm install

# Restore .NET local tools (NSwag & Entity Framework Core CLI)
dotnet tool restore
```

### 3. Environment Variables

Copy the provided example environment file to set up your local secrets:

```bash
cp .env.example .env
```

Generate a secret for Better-Auth (used by both client and server):

```bash
npm run generate-secret
```

Copy the output and paste it into both `BETTER_AUTH_SECRET` and `Auth__Secret` in your `.env` file. You will also need to add your Google OAuth credentials from the [Google Cloud Console](https://console.cloud.google.com/apis/credentials).

### 4. Start the Database

Spin up PostgreSQL and pgAdmin locally using Docker:

```bash
docker compose -f docker-compose.dev.yml up -d
```

pgAdmin will be available at `http://localhost:5050` (no login required).

## Development Commands

Run applications individually or concurrently using native npm workspace commands:

| Command | Action | URL |
| :--- | :--- | :--- |
| `npm run dev` | Starts **both** Next.js client & ASP.NET Core server | `http://localhost:3000` |
| `npm run dev:client` | Starts Next.js development client only | `http://localhost:3000` |
| `npm run dev:server` | Starts ASP.NET Core server only | `http://localhost:5186` |
| `npm run generate-secret` | Generates a 32-byte secret via `openssl` | — |

