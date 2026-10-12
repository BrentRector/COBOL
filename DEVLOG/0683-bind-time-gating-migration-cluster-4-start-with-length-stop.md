## Entry 683 — 2026-07-08 03:26 PDT — Bind-time gating migration Cluster 4 — START … WITH LENGTH + STOP RUN … WITH STATUS

Fourth cluster; two optional phrases with hard-reserved lead tokens (WITH). START … WITH LENGTH (§14.9.41, 2002):
ungated `CobolIO.g4:459`, added `Check(…, Constructs.StartWithLength2002, "the START … WITH LENGTH phrase")` at the
phrase's recognition point in `KeyedBindStart` (its sibling START FIRST/LAST already Checks in the same method).
STOP RUN … WITH {NORMAL|ERROR} [STATUS] (§14.9.42, 2002): ungated `CobolControlFlow.g4:245`. Its recognition point
was **parse-then-drop** — the `stopStatement` switch-arm (an EXPRESSION producing `BoundStop`/`BoundStopLiteral`)
never read `stopStatusPhrase()`; extracted a `BindStop` helper that Checks `Constructs.StopRunStatus2002` when the
phrase is present, then returns the bound node. BONUS (the recon predicted it): the `STATUS …` alt WITHOUT `WITH`
was uncovered by the old `EditionGateHints` `WITH`-keyed signature; keying off the parsed phrase now covers both
alts. Retired the two `WITH`-keyed signature arms (STOP-status + START-length). Verified: both → COBOLNET0900 at 85,
compile at 2002; plain `STOP RUN` stays clean. Battery: conformance 2055 · unit 224 · FULL legacy guard NIST 353
MATCH.

