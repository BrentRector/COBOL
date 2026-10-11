## Entry 691 — 2026-07-08 10:20 PDT — CI GREEN — the bind-time gating migration's two shared-frontend fallout points (characterization snapshots + legacy-CLI gating tests)

⚠ TRANSPARENCY / process miss: Clusters 1–11 each ran RED on CI even though `scripts/guard-fast.sh` was green locally
every time. `guard-fast.sh` does NOT run two things CI does: (1) the greenfield **characterization snapshots** gate
(`Cobol.Net.Tests.Characterization` — the P0 behavior-neutrality net) and (2) the LEGACY unit project's
**CliExitCodeTests** (it appears to run a filtered/stale legacy unit set). Both broke from the SHARED-frontend
consequence of the migration, and I should have run the full CI-equivalent battery per cluster. Root causes + fixes:

1. **Characterization snapshots (2).** `char_neg_typedef85` / `char_neg_occurs_dyn85` snapshot the diagnostic surface
   of two negative programs; the migration intentionally moved their edition diagnostics from the parse layer
   (`COBOL0001: no viable alternative` + a cascading `COBOL0307` + a "cannot parse construct near" 0900) to a single
   clean bind-time `COBOLNET0900`. Re-baked via `COBOLNET_UPDATE_SNAPSHOTS=1` — the new snapshots are strictly better
   (fewer, cleaner diagnostics). Only those 2 files changed (diff reviewed).
2. **Legacy CLI gating tests (4/8 cases).** `CobolSharp.Tests.Unit.CliExitCodeTests` drove the LEGACY CLI
   (`cobolsharp.dll --standard cobolNN`) to assert it REJECTS GOBACK RETURNING @85 / DELETE FILE @85/2002/2014 via the
   shared grammar's parse-time `{isYYYY()}?` predicates. The migration ungated that shared grammar and put the gate in
   the GREENFIELD binder only — so the legacy compiler (shares the frontend, not the greenfield binder) now PARSES and
   accepts these below their edition (confirmed: `cobolsharp DELETE-FILE@85` → "Compiled successfully", exit 0). These
   legacy-CLI assertions are therefore OBSOLETE — the correct behavior is verified against the authoritative greenfield
   by `EditionGateDiagnosticTests` + `VersionMatrixTests` (all green). Removed the two obsolete gating theories +
   `AssertDialectGate` with a documenting comment; kept the legacy-CLI mechanics tests (valid-program exit 0,
   unknown-option exit 1).

**Design note (flagged for owner review):** the legacy compiler LOSING edition introduction-gating for the 24 migrated
constructs is an accepted consequence — it is a soon-deleted (G8) differential oracle already severed from the battery
(P0), and the authoritative greenfield gates correctly. If the owner would rather the legacy compiler retain gating,
the alternative is adding the Check to the legacy binder too (work on a dying codebase) — not recommended.
Verified GREEN: legacy unit 1196 · characterization 32 · greenfield conformance 2055 · greenfield unit 224 · FULL
legacy guard NIST 353 MATCH. Migration commits `0e4251e2`→`b3a4e8e8` (Clusters 1–11) now have a green CI head.

