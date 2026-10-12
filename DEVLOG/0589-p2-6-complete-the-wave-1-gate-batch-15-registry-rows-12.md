## Entry 589 — 2026-07-03 20:30 PDT — P2.6 COMPLETE: the Wave-1 gate batch — 15 registry rows, 12 validator overrides, the STOP-literal fix, the 0873 migration — 1189 conformance green first run

**The remaining Wave-1 construct checklist lands as one batch (P2.6, narrowed by the flip commit's folded
binder migrations):**
- **85→2002 removal gates (0902 strict / warning permissive, all via `ConstructRegistry.Check`):** VALUE OF ·
  DATA RECORDS (FD **and** SD — ONE validator site via the shared grammar rule; the DataBinder SD-only 0873
  gate REMOVED — the Table-7 row-7.1 migration, pinned code kept) · MULTIPLE FILE [TAPE] · MEMORY SIZE /
  SEGMENT-LIMIT / WITH DEBUGGING MODE (token-scans of the `computerAttributes` wildcard sink — three registry
  rows, one override) · the five identification comment paragraphs (one row, paragraph named per site) ·
  REMARKS (≥2002 ONLY — the CCVS carve-out) · STOP literal · OPEN REVERSED (NO REWIND survives, §14.9.26).
- **2014→2023:** CLOSE WITH LOCK (VCR row 7). **Dual-obligation WINDOWS:** EXIT METHOD / EXIT FUNCTION
  (introduced 2002, REMOVED 2023 — `Check` refined: window rows use 0900 for the introduction edge and the
  entry's code for the removal edge; their positive cells are PENDING rows awaiting the OO/UDF waves — the
  gates are LIVE).
- **The 0903 archaic flags (§4.2.12 / Annex F.1):** EXIT PROGRAM + NEXT SENTENCE at ≥2023 — warning only,
  compiles at every edition; the new `ObsoleteMatrix` theory pins BOTH halves (warns at ≥obsoleteIn, silent
  below — no NIST-85 noise). constructs.json + the drift test gained `obsoleteIn`.
- **The STOP-literal mis-bind FIXED (the DEVLOG-578 latent bug, both halves per the roadmap):** ≥2002 gate
  0902 + the 85 SEMANTICS implemented — `BoundStopLiteral` per X3.23-1985 Format 2: the literal goes to the
  OPERATOR channel (stderr, never the program's output stream) and execution CONTINUES with the next
  statement (implementor latitude on the operator pause). No golden-bearing program executes one (the 349
  held), and the guard's run-only programs verify the legacy differential.
- ST101A now asserts BOTH removal codes in the one validator pass (0902 + the migrated 0873).
- **Suites: conformance 1189/1189 (91 new cells) + unit 38/38, green FIRST RUN;** the full permissive
  continuity sweep (flip-state CLI): **419 OK / 0 BREAKS / 40 SKIP85** — INV-1 green at all four editions
  (a Phase-1 exit criterion); guard + the 2023-permissive golden re-run gate this commit.

