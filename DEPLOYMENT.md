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

The Vercel frontend proxies relative `/api/*` requests to Railway using
`Frontend/vercel.json`. Browser requests and authentication cookies therefore
remain on the Vercel origin. Do not configure `VITE_API_BASE_URL`; a direct
Railway URL would bypass the proxy and restore the third-party-cookie problem.

Authentication uses an HttpOnly, Secure, SameSite=Lax cookie. Vercel also sends
`Cache-Control: no-store, no-cache, must-revalidate` for proxied API responses.

Local-only credentials belong in `TimeTracker/appsettings.Local.json`. That file
is ignored by Git and excluded from build and publish output.
