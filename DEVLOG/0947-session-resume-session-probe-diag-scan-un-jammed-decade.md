## Entry 947 — 2026-07-21 13:30 PDT — Session resume: session-probe diag scan un-jammed (decade-pinned regex → whole COBOLNET1xxx band); green-baseline confirmed

**Session bootstrap (plan §0 step ③).** `git checkout phase-13-grammar-batch` → `pwsh scripts/session-probe.ps1`. The
probe reported `diag: src-grep max COBOLNET1599 · catalog max COBOLNET1584 ⚠ SCANS DISAGREE — reconcile before
allocating`. Investigated before touching anything: this was a **false alarm from a stale regex in the probe itself**,
not a real collision. The probe's two diag scans were hard-pinned to the 15xx decade (`COBOLNET15[0-9][0-9]` /
`"(COBOLNET15\d\d)"`), so once diagnostics crossed 1600 — PERFORM Format 3 shipped 1597–1617, Wave D shipped 1618, ledger
C2 shipped 1619 — the src scan silently capped at 1599 and the catalog scan at 1584, and the `maxGrep ≠ maxCat` equality
test cried DISAGREE on two truncated readings. The plan's own prescribed grep (`grep -rho 'COBOLNET1[5-6][0-9][0-9]' src`)
already reads the true max = **1619**, and both 1618/1619 ARE `DiagnosticCatalog` descriptors, so catalog max = 1619 too.
Real next-free = **1620**, exactly as plan §0 states. The instrument, not the ledger, had drifted — and worse than a
stale ledger, because the probe exists *specifically* to be the drift-proof mechanical check the manual ledgers can't be
(the COBOLNET1573/1518 collision lesson). A jammed instrument silently re-broke the plan's "allocate only after BOTH scans
agree" gate for every future wave.

**Fix (`scripts/session-probe.ps1`, diag section).** Widened both scans to the whole `COBOLNET1[0-9][0-9][0-9]` band so
the reading tracks the true ceiling regardless of decade. Reworked the cross-check for the documented **two-channel
reality**: the compiler-channel raw `Edition.Error` codes (1585–1617) are intentionally NOT catalog descriptors (ledger
V11 queues folding them in), so `src-max ≥ catalog-max` is the EXPECTED steady state, not drift — a bare equality test is
structurally wrong now and would keep false-alarming on every compiler-channel allocation. Next-free is now the CEILING of
both scans (`max(grepNum, catNum) + 1`); the one real anomaly the cross-check still flags is `catalog-max > src-max` (a
catalog descriptor above every emitted code = an orphan reserved-but-never-emitted at the ceiling). Re-ran: `diag:
src-grep max COBOLNET1619 · catalog max COBOLNET1619 → next free = COBOLNET1620`. ✓

**Green baseline confirmed at HEAD `63c1fc6f`** (before any Wave-D code): `dotnet build CobolSharp.sln` 0W/0E · greenfield
unit **447/447** · characterization **33/33** byte-exact. Ready for the Wave-D remainder (real FLAG-14/FLAG-02 flagging ·
>>COBOL-WORDS · CC-directives-inside-COPY).

