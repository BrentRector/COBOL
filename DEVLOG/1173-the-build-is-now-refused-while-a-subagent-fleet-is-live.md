## Entry 1173 — 2026-08-04 14:45 PDT — the build is now REFUSED while a subagent fleet is live, because knowing better did not stop me

**Owner correction, and it was about waste rather than correctness: I rebuilt the compiler repeatedly while a
measurement fan-out was running over the very thing being rebuilt, and each repetition burned tokens for output
that was already worthless.** The honest count for the PB15 session: ~6 `dotnet build` invocations plus both full
test gates, across ~1.75 h and 60 agents, none of whose verdicts could be cited.

**The tell was there and I explained it away.** One of those builds failed outright —
`MSB3027: cobol.exe locked by another process`, the process being a live agent — and I recorded it as
"transient" and kept building.

⭐ **AND THE FIRST-ORDER ERROR WAS NOT THE CONCURRENCY.** I had already derived the twenty-function population
from the spec and measured all eight defects with a shell loop over `cobol.exe` — three tool calls — and THEN
dispatched ten agents to measure the same thing. The fan-out was redundant before it was contaminated. Editing
its subject made the output unusable; it had no value to lose.

**SO THE GUARD IS MECHANICAL, NOT A NOTE TO SELF.** `scripts/hooks/fleet_active_build.py` is a PreToolUse hook on
Bash and PowerShell, filtered to `dotnet *`, that DENIES `build` / `test` / `clean` / `publish` / `run` /
`msbuild` while any subagent transcript for THIS session was written in the last 120 s. Its refusal names the
live run and states the three legitimate exits: wait for the completion signal, `TaskStop` the fleet, or — if you
are about to fix what it measures — stop it now, because you already have the answer.

**Detection is by TRANSCRIPT MTIME**, the one signal a stale process cannot fake: a live agent writes to its own
`agent-*.jsonl` continuously. It globs on the SESSION ID rather than deriving the sanitized project directory
name, so it stays correct whatever that sanitization rule is and another project's fleet can never block this
one. **Fail-open by construction** — any error, missing path or unreadable directory allows the build, because a
guard that blocks the build because IT broke is worse than the defect it prevents.

**PROVEN IN BOTH DIRECTIONS, END TO END, not merely pipe-tested.** With a transcript touched into the window a
real `dotnet build` was REFUSED with the message above; with the mtime aged back out, the same build ran in 9.9 s.
The pipe tests also pin the three allow paths that matter: a non-build command, `dotnet --version` (must not read
as a build), and a build with only stale transcripts present.

⚠ **A NOTE ON THE STANDING ORCHESTRATION INSTRUCTION.** "Prefer a workflow for substantive tasks" is not "one
fan-out per task". Measure inline first; fan out only for what the inline pass could not answer. The memory that
recorded this now leads with that ordering rather than with the freeze rule, because the freeze rule alone would
have produced a slower version of the same waste.

