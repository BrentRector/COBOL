## Entry 1900 — 2026-10-05 13:20 PDT — The attended-session launcher lives in the repo: `scripts/start-session.ps1`, and the context-full restart is a written procedure

The attended session's context filled mid-afternoon, and the owner reminded it that it was meant to save, shut down and restart through a script. There was no such automation: a model cannot restart itself, and the
only launcher was `%LOCALAPPDATA%\CobolNet\cobolnet-autoresume.ps1`, an unversioned boot-time script whose prompt told the new session to re-create a session-only cron job and read the Quota Meter artifact
and a campaign-state note, all replaced by the orchestrator loop and the resume notes. The owner asked for the script to move into the repo. `scripts/start-session.ps1` is it: `claude -n COBOL --dangerously-skip-permissions` in the repository (the owner's 2026-09-20
decision, kept with its reasons in the header), a prompt that names no state and points at the memory index's START HERE note, and `-DryRun`. The context-full procedure is now written down where it will be read (DESIGN-orchestrator-loop.md
section 10b, the script's header): wind running work down with `stop.ps1`, save a resume note and WIP checkpoint commits, then ask the owner to run the launcher in a new terminal. The wind-down mattered: the loop was
96 minutes into a seven-group wave whose implementer gates were mid-run, and it is a child of the attended session, so a plain shutdown would have killed it; STOP made the agents checkpoint and hand off instead. The Startup entry and the old
`%LOCALAPPDATA%` script are repointed and deleted after this lands (no forwarder, per the no-shims rule), and `install-autostart.ps1` (the owner's pending D9) is unaffected: it still replaces the logon entry with the headless loop.

**Files:** `scripts/start-session.ps1`, `docs/rearchitecture/DESIGN-orchestrator-loop.md`. No diagnostic code used.
