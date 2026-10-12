## Entry 858 — 2026-07-16 05:12 PDT — P10 Step-6 PROC-5-allocate slice — ALLOCATE based-item INITIALIZED lands as the §14.9.3 GR7 lowering onto the ONE INITIALIZE expansion; both audit gaps closed

**The spec text IS the implementation.** §14.9.3 GR7: "If both the INITIALIZED phrase and data-name-1 are
specified, the allocated storage is initialized as if an `INITIALIZE data-name-1 WITH FILLER ALL TO VALUE
THEN TO DEFAULT` statement were executed." The Step-1 audit (PROC-5-allocate, 2 gaps) had this staged as
`BoundUnsupported` at `PtrBinder.cs:127-129` — a valid 2002 program compiled but died at runtime. The landing
binds LITERALLY that statement: a new `InitializeBinder.BindAllocateInitialized(basedRef)` builds the
GR7 `InitializeSpec` (WithFiller=true, bare-ALL ToValue, no REPLACING, ToDefault=true) and runs the ONE
existing `BindInitializeTarget` expansion over the based item's data reference — the based item resolves to
its `RedefViewPlace` window (`CobolPtr.OffsetOf(__addr_X) + …` over the deref bridge), so every store windows
the freshly-allocated cell. `PtrBinder.BindAllocate` form 2 now returns
`BoundSequence([BoundAllocate(based, …, Initialized: true, returning), BindAllocateInitialized(basedRef)])` —
sequenced AFTER the allocation so the implicit pointer (GR4a) is set before the stores deref it. ZERO new
emitter surface: `BoundSequence` renders children consecutively, `BoundInitialize` rides `InitializeEmitter`
→ the ONE MOVE path (VALUE clauses honored per §14.9.20 GR6a3; numerics/numeric-edited default to ZEROES —
the EDITED zero through MOVE editing; character categories to SPACES; FILLERs included). The GR5
not-available leg is unreachable here (a form-2 request is the template width > 0; `CobolPtr.Allocate`
always satisfies a positive size) — noted at the bind site.

**Gap 2 (no runtime witness for GR6/GR4b) closed by the same golden.** New ENABLED
`tests/conformance/2002/allocate_initialized.cob/.out` (PROGRAM-ID `ALLOCINIP10AL`): a mixed BASED group —
VALUE-carrying `PIC X(4) VALUE "AB12"` + `PIC 9(3) VALUE 7` (→ `AB12`/`007`, the ALL-TO-VALUE leg), FILLER
X(3) (WITH FILLER → spaces, observed through a second based window), defaulted `9(4)` → `0000`, `X(5)` →
spaces, and `Z9` → ` 0` (the GR6c EDITED zero, never spaces) — plus GR4b (the RETURNING pointer rebased onto
a `PIC X(10)` based item shows `AB12007   ` — same storage), the elementary GR7 leg (`ALLOCATE W INITIALIZED`
→ spaces), GR6 (`ALLOCATE 6 CHARACTERS INITIALIZED` = LOW-VALUE), and FREE-to-NULL. Output derived from the
GRs BEFORE running; the run matched byte-for-byte, and `--std 2023` is byte-identical (continuity) while
`--std 85` rejects through the existing `allocate-2002`/BASED 0900 gates. NO new matrix row: INITIALIZED is a
phrase of the already-gated ALLOCATE statement (the M2-UDF-4 phrase-level precedent); NO new negative: the
§14.9.3 SRs add no INITIALIZED-specific bindable violation (SR1/SR2/SR3 negatives already exist). Legacy
runner: `GreenfieldOnly` exclusion — the legacy CIL emitter satisfies INITIALIZED by zero-fill only, so the
GR7-conforming baseline is greenfield truth. `PointerAddressingTests` gains the `ALLOCATE B INITIALIZED
RETURNING P` compile lock.

**Verification (targeted):** CorpusRunner+PointerAddressing 199/199; characterization 33/33 byte-identical
(no snapshot uses ALLOCATE INITIALIZED); CobolPtr/Initialize units 8/8. Docs swept in the same change:
PHASE-10 STATUS + Step-6 checkbox/body + the PROC-5 audit verdict/gaps (both CLOSED), PHASE4_RECONCILIATION
(M2-PROC-5 row + the increment-2 design deviation #4), ISO2023_CONFORMANCE_PLAN §3 M2-PROC-5 ☑,
constructs.json `allocate-2002` description.

