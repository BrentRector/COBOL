## Entry 351 — WS-SPEC #6 (Report Writer) sub-stage 5: SUM accumulators (the control-break subtotal)

Backlog #6, sub-stage 5 — completes the control-break report. A SUM counter in a CONTROL FOOTING accumulates a
data item over the details of its control group and prints the subtotal at the break, then resets (ISO §13.18.54).

Design (avoids runtime numeric-expression lowering): at INITIATE, each CF SUM field registers a kind-4 print
field + a `SumInfo` holding the running total and the addend's DISPLAY-numeric storage location + scale
(`IrReportRegisterSum` / `RegisterAutoSumField`; counter id = `report#slot#column`). At each detail GENERATE,
`EmitGroup` calls `AccumulateSums` AFTER `ProcessDetailControls` — so a CONTROL FOOTING (presented in the break
logic) prints the total of the details that PRECEDED the break (§13.18.54.4 GR7), then this detail's addend is
added. `PresentGroupPlan` prints a kind-4 field's accumulator and resets it to 0 (end-of-group, GR2). The addend
is decoded from its bytes (low nibble per byte, scaled) — unsigned DISPLAY; COMP/COMP-3 SUM addends and
numeric-edited SUM fields are later increments.

Test (`ReportWriterSpecTests.ControlFooting_SumCounter_SubtotalsAndResets`): CONTROL IS WS-DEPT; a CONTROL
FOOTING "T=" + SUM WS-AMT; GENERATE A/10, A/20, B/5 → the A→B break prints "T=0030" (10+20), TERMINATE prints
"T=0005" (proving the reset). Guard ALL GREEN: **1047 unit / 428 integration (+1) / 364 NIST**, 0 regressions.
GROUP INDICATE (#6.6) is the last Report Writer piece.

