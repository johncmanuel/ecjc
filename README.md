# ecjc 

## Quickstart

### 1. Prerequisites

Ensure you have the following installed on your machine:
- **Node.js** 
- **.NET SDK** v10+

### 2. Initial Setup

Install npm dependencies and restore .NET local tools (`nswag`, `dotnet-ef`):

```bash
# Install Node dependencies
npm install

# Restore .NET local tools (NSwag & Entity Framework Core CLI)
dotnet tool restore
```

## Development Commands

Run applications individually or concurrently using native npm workspace commands:

| Command | Action | URL |
| :--- | :--- | :--- |
| `npm run dev` | Starts **both** Next.js client & ASP.NET Core server | `http://localhost:3000` |
| `npm run dev:client` | Starts Next.js development client only | `http://localhost:3000` |
| `npm run dev:server` | Starts ASP.NET Core server only | `http://localhost:5186` |
