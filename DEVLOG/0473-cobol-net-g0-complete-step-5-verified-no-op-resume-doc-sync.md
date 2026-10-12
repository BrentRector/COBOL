## Entry 473 — COBOL.NET G0 COMPLETE (step 5 = verified no-op) + resume-doc sync → resume at G2

G0 step 5 ("scripts / CI / sln consistency") turned out to be a **no-op** for the renames done in steps 1–4: a
repo-wide grep confirms `scripts/*.sh` and `.github/` reference ONLY the legacy `CobolSharp.*` projects (the CLI
`cobolsharp.dll`, `CobolSharp.Runtime.dll`, both `CobolSharp.Tests.*`) — all unchanged, since steps 1–4 renamed the
greenfield `CobolNet`/`CobolNet.Runtime` → `Cobol.Net.*`, not the legacy oracle. So nothing in the scripts/CI needs
updating, and they stay green. The one optional step-5 item — `CobolSharp.sln → Cobol.Net.sln` (owner-taste, "for
brand consistency") — is **DEFERRED to G8**: the solution genuinely contains BOTH brands until the legacy
`CobolSharp.*` is deleted at cut-over, so renaming it now (re-touching the just-fixed 5 scripts + CI for a purely
cosmetic change, mid-transition) is poor risk/reward; at G8 the rename is unambiguous and lands with the namespace
big-bang. **G0 (project reorganization) is therefore complete.**

Synced the resume docs (`resume-prompt.md`, `CLAUDE.md`) to the new state: G0 done (steps 1–4 + step-5 no-op), CI
made cross-platform green (468/469/470), **RESUME AT → G2** — build the bound semantic tree +
`ReferenceResolver`→`Place` + data model + native numerics, building the emitter INTO the §17 §2.2 decomposed
structure (per the advisor: do NOT pre-decompose the parse-tree-walk emitter the DESIGN supersedes; build it
decomposed as the bound-tree rebuild lands) → G3 verbs → G4 PC-dispatcher → G5 NIST via the differential harness.

DEVLOG 473.

