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

Vercel proxies same-origin API requests to Railway using the first rewrite in
`Frontend/vercel.json`. The frontend deliberately uses relative API URLs and
does not read `VITE_API_BASE_URL`, so requests always use:

```text
/api
```

Railway still allows requests whose browser origin is:

```text
https://time-tracker-lake-delta.vercel.app
```

Authentication uses the JWT returned by the login endpoint. The frontend stores
the short-lived access token only in JavaScript memory and sends it in the
`Authorization: Bearer` header. A rotating refresh token is stored in a
`Secure`, `HttpOnly`, `SameSite=Strict` cookie. Login, refresh, and logout requests
must come from an exact origin listed under `Cors:AllowedOrigins`.

The defaults are a 15-minute access token, a 7-day refresh inactivity timeout,
and a 14-day absolute session lifetime. They can be overridden in production:

```text
Jwt__ExpirationMinutes=15
RefreshToken__IdleExpirationDays=7
RefreshToken__AbsoluteExpirationDays=14
RefreshToken__RotationGraceSeconds=30
```

The Vercel rewrite is important: it keeps the refresh cookie first-party and
avoids browser third-party-cookie blocking. If the Railway URL changes, update
the rewrite destination before deploying the frontend. A custom domain remains
a good future option, but it is not required by this setup.

Refresh token values are never stored in the database; only SHA-256 hashes are
persisted. Each refresh rotates the token. Concurrent refreshes within the short
rotation grace window receive the same replacement token, while later reuse of
an already-rotated token revokes its entire session family. Logout revokes the
current family.

Local-only credentials belong in `TimeTracker/appsettings.Local.json`. That file
is ignored by Git and excluded from build and publish output.
