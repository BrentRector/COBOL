## Entry 721 — 2026-07-09 16:11 PDT — PHASE-03 Step 14d: two flag-backed statement gates (CALL ON OVERFLOW, STOP…STATUS)

Two edition gates whose gated FACT is a syntactic detail the bound tree previously DROPPED now ride a new **byte-neutral
semantic flag** on their distinctive node (the recon ledger's Group C-flag — a resolved boolean, NOT a raw parse
context; the `BoundTree.cs` invariant stands), read by the pass:

- **CallOnOverflowRemoved2023** → new `BoundCallProgram.UsedOverflowSpelling` (true iff the ON or NOT ON phrase used the
  archaic OVERFLOW synonym for EXCEPTION — the COBOL-74 carry-over REMOVED at ISO 2023, Annex E.2 item 1c). Set in
  `CallBindCall`; the `CallGateExceptionSpelling` helper (which only fired the Check) is DELETED.
- **StopRunStatus2002** → new `BoundStop.HasStatusPhrase` (STOP RUN … WITH NORMAL/ERROR STATUS, §14.9.42, COBOL-2002;
  the status VALUE is still unmodeled — only the phrase's presence). Set in `BindStop`.

Both binder Checks removed; the pass gates on the flags (BY VALUE before ON OVERFLOW — the binder's order). **Battery:**
edition/CALL/STOP tripwires **1829**, conformance **3114**, characterization **32**, unit **227**, FULL legacy guard
NIST **353 MATCH** (0 regressions — greenfield-only). NEXT — **14e:** the remaining flag / multi-site statement gates
(END-ACCEPT, INVOKE-explicit, ROUNDED MODE IS, sequential record-lock, RETRY).

