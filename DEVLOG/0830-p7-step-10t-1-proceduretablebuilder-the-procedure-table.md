## Entry 830 — 2026-07-11 21:55 PDT — P7 Step 10t/1: `ProcedureTableBuilder` — the procedure table + declaratives leave the god class

**What (the FINAL-WIRING batch, sub-1 of the 10t plan — the table hoist).** The pc space + its resolver +
the DECLARATIVES half became ONE `Binding/Procedure/ProcedureTableBuilder(ctx)`: the `_paras ∥ _paraSection
∥ _paraMethod` lockstep lists, the `_sections`/`_paraIndex` maps, `AddParagraph`/`CollectParagraphs`,
`ResolveProcedure` (§8.4.2.2), `EntryPc`/`Declaratives`, and the whole `StatementBinder.Declaratives.cs`
partial (DeclCollectSection/DeclBindUse/DeclBindUseF3/DeclHandlerEndPc — ISO §14.2.4/§14.9.49 USE), all over
`ctx`. `BinderContext.Table` is the per-unit accessor. The two bind loops (program `Bind()` + the class
`BindMethodRoster()`) now read `Ctx.Table.Paragraphs/ParaSections/ParaMethods/EntryPc/Declaratives` and drive
the pc register through `table.AddParagraph`; the ambient section/method cursor stays on ctx (10s), entered
via `EnterMethodScope(table.ParaSections[i], table.ParaMethods[i], i)`. Collaborator table edges FLIP to
`ctx.Table.*`: ControlFlow/Sort/Ec/SetAlter `ResolveProcedure`, Ec's `Declaratives`/`EntryPc`/`Paragraphs`/
`ParaSections` (EcLocation), SetAlter's `Paragraphs`/`ParaSections`/`CurrentSection` — and **SetAlterBinder
drops its `host` param entirely** (ctx-only now, like ReportWriterBinder). The dead `MethodOf` deleted. The
host's `_paras`/`_sections`/`_entryPc`/`_declaratives`/`MethodOf` are GONE — the core is 411 lines (from
~700 pre-10p). One predicted fix: the fields slice initially over-reached into the OO-props block (the
SectionInfo class it used to stop at was promoted in 10s) — an assert caught it before any write; re-anchored
to the OoClasses doc.

**Verify.** Sln Debug clean; 33 characterization (32 snapshots byte-exact) · 281 unit · 3166 conformance —
verdicts read as separate actions.

