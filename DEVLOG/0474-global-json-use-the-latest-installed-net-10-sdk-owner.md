## Entry 474 — global.json: use the latest installed .NET 10 SDK (owner directive), unblocks WSL/CI

Owner: "use the latest .net. .300 is fine everywhere." `global.json` had pinned `10.0.204` with
`rollForward: latestPatch` — which rejects the `10.0.300` (3xx feature band) SDK that the GitHub runners and a
local WSL install carry (latestPatch stays within the 2xx band), the friction that made the WSL repro need the
2xx band. But the owner's own Windows box currently has only `10.0.202`/`10.0.204` (no `.300`), so pinning `.300`
exactly would break the local build. Resolution that satisfies "use the latest everywhere" without breaking any
host: `version: 10.0.100`, `rollForward: latestMinor` — each machine resolves to its **latest installed** .NET-10
SDK (Windows → 10.0.204 today, WSL/CI → 10.0.300), and a `.300` install on Windows is picked up automatically.
Stays within the net10.0 TFM. Verified: Windows `dotnet --version` → 10.0.204, solution build clean; WSL
`dotnet --version` → 10.0.300 (so the full `guard.sh` incl. the SDK build now runs in WSL for Linux verification).

DEVLOG 474.

