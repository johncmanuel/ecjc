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

## Testing

To run the server tests, run the following command from root:

```bash
dotnet test apps/server.tests
```

## Public HTTPS with Tailscale Funnel

To expose the application to the internet with HTTPS support, use [Tailscale Funnel](https://tailscale.com/kb/1223/funnel).

1. Ensure Tailscale is installed and Funnel is enabled on your tailnet.
2. Update `.env` with your Tailscale URL (e.g., `https://your-machine.tailnet-name.ts.net`):
   ```env
   BETTER_AUTH_URL=https://your-machine.tailnet-name.ts.net
   ```
3. Start the application (e.g. using the test compose file):
   ```bash
   docker compose -f docker-compose.test.yml up -d
   ```
4. Start Tailscale Funnel on port 3000 (Next.js client port):
   ```bash
   tailscale funnel 3000
   ```
