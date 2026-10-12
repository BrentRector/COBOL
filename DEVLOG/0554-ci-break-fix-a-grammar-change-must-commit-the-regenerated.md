## Entry 554 — 2026-06-10 18:45 PDT — CI break + fix: a grammar change must COMMIT the regenerated parser

The DEVLOG-552 grammar edit (RETURN reversed AT-END order) regenerated `src/Cobol.Net.Frontend/Generated/
CobolParserCore.cs/.interp` locally via the EnsureGeneratedFiles target — but the regenerated artifacts were
never committed. CI's ANTLR regeneration step FAILED on Linux (`error(114): cannot find tokens file` — the
target's backslashed paths misfire there) and silently fell back to the STALE checked-in parser, so the
spec-pinned reversed-order RETURN fact died with `no viable alternative at input 'NOT'`. Fix: commit the
regenerated artifacts (cdd3b8f) — CI green; the WSL Linux run also verified 648/648 locally (the
`reference_wsl_linux_repro` pattern: build on Windows, `dotnet test --no-build` under WSL). **Rule recorded
(memory `feedback_commit_generated_parser`): any .g4 change ships WITH its regenerated Generated/* in the same
commit — CI cannot regenerate and falls back silently.** Follow-up queued: make GenerateIfNewer/the target
path-portable so the CI regen failure stops being silent.

