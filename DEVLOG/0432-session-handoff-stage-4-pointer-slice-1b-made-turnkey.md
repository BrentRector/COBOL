## Entry 432 — Session handoff: Stage-4 pointer slice-1b made turnkey (surface map + blast radius confirmed)

Consolidated the pointer subsystem to a turnkey state so a fresh session resumes slice 1b with zero re-discovery,
per the owner's directives reaffirmed this session: **production quality / decades of support / rearchitect the entire
codebase if needed / no backward compatibility required** (memory `feedback_production_refactor`, strengthened); the
**singular-pattern** rule; and **pointers = ONE `ManagedPointer` representation, no 8-byte handle, always-typed (not
`EnableTypedFields`-gated)** (Entry 431).

Mapped — and recorded in `docs/RECORD_STRUCT_STORAGE_DESIGN.md` §10.5 — the EXACT current-state rearchitecture
surface (verified against the live tree): storage `FieldSizeCalculator.cs:28`; classifier `DataItemClassifier.cs:175`;
the SET dispatch `DataStatementBinder.cs:187-204` (NULL/`q`) + `:301` (TO `q`); the compare path
`ConditionLowerer.cs:125` (maps `FigurativeKind.Null → '\x00'` for an 8-byte char compare) + the relation lowering;
the dead handle paths to delete (`CilDataEmitter.cs:616` pointer-MOVE, `StorageLocation.cs:190-210` 8-byte synth); the
new always-on pointer-field pass (sibling to `Binder.CollectTypedFields:278`); the new IR (`IrPointerStore`,
`IrPointerCompare`, `IrBasedDerefLocation`); and the **two guard-green commit boundaries**. **Blast radius confirmed:**
a corpus-wide grep finds **exactly one** program using pointers (`tests/conformance/2002/pointer_data.cob`) — so
boundary 1 is fully verifiable against that test + the guard; there is no other consumer to regress. Pointer equality
must be **address identity** (`ReferenceEquals(Buffer) && Offset==`), NOT record-struct `Equals` (which also compares
`Length`/`Pic`). `resume-prompt.md` (RESUME-AT + a directives-reaffirmed block), plan §0.5/§NEXT, and `CLAUDE.md` all
synced. No code change; guard unaffected (1196 / 507 / 364).

