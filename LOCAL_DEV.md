# Local dev / testing environment

The app connects to a **local PostgreSQL** you already have installed. Just run
`BPM.Client` (F5 / `dotnet run`) — no manual database setup is needed.

## How it works

- The connection string lives in `BPM.Client/appsettings.Development.json` under
  `ConnectionStrings:Bpm` (defaults to `Host=localhost;Port=5432;Database=BpmClient;Username=postgres;Password=postgres`).
- On launch **in the Development environment**, Marten's `CreateDatabasesForTenants`
  creates the `BpmClient` database automatically if it doesn't exist yet (it connects
  to the `postgres` maintenance database to do so).
- Marten is configured with `AutoCreate.CreateOrUpdate`, so all event-store and
  document schema objects are created/updated on first use.

Net result: fresh clone → set your Postgres password (if not `postgres`) → F5 → connected.

## First-time checklist

1. Make sure your local PostgreSQL is running and listening on `localhost:5432`.
2. If your `postgres` user password is **not** `postgres`, edit the `Password=` value in
   `BPM.Client/appsettings.Development.json`. Update the `WithOwner("postgres")` line in
   `BPM.Client/Program.cs` too if you connect as a different role.
3. Run `BPM.Client`. The `BpmClient` database and schema are created on startup.

Verify it came up:

```bash
psql -U postgres -d BpmClient -c "\dn"     # lists the 'bpm' schema Marten created
```

## Notes

- Auto database creation only runs in **Development** (guarded by
  `builder.Environment.IsDevelopment()`), so other environments are untouched.
- The connecting user needs `CREATEDB` privilege for the auto-create step. The default
  `postgres` superuser has it.
- The repo also ships a `docker-compose.yml` if you'd rather run Postgres (or the whole
  stack) in Docker instead of a local install — `docker compose up -d postgres`.

---

# Testing the Loan.Api sample (MCP) live

`samples/Loan.Api` turns the loan process into an MCP server behind JWT auth. Test it
interactively with the MCP Inspector.

### 1. Start Postgres (Docker)

```bash
docker compose up -d postgres
```

The sample uses a database named `bpm`. On launch in Development, it auto-creates that
database if it's missing (same mechanism as BPM.Client), so you don't need to create it
by hand.

### 2. Run the sample

```bash
dotnet run --project samples/Loan.Api
```

It listens on `http://localhost:5000`. A `Properties/launchSettings.json` forces the
**Development** environment, which enables the `/dev-token` endpoint used below (it does
not exist outside Development).

### 3. Get a dev token

```bash
# bash
TOKEN=$(curl -s http://localhost:5000/dev-token)
echo "$TOKEN"
```

```powershell
# PowerShell
$TOKEN = (Invoke-RestMethod http://localhost:5000/dev-token)
$TOKEN
```

This mints a short-lived JWT for a fake customer (Nino Beridze). It's a dev-only
convenience — in production the `/mcp` endpoint sits behind your real identity provider.

### 4. Launch MCP Inspector and connect

```bash
npx @modelcontextprotocol/inspector
```

In the Inspector UI:

1. Transport type: **Streamable HTTP**
2. URL: `http://localhost:5000/mcp`
3. Add a header — name `Authorization`, value `Bearer <paste your token>`
4. Connect, then **List Tools**.

Things to try:

- `bpm_get_command_schema` with `{"command": "InitiateLoanApplication"}` — inspect the input schema.
- Start a loan application, then advance it. `SignLoanContract` comes back as
  `approval_required` to demonstrate the policy gate.

### Verify the data landed

```bash
psql -h localhost -U postgres -d bpm -c "\dt bpm.*"   # Marten's event/document tables
```

> Note: `docker compose up -d postgres` starts **only** Postgres. Don't run bare
> `docker compose up` — that also builds and runs BPM.Client in a container, which you
> don't want while testing the sample from `dotnet run`.
