## Entry 348 — WS-SPEC #6 (Report Writer) sub-stage 2: REPORT HEADING / FOOTING presentation

Backlog #6, sub-stage 2. The RWCS auto-presented only PAGE HEADING/FOOTING; TYPE REPORT HEADING / REPORT
FOOTING groups were never registered or presented (ISO §14.9.16.4 GR4a: RH first, before any PAGE HEADING;
§13.18.57 GR6g: RF last, at TERMINATE).

Generalized the runtime's auto-presented-group mechanism from 2 hard-coded slots (HeadingFields/FootingFields)
to a 4-slot model indexed by Slot* (PageHeading=0, PageFooting=1, ReportHeading=2, ReportFooting=3):
`ReportContext.GroupLines[4]` + `GroupFields[4]`. `RegisterPageGroup/RegisterPageField` → `RegisterAutoGroup/
RegisterAutoField` (the `isFooting` bool becomes a `slot` int through the IR/emitter); `PresentPageGroup` →
`PresentAutoGroup(ctx, slot)`. `EmitGroup` now presents the REPORT HEADING once (guarded by
`ReportHeadingPresented`) before the first PAGE HEADING; `TerminateReport` presents the REPORT FOOTING before
removing the context. `LowerInitiate` registers all four group types (GroupKind → slot). The page mechanics,
FIRST DETAIL positioning, and LINE/PAGE-COUNTER composition are unchanged — RH/RF reuse the same FieldPlan path
(VALUE-literal + counter fields; data-SOURCE in auto groups is a later increment, still kind-3 blank).

Test (`ReportWriterSpecTests.ReportHeadingAndFooting_ArePresented`): RH "TITLE" + DETAIL + RF "END" → the
report shows TITLE before the detail and END after it.
Guard ALL GREEN: **1047 unit / 425 integration (+1) / 364 NIST**, 0 regressions (the 5 existing RW tests +
RW101A-104A NIST baselines unaffected by the page-mechanism generalization).

