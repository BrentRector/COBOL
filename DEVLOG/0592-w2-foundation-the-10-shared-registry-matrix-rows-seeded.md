## Entry 592 — 2026-07-03 19:20 PDT — W2 foundation: the 10 shared registry+matrix rows seeded PENDING (the cross-track contention removed before the fan-out)

**Roadmap Phase 2 W2 opens.** The wave's four tracks (A: MOVE rows · B: loud-guard misbind sweep + national/
boolean skeleton · C: negative-corpus seeds · D: position-aware reserved-word checking) all funnel through TWO
shared files — `ConstructRegistry.Entries` and `tests/version-matrix/constructs.json` (drift-locked to each
other) — so the shared rows are laid down FIRST, serially, and the agents then work disjoint files only
(feedback_workflow_agent_dispatch).

**Seeded (all `status:"pending"` — metadata frozen by the drift tests, no behavior asserted until each track
flips its rows active):** `move-alphanumeric-figurative-removed-2023` (85→removed 2023, 0902; Annex E.2 item 1
bullet 1 / §14.9.25.3 SR5 / VCR row 1) · `move-all-digit-integer-obsolete-2023` (85, obsolete 2023, 0903; SR5
NOTE / F.2 item 2 / VCR 92+128) · `national-data-2002` + `boolean-data-2002` (§8.5.2/§13.18.40/§13.18.60) ·
`pic-external-float-2002` (§13.18.40 symbol E) · `usage-pointer-2002` · `usage-object-reference-2002` ·
`usage-binary-char-family-2002` (§13.18.60) · `usage-float-long-2002` + `usage-float-extended-2002` (D16).

**Empirical latent-bug pins (probes, this session):** `MOVE ALL "5" TO PIC 9(3)` COMPILES at 85 then dies
runtime-loud (`BoundAllLiteral`) — SR5 says it is VALID at every edition (555 expected); `MOVE SPACE TO PIC
9(3)` / `MOVE QUOTE TO ZZ9` also compile then die runtime-loud ("figurative 'S' in a numeric context") —
permitted ≤2014, removed 2023. Both are track A's to fix. Track B's misbind confirmations: `PicInfo.ParseUsage`
falls to `Usage.Display` for NATIONAL/BIT/POINTER/FLOAT-*/BINARY-CHAR-family/OBJECT-REFERENCE; the bare-form
`UsageKeyword` string-strip turns `BINARY-CHAR SIGNED` into "BINARY-CHARSIGNED" → silent DISPLAY (and only
accidentally survives DISPLAY: "DISPLAY".Replace("IS","") = "DPLAY" → the default arm).

Gate: compiler build 0W/0E; unit 38/38 (both drift disciplines) + matrix/corpus classes 209/209 green.

