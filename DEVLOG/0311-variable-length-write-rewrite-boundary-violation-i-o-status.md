## Entry 311 — Variable-length WRITE/REWRITE boundary violation → I-O status 44 (the SQ212A crash); declaratives-return bug exposed

**The diagnosed bug.** SQ212A (RECORD IS VARYING IN SIZE FROM 18 TO 2048 … DEPENDING ON RECORD-LENGTH) was a
0-byte false-green removed in DEVLOG 302 because it *crashed*: `RT0001: ... during WRITE on 'SQ-VS7': buffer
access out of range (offset=120, length=2049, bufferLength=2168)`. The test deliberately writes records whose
length walks past the bounds — "3 SHORTER RECORDS" (RECORD-LENGTH 15/16/17, **below** the FROM-minimum 18) and
"9 LONGER RECORDS" (lengths 2049+ , **above** the TO-maximum 2048) — and expects each such WRITE to be rejected
with **I-O status 44** (ISO §9.1.13, *I-O status = 44, boundary violation*: "an attempt is made to write *or
rewrite* a record that is larger than the largest or smaller than the smallest record allowed by the RECORD IS
VARYING clause"). Our `FileRuntime.WriteRecordVariable` instead passed the out-of-range length straight to
`RuntimeGuard.Buffer`, which (correctly, since DEVLOG 309) threw rather than overrun the record area — but a
spec-defined 44 is not an internal error.

**The fix (all three orgs, WRITE and REWRITE).** The bounds live in the symbol table (`FileSymbol.Record-
VaryingMin`, and the true maximum from `SemanticModel.MaxRecordLength`) but were never conveyed to the runtime.
Plumbed them through the existing varying-flag channel: `FileRuntime.Set{Sequential,Relative,Indexed}Varying`
gained `(int minSize, int maxSize)` parameters (Binder emits two extra `IrLoadConst` ints per call; the three
`CilEmitter` dispatch cases widened their `GetMethod` signature to `{string,bool,int,int}`); each handler stores
`MinVaryingRecordSize`/`MaxVaryingRecordSize` (added to `IFileHandler`). A single centralized
`FileRuntime.VaryingBoundsViolated(handler, length)` (`length > max` or `length < min`, with 0 disabling a side)
is consulted at the start of BOTH `WriteRecordVariable` and `Rewrite`; a violation sets
`FileStatus.RecordBoundaryViolation` ("44") and returns with no record transfer, BEFORE the guard/slice — so no
crash and the record is left unchanged. Because all variable WRITEs funnel through `WriteRecordVariable` (via
`IsVaryingRecord` → `IrWriteRecordVariable`) and all REWRITEs through `Rewrite`, the one check covers
sequential/relative/indexed at once. The sequential handler's stricter §14.9.35 GR16 rule (a REWRITE length that
merely *differs* from the read record's length is also 44) is unchanged and still fires for in-bounds lengths.
Only the upper bound is reachable with zero new plumbing (`_recordLength` is already the max); the lower bound
needed the new `minSize` — and SQ212A's "3 SHORTER RECORDS" proves it was worth doing: a max-only fix would have
silently failed those three sub-minimum WRITEs. (Lesson reaffirmed: read the .cob and verify which bounds the
test exercises — don't scope from the first symptom.)

Guard ALL GREEN: **1040 unit / 347 integration / NIST all green, 0 regressions** — the change touches the
WRITE/REWRITE chokepoint and the varying registration for every org, yet none of the ~150 file-I/O baselines
moved (in-bounds writes are unaffected; `length == max` is not a violation).

**Transparency — SQ212A is NOT yet baselined; a second, deeper bug was exposed.** With the boundary check in
place SQ212A now runs to completion (rc=0), the data file holds the correct 2031 records, and every test
paragraph PASSes (DECL-STATUS-44-0 shorter + longer, READ 1030/1001, AT END, REWRITE-44-1/44-2). BUT the CCVS
report prints the "END OF TEST" footer **14 times** (once per status-44 event) instead of once. Root cause: the
`USE AFTER STANDARD EXCEPTION PROCEDURE ON SQ-VS7` declarative (section `SECT-SQ212A-0001`) handles the
exception and `GO TO EXIT-PARA` (an `EXIT.` paragraph that is the handler's intended return point), but our
USE-procedure dispatch (`FileIoLowerer.EmitPerformDeclarativeSection` → `IrPerformThru(first … last-para-of-
section)`) sets the THRU end to the section's *physical* last paragraph. The CCVS declaratives section places the
program-termination tail (`CLOSE-FILES1` → `PERFORM END-ROUTINE1 THRU END-ROUTINE1-13` = the footer → ultimately
`STOP RUN`) AFTER `EXIT-PARA` in the same section, so each declarative firing falls through `EXIT-PARA` into
`CLOSE-FILES1` and re-emits the footer. This is the DEVLOG 259–260 declaratives-dispatch class; it was masked
until now because SQ212A crashed before any USE procedure could fire. **The correct fix is to end the
USE-procedure PERFORM-THRU range at the declarative's exit (the trailing `EXIT.` paragraph) rather than the
section's physical last paragraph — deferred to the next item to keep this commit a clean, isolated, spec-cited
runtime fix and because the declaratives-dispatch change has blast radius across every USE-procedure test.**
SQ212A stays out of the guard until that fix lands; NIST count unchanged.

