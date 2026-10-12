## Entry 395 — Stage 1: route live COBOL arithmetic through CobolNum; a layering correction (unsigned sign) the wiring + guard caught

First live integration of the data-model numeric substrate (ADR §10 Stage 1). `PicRuntime.StoreArithmeticResult` —
the single choke point for **every** COBOL arithmetic store (ADD / SUBTRACT / MULTIPLY / DIVIDE / COMPUTE) — now
delegates its value-level **scale → round → capacity → SIZE ERROR** decision to `CobolNum.TryStore` (the BigInteger
core), keeping the byte encode/decode unchanged. The now-superseded legacy value-logic — `WouldOverflow`,
`IsInexactAtScale`, `CountDigits` — is removed (their logic lives in `CobolNum`, proven equivalent by the
differential oracle; `TreatWarningsAsErrors` would have flagged them dead anyway). `ApplyScalingAndRounding` stays —
it still serves the MOVE and DIVIDE-REMAINDER paths, which migrate in a later increment.

**A genuine design correction the live wiring surfaced (and the guard caught — exactly why we wire + guard).**
The full guard went red on two integration tests: `FUNCTION SIGN` into `PIC -9` and `FUNCTION FRACTION-PART` into
`PIC -9.99` lost their sign (`-1` → ` 1`, `-0.75` → ` 0.75`). Root cause: my Stage-0 `CobolNum.TryStore` applied the
unsigned-magnitude rule (`Abs` when `!profile.Signed`) as a step of the value-level store. But `pic.IsSigned` is
false for a **numeric-edited** field whose sign is an *edit symbol* (`-`/`+`/`CR`/`DB`), and such a receiver renders
its sign through the edit pattern in `FormatNumericEdited` — which needs the **signed** value. The unsigned-magnitude
rule (ISO §14.9.25 GR8) is a property of the receiver's **representation**, not of the value-level numeric store:
the byte codecs already drop the sign for an unsigned *plain* field, and a future typed unsigned field will drop it
at the typed-field store (Stage 3). Fix: `TryStore` returns the **signed** rounded value (capacity still bounds the
magnitude); the differential oracle now mirrors the representation (compares magnitude for unsigned plain fields).
The unit-level oracle had been internally consistent (it asserted magnitude) yet missed this, because only the live
numeric-edited consumer exercises the edit-pattern sign path — the value of integration testing over unit testing
alone. **`docs/DATA_MODEL_ARCHITECTURE.md` §5 updated** with the corrected `CobolNum` sign contract.

Guard **ALL GREEN: 1144 unit / 481 integration / 364 NIST** — the entire NIST arithmetic corpus (thousands of real
ADD/COMPUTE/… operations) now flows through `CobolNum` byte-identically. Next Stage-1 increment: route the numeric
MOVE path (`MoveNumericToNumeric`) and DIVIDE-REMAINDER through `CobolNum`, then begin Stage-0's `PicDescriptor` →
`FieldShape` split.

