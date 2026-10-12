## Entry 004 — 2026-03-13: Defense-in-Depth Against Context Loss (Claude's Summary)

After setting up the transparency and session management rules, here's the system we have in
place — four layers of external state that together should make any session (fresh or continued)
productive quickly:

| Layer | What it preserves |
|-------|-------------------|
| `PROJECT_PLAN.md` | Current state — what's done, what's next |
| `DEVLOG.md` | Reasoning — why things are the way they are |
| Persistent memory | Process rules — how to behave across sessions |
| Git commit messages | Code evolution — forensic trace of every change |

The 1M token context window on Opus should give us much longer productive sessions before
drift becomes an issue. But when it does happen, the commitment is to flag it rather than
quietly degrading. And if we need a fresh session, the ramp-up should be fast: read the plan,
read the latest devlog entries, check git log, and go.

This is a testable hypothesis. If it works, it's a replicable pattern for long-running
AI-assisted projects. If it doesn't, understanding *why* it failed is equally valuable.

---

