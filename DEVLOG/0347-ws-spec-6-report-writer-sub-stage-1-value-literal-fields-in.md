## Entry 347 — WS-SPEC #6 (Report Writer) sub-stage 1: VALUE-literal fields in body groups

Backlog #6 (M1/'85 — the flagship live module). The Report Writer control-break / SUM / RH-RF / GROUP-INDICATE
work is large, so it is being built incrementally, each sub-stage guard-gated. Sub-stage 1 fixes the headline
body-field bug that blocks the rest: `FileIoBinder.BuildReportLines` emitted a field only when
`g.HasColumn && g.SourceName != null`, so a body-group (DETAIL / CONTROL HEADING/FOOTING) field whose value is
a VALUE literal — e.g. `05 COLUMN 1 PIC X(6) VALUE "TOTAL="` — was silently dropped (ISO §13.18.63).

Fix: BuildReportLines now also emits a field for a VALUE literal (a `BoundLiteralExpression` carrying the raw
captured text); `LowerGenerate` places a `BoundLiteralExpression` source via a new `IrReportPlaceLiteral`
instruction → `ReportWriterRuntime.PlaceLiteralField` (compose the literal bytes, place at COLUMN, truncate to
the field width). The quote-unwrapping reuses the existing `ExtractLiteral`. Data SOURCE fields are unchanged
(the existing `IrReportPlaceField` path); special-register SOURCE (LINE-/PAGE-COUNTER) and SUM-counter body
fields are later sub-stages.

Test (`ReportWriterSpecTests.Detail_ValueLiteralField_IsPlaced`): a DETAIL with `COLUMN 1 PIC X(4) VALUE "ID: "`
+ `COLUMN 5 PIC X(3) SOURCE WS-ID` (="ABC") → `[ID: ABC]` (the literal precedes the source).
Guard ALL GREEN: **1047 unit / 424 integration (+1) / 364 NIST**, 0 regressions. (A guard re-run was needed:
`FileIO_Start_PositionsForReadNext` flaked once on a parallel-load timeout — it passes in isolation and these
RW-only code paths never execute for a non-report program; noted as a guard-reliability item.)

