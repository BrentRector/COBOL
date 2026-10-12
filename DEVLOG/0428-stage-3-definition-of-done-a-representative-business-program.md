## Entry 428 — Stage-3 "definition of done": a representative business program flips its WHOLE data division (byte-identical)

A milestone validation, not a new feature: a realistic ordinary COBOL program now compiles with its **entire data
division** as native .NET types and runs byte-for-byte identically to the byte engine — the design doc's §8
definition of done. The program has a nested group `CUSTOMER` (a `CUST-ADDR` sub-group with char `CITY` + `long`
`ZIP` + `decimal` `BALANCE`), a numeric OCCURS table `MTOT`, and standalone `long`s, exercised across DISPLAY,
decimal ADD, MOVE, PERFORM VARYING, COMPUTE-into-table-element, and IF. Reflection confirms the whole division
flipped: `_TS_CUSTOMER` (nested record struct), `_TA_MTOT` (`Int64[]`), `_T_IDX`/`_T_GRAND` (`Int64`) — and the
output matches the byte path exactly. New flip test `RepresentativeBusinessProgram_WholeDataDivisionFlips_ByteIdentical`.

**State of the data-model migration (Stage 3):** the core is COMPLETE — character → `string`; numeric → `long`
(unsigned-int) / `decimal` (signed-scaled) over DISPLAY/COMP/BINARY across VALUE/MOVE/DISPLAY/COMPARE/class-cond/
arithmetic/figurative-ZEROS; flat groups + **nested groups** → (nested) `record struct`s; fixed **OCCURS** tables
(char + numeric) → `T[]` with subscripted + PERFORM-VARYING access; SEARCH safely byte; whole-table/whole-group
operands and every byte-trigger (REDEFINES/RENAMES/edited/file/EXTERNAL/LINKAGE/ref-mod/ODO) correctly stay byte.
All gated behind `EnableTypedFields` (default OFF → corpus byte-identical); 24 flag-on≡flag-off differential tests.
Guard **ALL GREEN: 1196 unit / 507 integration / 364 NIST**.

**Remaining migration work** (the large stages): Stage-4 **pointers** (typed flip to **managed .NET references** —
the ADR's `ManagedPtr`, GC-tracked, no native heap / no handle table; the PointerRegistry approach is REJECTED and
this is a SETTLED decision, NOT owner-gated — owner directive, DEVLOG 428) and **OO** (.NET classes); Stage-5
**Roslyn C# backend**; Stage-6 finalize + flip-on-by-default decision + rename. All are autonomous-eligible.

