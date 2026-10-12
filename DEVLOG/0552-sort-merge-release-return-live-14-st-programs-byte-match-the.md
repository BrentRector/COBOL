## Entry 552 — 2026-06-10 17:20 PDT — SORT/MERGE/RELEASE/RETURN live → 14 ST programs byte-match; the legacy guard re-proved on a grammar change

**The second wave-2 family.** The scout→implement pipeline delivered `StatementBinder.Sort.cs` /
`CSharpEmitter.Sort.cs` / `Runtime/IO/CobolSort.cs` + 12 differential facts and 9 anchored edits (incl. ONE
shared-frontend grammar change — the §14.9.34.3 SR4 reversed AT-END order on RETURN — so the FULL legacy guard
ran: **364 NIST MATCH, 1204 unit, 536 integration, ALL GREEN**). Design per §14.9.40/§14.9.24: SDs bind through
the same record path as FDs (`FileModel.IsSortMerge`; never host-registered — the store is the in-memory
`CobolSort` buffer); the three phases emit inline — implicit USING/GIVING transfers (GR12/GR15), INPUT/OUTPUT
PROCEDURE as bounded dispatcher ranges (GR11/GR14); stable sort = DUPLICATES IN ORDER (GR3); numeric keys decode
ALGEBRAICALLY via ParseDisplay (GR8 — overpunch images never compare as characters); alphanumeric keys ride the
PCS `CollatingTable`/statement COLLATING phrase per the GR5 precedence (the DEVLOG-546 seam paying off:
**ST137A/139A/140A/147A — the COLLATING-phrase programs — went green with zero new collating code**).

**Integration finds (mine):** (1) READ-only FD/SD records were never whole-group-marked — a record area is
filled WITHOUT conversion (§9.1.2), so DataBinder now marks every file record's numeric-DISPLAY leaves
StoreAsImage AT BIND (the SORT binder consults IsCharacterImage before the emitter's pass runs). (2) Six of the
agent's facts used a GO-TO-loop output procedure — the LEGACY truncates that loop to ONE iteration (the same
loop in a plain section works): a legacy-oracle hole; the six facts are now SPEC-PINNED with §-derived outputs
(every pinned expectation matched our output exactly, overpunch images included). (3) The agent's SD
DATA RECORDS edition gate (COBOLNET0873, deleted-by-2002) broke the INV-1 continuity spot-check — resolved the
RIGHT way: VERSION_CHANGE_REFERENCE gained its first **85→2002 row set (Table 7)** and ST101A's continuity case
became a documented-removal assertion (compiles at 85, rejects at 2023 with exactly 0873).

**14 ST programs byte-match and are locked** (ST101A 104A 106A 118A 119A 125A 132A 135A 136A 137A 139A 140A
146A→swept-only 147A; ST146A's golden encodes the legacy's LOW-VALUE fill in a spec-UNDEFINED record-area tail).
Chain consumers (ST103A/105A/111A read a predecessor's output file) need harness chaining — queued. NC census
holds 88/95, zero lost. **605 conformance + 15 unit green.** KeyedIO and CALL still parked.

