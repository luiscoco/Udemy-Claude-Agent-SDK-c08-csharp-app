# C08 · Routing Events to the Right Handler (C# / Blazor)

A .NET 10 Blazor Server port of `lesson-c08-app`, using the official [Anthropic](https://www.nuget.org/packages/Anthropic) C# SDK (13.3.0) instead of `@anthropic-ai/sdk`.

Runs a real Managed Agents session through a local event router. Action events are displayed, not executed or approved. Deduplication is scoped to one run.

## Concepts students will learn

- Separate previews from persisted records
- Deduplicate after successful dispatch
- Route text, actions, usage and lifecycle events

## Configure and run

Use the .NET 10 SDK. In PowerShell, from this folder:

```powershell
dotnet --version
dotnet restore
Copy-Item .env.example .env
notepad .env
```

Use your own Anthropic API key and an existing agent ID and environment ID. The app looks for `.env` in the working directory and its parents, so this folder's `.env` is found from `src/LessonC08.App` too.

If you do not already have resource IDs, run the optional setup. It creates the environment before a text-only agent, saves each ID to `.env` immediately, and reuses IDs already configured. It uses `CLAUDE_MODEL` or the default `claude-opus-5-5`.

```powershell
dotnet run --project src/LessonC08.App -- setup
```

Start the web app and open `http://127.0.0.1:3008` (or your `PORT`):

```powershell
dotnet run --project src/LessonC08.App
```

## Project layout

| Path | Ported from | Purpose |
|---|---|---|
| `src/LessonC08.App/Lab/EventRouter.cs` | `src/router.ts` | Preview / text / action / usage / lifecycle routing with per-run dedup |
| `src/LessonC08.App/Lab/SessionRunner.cs` | `/api/run` in `src/server.ts` | Creates the session (50-cent budget), opens the stream, sends the prompt, routes events |
| `src/LessonC08.App/Lab/SetupResources.cs` | `scripts/setup-resources.mts` | `dotnet run -- setup` provisioning |
| `src/LessonC08.App/Lab/DotEnv.cs`, `LabOptions.cs` | `process.loadEnvFile` + env reads | Server-side configuration |
| `src/LessonC08.App/Components/Pages/Home.razor` | `public/index.html` + `public/app.js` | Interactive lab page |
| `src/LessonC08.App/wwwroot/` | `public/` | Guide, architecture page, styles, images |
| `tests/LessonC08.Tests/` | `src/router.test.ts` | xUnit tests for the router and SDK event shape |

### What changed in the migration

- **No `/api/run` NDJSON endpoint.** Blazor Server runs the lab's C# on the server and pushes updates over its SignalR circuit, so the API key and resource IDs still never reach the browser.
- **SDK calls:** `client.beta.sessions.create` → `client.Beta.Sessions.Create(SessionCreateParams)`, `events.stream` → `client.Beta.Sessions.Events.StreamStreaming(...)`, `events.send` → `client.Beta.Sessions.Events.Send(...)`.
- **Routing on wire JSON:** each typed SDK event is serialized back to its wire JSON (`JsonSerializer.SerializeToElement`) so the router keeps the original contract (`type`, `id`, `evaluated_permission`).
- **Stream first:** the stream's first read is started before the kickoff `user.message` is sent.

## Validation

```powershell
dotnet test
```

`wwwroot/architecture.html` still walks through the original TypeScript excerpts; the C# equivalents are listed in the table above.
