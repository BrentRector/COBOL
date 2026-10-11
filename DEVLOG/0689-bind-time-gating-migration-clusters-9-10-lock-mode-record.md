## Entry 689 — 2026-07-08 04:56 PDT — Bind-time gating migration Clusters 9+10 — LOCK MODE + record-lock phrase (combined; closes a latent gap)

Clusters 9 and 10 were COMBINED after 9-alone failed: they interact on one fixture and the same CobolIO.g4. LOCK MODE
(§12.4.5.9, 2002; LOCK/MODE hard-reserved): ungated `CobolIO.g4:69`, Check(LockModeClause2002) at the lockModeClause
branch (`DataBinder.cs`), retired the `LOCK when Next==MODE` signature arm. That alone RED-ed the
`record-lock-phrase-2002`@85 matrix row: its fixture (`LOCK MODE IS MANUAL` FILE-CONTROL + `READ F WITH LOCK`) got its
0900 INCIDENTALLY from the LOCK MODE PARSE gate; once LOCK MODE parses, the still-gated `READ … WITH LOCK` parse-errors
first (tree null → no bind) and EditionGateHints had NO record-lock arm — so NO 0900. This is precisely the recon's
predicted latent gap. Fix = also do Cluster 10: record-lock phrase (WITH LOCK / WITH NO LOCK / IGNORING LOCK /
ADVANCING ON LOCK, §14.9.30/.51/.35, 2002). Ungated `readAdvancingOnLock` + the three `recordLockPhrase` sites
(`CobolIO.g4:290/292/344/385`; LEFT `retryPhrase` gated — RETRY is a reservation word) and added
Check(RecordLockPhrase2002) at the ONE `CheckRecordLockPhrase` funnel all READ/WRITE/REWRITE verbs call, plus at the
READ ADVANCING-ON-LOCK recognition. record-lock had NO EditionGateHints arm, so this is a NET-NEW below-2002
diagnostic (the gap is closed: `READ F WITH LOCK`@85 was a generic error before, now COBOLNET0900). **AMBIGUITY_RISK
(three co-located optional READ tails: readAdvancingOnLock / retryPhrase [still gated] / recordLockPhrase; ADVANCING
shared with WRITE BEFORE/AFTER) CLEARED by the FULL guard byte-identical + a valid READ/RETRY probe.** Beyond-recipe:
`using CobolNet.Editions;` in `StatementBinder.FileLock.cs`. Verified: LOCK MODE + READ WITH LOCK → COBOLNET0900 at 85,
compile at 2002; RETRY still parse-gated. Battery: conformance 2055 · unit 224 · FULL legacy guard NIST 353 MATCH.

