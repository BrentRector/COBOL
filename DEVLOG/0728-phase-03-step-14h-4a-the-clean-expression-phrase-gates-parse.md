## Entry 728 — 2026-07-09 20:08 PDT — PHASE-03 Step 14h.4a: the clean expression/phrase gates → parse-arm

Five bind-time gates with ONE unambiguous parse-tree detection point each move to the `ParseArm`:

| Gate | from (bind-time) | parse-arm | detect |
| --- | --- | --- | --- |
| `LogicalXorOperator2023` | `BindXorSequence` | `VisitLogicalXorExpression` | `ChildCount > 1` |
| `BareGotoRemoved2002` | `AlterBindBareGoTo` | `VisitGoToStatement` | `procedureName().Length == 0 && dataReference() == null` |
| `RoundedModeIs2014` | `RoundingOf` | `VisitRoundedPhrase` | `roundingModeName() != null` |
| `RetryPhrase2002` | `GateRetryIntro` (6 sites + the method) | `VisitRetryPhrase` | presence |
| `RecordLockPhrase2002` (verb) | `CheckRecordLockPhrase` | `VisitRecordLockPhrase` | presence; verb from parent statement type |

The `GateRetryIntro` helper + its six call sites (OPEN/READ/WRITE/REWRITE/DELETE/DELETE-FILE) are deleted; the ONE
`retryPhrase` grammar rule at those six sites is now covered by the ONE `VisitRetryPhrase` override — whose existence
the grammar already governs (the OPEN site's `{is2002()||retryPhraseAhead()}?` forward-detect only produces a
`retryPhrase` node on an unambiguous numeric tail), so presence IS the gate.

`RecordLockPhrase2002` has two producers with one constructId and two where-strings: the VERB phrase (WITH/NO/IGNORING
LOCK on READ/WRITE/REWRITE) moves here (`VisitRecordLockPhrase`, verb named from the parent), while the READ …
ADVANCING ON LOCK occurrence STAYS bound-arm (a distinct where-string on `BoundKeyedRead.AdvancingOnLock`) — both can
fire on one READ; they are NOT merged. `CheckRecordLockPhrase` keeps its §14.9.30/.51/.35 SR validation.

Battery: greenfield conformance 3114 · unit 227 · characterization 32 (fresh build); INV-1-strong + Release
warnings-as-errors verified locally; FULL legacy guard runs on CI (greenfield-only, no grammar change). Recon
wf_be806171-b25.

