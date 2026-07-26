# Deployment configuration

The API applies pending Entity Framework migrations and creates the `User` and
`Admin` roles during startup.

Set these environment variables before the first production start:

```text
ConnectionStrings__DefaultConnection=<production PostgreSQL connection string>
Jwt__Key=<random secret with at least 32 characters>
BootstrapAdmin__Username=<initial administrator username>
BootstrapAdmin__Password=<strong initial administrator password>
```

After the first successful start, remove the two `BootstrapAdmin` variables.
Future starts will accept an existing Admin account.

The frontend sends requests directly to Railway using:

```text
VITE_API_BASE_URL=https://timetracker-production-0fdf.up.railway.app
```

Railway allows credentialed CORS requests from:

```text
https://time-tracker-lake-delta.vercel.app
```

Authentication uses the JWT returned by the login endpoint. The frontend stores
it in tab-scoped `sessionStorage` and sends it in the `Authorization: Bearer`
header. Closing the browser tab clears the session token.

Local-only credentials belong in `TimeTracker/appsettings.Local.json`. That file
is ignored by Git and excluded from build and publish output.
