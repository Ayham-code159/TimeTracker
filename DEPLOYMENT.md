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

The frontend and API should be served from the same HTTPS site. If they use
different origins on the same site, also configure:

```text
Cors__AllowedOrigins__0=https://app.example.com
VITE_API_BASE_URL=https://api.example.com
```

Build `VITE_API_BASE_URL` into the frontend. Authentication uses an HttpOnly,
Secure, SameSite=Strict cookie, so unrelated cross-site domains are intentionally
unsupported.

Local-only credentials belong in `TimeTracker/appsettings.Local.json`. That file
is ignored by Git and excluded from build and publish output.
