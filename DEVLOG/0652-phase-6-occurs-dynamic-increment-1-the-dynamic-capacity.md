## Entry 652 — 2026-07-07 01:41 PDT — Phase 6: OCCURS DYNAMIC increment 1 — the dynamic-capacity table declaration + the growable `CobolDynTable<T>` substrate + the 2014 edition gate

The first slice of **OCCURS DYNAMIC** (ISO/IEC 1989:2023 §13.18.38 Format 4 / §8.5.1.9, COBOL-2014; data-model
design **D9**): a dynamic-capacity table — one whose current number of occurrences (its *current capacity*, §8.5.1.9.1)
grows and shrinks at run time. Per the D9 increment plan this is **the ONLY grammar/legacy-guard slice** of the
feature; increments 2–5 (CAPACITY-register read, SET Format 14, subscripted access, SEARCH/INITIALIZE, the
staged-loud variable-length-group guards) are greenfield-only and follow without touching the shared grammar.

**Landed.** (1) **Grammar** (`src/Cobol.Net.Frontend/Grammar/Core/`): a `CAPACITY` lexer token and, in
`CobolData.g4`, a `{is2014()}?`-gated `OCCURS DYNAMIC occursDynamicPhrase* occursKeyClause* (INDEXED BY? …)` alt with
`occursDynamicPhrase : CAPACITY IN? dataReference | FROM integerLiteral | TO integerLiteral | INITIALIZED`. (2)
**Model**: `OccursSpec` dynamic fields (`IsDynamic`/`CapacityName`/`CapacityRegister`/`InitialCap`/`ExpectedMax`/
`Initialized`) parsed in an `OdoBindOccursSpec` Format-4 branch (before the keyless early-return); `DataItem`
`IsDynamicTable`/`IsTable`/`FieldType` (a dynamic table's field type is `CobolDynTable<T>`, not `T[]`) with the
image-capable predicates excluding dynamic tables. (3) **Storage**: `CobolNet.Runtime.CobolDynTable<T>` — an
out-of-line growable `T[]`+count with `RefSending`/`RefReceiving`/`GrowTo` (per-occurrence seeding, §8.5.1.9.5),
`SetCapacity`/`CapacityUpBy`/`CapacityDownBy` (SET Format 14, EC-FLOW-SEARCH guard), `InitializeAll`,
`EnterSearch`/`ExitSearch`, and the EC-BOUND-TABLE-LIMIT fatal at the implementor max — the whole substrate ships now
even though only construction is source-reachable this increment. (4) **Emit**: a `FieldEmitter.FieldInit` dynamic
branch emitting `new CobolDynTable<T>(() => <seed>, FROM, TO?, INITIALIZED)`. (5) **Edition gate**:
`EditionGateHints.OccursDynamic` (2014) so a pre-2014 `OCCURS DYNAMIC`/`CAPACITY` probe upgrades the raw parse error to
**COBOLNET0900** naming COBOL-2014 + the `constructs.json` row.

**Version-matrix + drift**: the paired `occurs-dynamic-2014` row in both the canonical `tests/version-matrix/
constructs.json` (**active**, `expectDiagnostic COBOLNET0900`) and the in-code `ConstructDialectStatus` registry — the
P2.5 drift test now passes both directions, and `VersionMatrixTests` asserts the declaration compiles at 2014/2023 and
is rejected with 0900 at 85/2002 (424 matrix cells green). Golden **`dyn_declare`** (a GROUP-element dynamic table —
exercises the composed per-occurrence initializer) in the 2014 corpus, greenfield-only (the frozen legacy binder/emitter
has no dynamic-table model; the shared grammar parses it but legacy cannot emit it).

**Two D9 plan refinements, recorded in the deep-dive (process rule — say why the original wasn't followed):**
(a) **NO VCR row** — `VERSION_CHANGE_REFERENCE.md`'s own preamble states 2002→2014 *introductions* live in the matrix
`introducedIn` tag, not the VCR (which carries 2014→2023 Annex-E deltas + a few 2002→2014 behavior rows); the
constructs.json + registry pair IS the canonical introduction record. (b) **NO separate `dyn_pre2014` corpus golden** —
the corpus runner asserts compile+run SUCCESS only, so the below-2014 0900 rejection belongs to (and is asserted by) the
matrix row's `expectDiagnostic`, the one place negative gating lives. `DynamicResolve` deferred to inc 2/3 where the
CAPACITY register becomes a readable item.

**Verified**: greenfield battery **1982 conformance + 213 unit, 0 failures**; the full legacy guard (shared-grammar
discipline — `CAPACITY`/`DYNAMIC` are additive tokens/rules on the grammar the frozen legacy oracle loads). Diagnostic
band note: 08xx exhausted, so D9's declaration/CAPACITY/SET diagnostics reserve **1522–1528** (15xx band). NEXT: inc 2
— the CAPACITY register read + SET Format 14 + the capacity ECs, greenfield-only.

