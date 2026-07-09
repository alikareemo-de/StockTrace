# StockTrace Testing UI

Internal testing UI for the StockTrace Inventory Module.

This is not a production frontend. It exists to exercise the REST API and SignalR hub without Postman.

Default login:

```text
Username: admin
Password: Admin@12345
```

All testing users are listed in `../../docs/TEST_USERS.txt`.

## Run

Start the backend first:

```powershell
dotnet run --project ../../src/StockTrace.Api
```

Then start the testing UI:

```powershell
npm install
npm run dev
```

The default development settings are in `.env.development`. To customize them, copy `.env.example` and adjust:

```text
VITE_API_BASE_URL=http://localhost:5133
VITE_DEV_SERVER_PORT=5173
```

Open:

```text
http://127.0.0.1:5173
```

The UI sends API and SignalR requests to:

```text
http://localhost:5133
```

The UI stores the JWT in browser local storage, sends it with API requests, and returns to Login when a request receives `401 Unauthorized`.
