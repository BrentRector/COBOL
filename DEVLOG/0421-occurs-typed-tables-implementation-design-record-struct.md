## Entry 421 — OCCURS → typed tables: implementation design (RECORD_STRUCT_STORAGE_DESIGN.md §9)

Before touching code, designed the OCCURS flip — it is the first *cross-cutting* typed increment (a grep shows 18
`is IrTypedFieldLocation` dispatch sites across 8 files), so a sprawling ad-hoc change would fight the
decades-maintainable bar. Wrote `RECORD_STRUCT_STORAGE_DESIGN.md §9`, grounded in the **actually-shipped** vocabulary
(the §3/§6.1 `IrDataSlot` plan was superseded by the ADR-§9 `IrLocation`-retarget that landed in 403–420; §9 opens by
reconciling that drift). Key decisions:

- **Representation**: a fixed `OCCURS n` over a flippable elementary element → a typed **.NET array** static field
  (`string[]`/`long[]`/`decimal[]`, ADR §4's sanctioned `T[]`); `CobolTable<T>` deferred (a plain array is
  byte-identical for fixed tables, no new runtime type). ODO / group-element / multi-dim / REDEFINES-over-table stay
  byte for now.
- **The crux that makes 18 sites tractable**: introduce an abstract `IrTypedLocation : IrLocation` (common
  `Width`/`Pic`/`IsDecimalNumeric`) with `IrTypedFieldLocation` (existing) and a new `IrTypedElementLocation`
  (`ArrayFieldName` + 0-based `Index` expr) as subclasses. Generalize only the **three value-access primitives**
  (`EmitTypedFieldValueLoad` → `ldelem`, `EmitTypedStorePrefix`/`Suffix` → `ldsfld array; index` / `stelem`); every
  numeric/char cell is expressed in terms of those primitives + `Width`/`Pic`, so they work on an element **unchanged**.
- **Resolver/init**: a subscripted ref to a typed array → `IrTypedElementLocation` with the COBOL 1-based subscript
  lowered to a 0-based index; `InitializeState` `newarr` + fills every slot (never `default(T)`). Byte-identity is the
  flat-field argument applied per element.
- **Staged sub-slices** (each guard-green + flip test): (1) char element any-subscript [lands the scaffolding];
  (2) numeric element [falls out of (1)]; (3) PERFORM VARYING/SEARCH; (4 later) group/multi-dim/ODO/INDEXED BY.

Per the doc's own "scaffolding lands with its first consumer (else it is dead code)" rule, slice 1 is one atomic
end-to-end commit — so this design is the separable deliverable; implementation starts next. No code change; guard
unaffected (**1196 / 498 / 364**).

**Design refinement (caught before coding).** Spiking the IR base + `IrTypedElementLocation` surfaced a gap §9 had
missed: a flipped `T[]` table has **no byte home** for a *whole-table* operand (`MOVE TABLE`, `DISPLAY TABLE`, a
spanning group MOVE). The classifier has no such trigger today (and the Binder excludes all OCCURS), so it never
arose. Resolution added to §9.3: slice 1 gives the `ProcedureScanner` a new demotion — a **non-subscripted (whole)
reference to an OCCURS item (or a group transitively containing one) → byte** — so only exclusively-element-accessed
tables flip; any whole-table use stays byte-backed (no whole-array↔byte materialize cell needed yet). The spiked IR
scaffolding was reverted (it must land atomically with its consumers in slice 1, not as dead code) — exactly the
"design before a cross-cutting change" payoff: a real correctness gap fixed on paper, not in a half-built 18-site edit.

