## Entry 350 — WS-SPEC #6 (Report Writer) sub-stage 4: CONTROL-break detection + CONTROL HEADING/FOOTING

Backlog #6, sub-stage 4 — the headline Report Writer feature (control-break subtotal reports). The CONTROL
hierarchy + CH/CF groups were in the symbol model but never honored: no break detection, no CH/CF presentation.
ISO §13.18.16 (CONTROL), §14.9.16.4 GR4/GR5 (GENERATE order), §13.18.57 GR6c/e (CH/CF order). Design in
docs/REPORT_WRITER_CONTROL_DESIGN.md.

Generalized the auto-presented-group runtime from a fixed 4-slot array to a `Dictionary<int, AutoGroupPlan>` so
CONTROL HEADING/FOOTING (slots 4+2L / 5+2L for control level L) reuse the entire existing registration / IR /
emitter / lowering path — only the engine is new. `ReportContext.Controls` (List<ControlInfo>, major→minor,
FINAL first) holds each control item's live storage location + a saved prior value.

Engine:
- `ProcessDetailControls` (from EmitGroup, after RH/PH, before the detail): first detail → snapshot all controls
  + present CONTROL HEADINGs major→minor; subsequent → at the most-major non-FINAL control that changed (break
  level L), present CONTROL FOOTINGs minor→L with the ENDING group's control values RESTORED (so a CF's SOURCE
  control-item shows the group that just ended, not the new one — §13.18.16.4 GR2), restore current, then present
  CONTROL HEADINGs L→minor, then update the snapshots.
- `ProcessTerminateControls` (from TerminateReport, before the REPORT FOOTING): a final break — every CONTROL
  FOOTING minor→major with the last group's values (§13.18.16.4 GR5).
- New IR `IrReportRegisterControl` + emitter (pushes the control item's location, or null/0/0 for FINAL) →
  `ReportWriterRuntime.RegisterControl`; `LowerInitiate` registers the controls and maps CH/CF GroupKind → the
  encoded slot via `ControlLevelOf`.

Test (`ReportWriterSpecTests.ControlBreak_ControlFooting_IsPresentedWithEndingValue`): CONTROL IS WS-DEPT;
GENERATE A, A, B; a CONTROL FOOTING "ZF-" + SOURCE WS-DEPT → the A→B break presents "ZF- A" (the ending dept,
proving prior-restore), TERMINATE presents "ZF- B".
Guard ALL GREEN: **1047 unit / 427 integration (+1) / 364 NIST**, 0 regressions (RW101A-104A use no CONTROL; the
8 existing RW spec tests survive the Dictionary refactor). SUM accumulators (#6.5) are next.

