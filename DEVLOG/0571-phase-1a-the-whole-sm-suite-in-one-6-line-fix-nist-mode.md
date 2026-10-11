## Entry 571 — 2026-06-10 23:13 PDT — Phase 1A: the WHOLE SM suite in one 6-line fix — NIST-mode default copy library (§7.2.3.4 GR3) → 15 programs locked (842 conformance; 288 NIST)

**The Phase-1 scout wave's first return (six parallel scouts over the COBOL-85 closure subsystems; briefs in
`/e/tmp/phase1-briefs/`).** The COPY/SM scout's verdict held up exactly: all 15 SM programs already byte-match
with the EXISTING shared `CopyProcessor` — the lone defect was that the greenfield `CompilerDriver` never wired
the NIST sibling `copylib/` directory into the COPY search path (the legacy CLI auto-discovers it in NIST mode;
the greenfield forgot to mirror that). Unresolved copybooks took the lenient fallback — the COPY statement AND
its separator period replaced by a comment — beheading the surrounding entry (SM101A's `01 TST-TEST COPY
K101A.`) or leaving dangling ELSEs (SM206A's mid-IF fragments), which produced the census's whole CMPL_FAIL/
DIFF spread from ONE cause.

**The fix (per ISO §7.2.3.4 GR3 — the implementor defines the default COBOL library):** in NIST mode,
`CompilerDriver.Compile` appends the `copylib/` sibling of the source's directory to the COPY search path
(after caller-supplied paths, so an explicit `--copy` outranks the convention; `Directory.Exists`-guarded).
ONE seam fixes the CLI, the differential harness, and the sweeps at once. No frontend/shared-file change — the
shared `CopyProcessor` already implements §7.2.3/§7.2.4 in full (COPY in every division, REPLACING word/
identifier/literal/pseudo-text + LEADING/TRAILING, OF/IN library subdirectories, mid-sentence fragments,
REPLACE) — so NO legacy-guard exposure. Blast radius checked by the scout: real COPY statements exist ONLY in
the SM suite (+ the no-golden SM301M/SM401M); the full suites confirm the locked corpus is untouched.

**Locked: SM101A–SM208A (all 15; chains for 102A/104A/202A/204A were already in chains.tsv). 842 conformance +
15 unit green; 288 NIST programs locked. The SM suite is COMPLETE.**

