## Entry 324 — Report Writer Increment B: INITIATE/GENERATE/TERMINATE end-to-end → RW101A baselined (NIST 360→361)

The Report Writer's first executable test passes. RW101A's CCVS report is **008 OF 008 EXECUTED, NO TEST(S)
FAILED**, and the RWCS output file is exactly the spec's 20 detail lines with LINE-COUNTER 000000→000019. This
is the full verb pipeline — grammar → bound → binder → IR → CIL → runtime — built as one coupled unit (the layers
must compile together) after a **5-agent parallel recon workflow** mapped every integration point (the verb
pipeline via OPEN/CLOSE, the symbol-model + LINAGE-COUNTER register pattern, the WRITE/ADVANCING runtime path, the
exact RW101A acceptance spec, and the existing scaffolding inventory). Key realization from the recon: **RW101A's
pass/fail is decided by LINE-COUNTER/PAGE-COUNTER values it reads directly in the PROCEDURE DIVISION** (MOVE
LINE-COUNTER…, IF LINE-COUNTER = …), not the RWCS output file — so those two special registers + correct counter
arithmetic in INITIATE/GENERATE are the heart of it.

What was built (RW101A's minimal subset — one DETAIL group, SOURCE at COLUMN, LINE NUMBER PLUS 1, PAGE LIMIT 20;
no control breaks/SUM/headings, which are the next increments):
- **Grammar** (`CobolReportWriter.g4` + `CobolParserCore.g4`): `initiate/generate/terminateStatement` rules wired
  into `statement`; `LINE_COUNTER`/`PAGE_COUNTER` alternatives added to `dataReference` (mirroring LINAGE_COUNTER).
- **Symbol model** (`ReportSymbol.cs` NEW, `Symbol.cs`): `ReportSymbol` (page geometry, control fields, group
  tree, host file from REPORT IS, computed line width) + `ReportGroupSymbol` (TYPE/LINE/COLUMN/SOURCE/SUM/PIC, the
  level-number tree). `SemanticBuilder` visits the RD + groups (`VisitReportDescriptionEntry`/`…GroupEntry`),
  captures each FD's REPORT IS link, and computes each field's receiving width via `PicDescriptorFactory`.
  `SemanticModel.ResolveReport`/`ResolveReportGroup`/`FindFirstReport`.
- **LINE-COUNTER / PAGE-COUNTER special registers** — the LINAGE-COUNTER pattern end to end: bound expressions
  (`BoundLineCounterExpression`/`…Page…`), `ExpressionBinder` branches, IR (`IrLineCounter`/`IrPageCounter`),
  `ExpressionLowerer`, and `CilExpressionEmitter` → `ReportWriterRuntime.GetLineCounter`/`GetPageCounter`
  (decimal). Also taught the **comparison** lowerer (`ConditionLowerer`) and **MOVE** lowerer
  (`DataMovementLowerer`) to treat them as runtime numerics — and normalized a figurative ZERO compared with a
  numeric arithmetic expression to the numeric 0 (ISO §8.3.1.2) so `IF LINE-COUNTER = ZERO` lowers (was COBOL0505).
- **Verbs**: bound nodes (`BoundInitiate/Generate/Terminate` + a `BoundReportLine`/`BoundReportField` plan), binder
  in `FileIoBinder` (resolves the group, flattens its entries into print lines — a LINE clause starts a line, each
  COLUMN+SOURCE adds a field — and binds each SOURCE to a location), lowering in `FileIoLowerer` (INITIATE/
  TERMINATE/BeginLine/EmitGroup as `IrRuntimeCall`; SOURCE placement as the dedicated `IrReportPlaceField` carrying
  an `IrLocation`), and CilEmitter dispatch (4 `EmitRuntimeCall` branches + `EmitReportPlaceField` via
  `EmitLocationArgs`). **GOTCHA avoided**: the `EmitRuntimeCall` if-chain silently NOPs unmatched names, so the 4
  new `ReportWriterRuntime.*` branches were essential (a missing branch → no-op, not a crash).
- **Runtime** (`ReportWriterRuntime.cs` NEW): per-report context (line buffer, LINE-COUNTER, PAGE-COUNTER, page
  limit). INITIATE resets PAGE-COUNTER=1/LINE-COUNTER=0; BeginLine clears to spaces; PlaceField does a left-
  justified alphanumeric move into the buffer at COLUMN (truncated to the field width); EmitGroup advances by the
  LINE value, page-breaks past LAST DETAIL, writes the line via `FileRuntime.WriteAdvancing`, updates LINE-COUNTER;
  TERMINATE clears the context. Reset wired into `FileRuntime.Init`.

Guard **ALL GREEN** — 1040 unit / 347 integration / **361 NIST** (RW101A: MATCH), 0 regressions. The baseline is
non-vacuous (008 EXECUTED, the 8 paragraph tests are real counter checks). Next RW increments: RW102A (multi-field
DETAIL — likely already close), then RW103A/104A (PAGE HEADING/FOOTING + absolute LINE NUMBER + page structure),
then control breaks/SUM for the broader module. `project_reportwriter` memory + ROADMAP updated.

