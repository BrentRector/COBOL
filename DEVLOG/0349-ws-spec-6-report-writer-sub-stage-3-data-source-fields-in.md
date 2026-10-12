## Entry 349 — WS-SPEC #6 (Report Writer) sub-stage 3: data-SOURCE fields in auto-presented groups

Backlog #6, sub-stage 3. An auto-presented group (PAGE/REPORT HEADING/FOOTING — and the coming CONTROL
HEADING/FOOTING) could only show VALUE literals + LINE/PAGE-COUNTER; a SOURCE of arbitrary working-storage data
(ClassifyPageField kind 3) was blank-filled (ISO §13.18.53). This is the foundation for a CONTROL FOOTING that
shows its control value.

Fix: the field plan keeps the live storage location for a data SOURCE and the runtime reads it at presentation
time. New IR `IrReportRegisterDataField(reportName, slot, column, width, IrLocation source)` →
`ReportWriterRuntime.RegisterAutoDataField(… byte[] area, int offset, int size)`; FieldPlan gains
SrcArea/SrcOffset/SrcSize for kind 3, and `PresentAutoGroup` places the live bytes (read at present time, so a
later MOVE to the WS item is reflected). `RegisterAutoGroup` resolves a kind-3 field's SOURCE to its location
and emits the new node instead of the literal/counter `RegisterAutoField`. Raw-byte placement (alphanumeric and
unedited display-numeric SOURCE); numeric-edited report fields remain a later concern.

Test (`ReportWriterSpecTests.PageHeading_DataSource_IsPresented`): a PAGE HEADING `COLUMN 1 PIC X(5) SOURCE
WS-HDR` (="PAGE!") → the page heading shows "PAGE!" before the detail (was blank).
Guard ALL GREEN: **1047 unit / 426 integration (+1) / 364 NIST**, 0 regressions.

