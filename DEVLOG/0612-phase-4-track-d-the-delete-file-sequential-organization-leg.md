## Entry 612 - 2026-07-04 22:52 PDT - Phase 4 track (d): the DELETE FILE sequential-organization leg (M4-1 closed)

A bounded track-(d) increment (per the DEVLOG-610 reconciliation): the keyed DELETE FILE leg (§14.9.10
Format 2, COBOL-2023) was LANDED but the SEQUENTIAL-organization leg stage-loud'd - "the sequential
connector does not yet expose its host path." Cure: `SequentialFile.HostPath` accessor + the sequential
branch in `CobolFile.DeleteFile` (the keyed half owns the runtime dispatch; it now checks the sequential
Files registry first), so all three organizations share ONE delete path with the same GR13/GR14/GR16
outcomes. The emitter's sequential loud-stage is retired.

Behavior (both golden-proven, greenfield-only 2023): GR13 round-trip - OPEN OUTPUT / WRITE / CLOSE /
DELETE FILE / OPEN INPUT reports '35' file-not-available (delete_file.cob, ST=35); GR14 - DELETE FILE of
an ABSENT file is a SUCCESSFUL completion with status '05' and takes NOT ON EXCEPTION (delete_file_absent.cob,
NOEXC / ST=05 - the spec's '05' over the legacy's '35', already the documented divergence). Open-connector
→ '41' (GR13) and permission-denied → '37' (GR16) ride the shared path.

Residual (documented): the multiple-file-name form `DELETE FILE f1 f2…` (§14.9.10 GR) needs the
`fileName+` grammar + the "ON EXCEPTION if ANY file fails" semantics - a separate grammar-touching
increment. Both 2023 goldens joined the GreenfieldOnly set (the frozen legacy cannot parse DELETE FILE).

**Battery:** greenfield conformance **1600/1600** (+2 goldens) · guard-fast **ALL GREEN** (no grammar
change ⇒ guard-fast). Reconciliation: M4-1 → LANDED (multi-file residual noted).


