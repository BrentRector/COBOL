## Entry 352 — WS-SPEC #6 (Report Writer) sub-stage 6: GROUP INDICATE — completes the Report Writer + the M1 backlog

Backlog #6, sub-stage 6 — the last Report Writer piece, and with it the whole M1 spec-fix backlog. A GROUP
INDICATE field prints only on the first detail of the run, page, or control group, and is blanked on subsequent
details of the same group (ISO §13.18.28) — so a column of repeated values (e.g. the department) prints once per
group instead of on every line.

Implementation: `ReportGroupSymbol.GroupIndicate` (read from `reportGroupIndicateClause` in VisitReportGroupEntry).
`ReportContext.GroupIndicateFresh` is true at INITIATE (§13.18.28.4a), set true on a page advance (b) and a control
break (c), and cleared after each detail is written. `EmitGroup`, just before writing the detail, blanks each
registered GROUP INDICATE (column, width) when not fresh; `LowerInitiate` registers those positions
(RegisterGroupIndicateField). The detail's field value is always composed by LowerGenerate; the runtime suppresses
it on repeats.

Test (`ReportWriterSpecTests.GroupIndicate_PrintsOncePerGroup`): CONTROL IS WS-DEPT, a DETAIL with `COLUMN 1 GROUP
INDICATE SOURCE WS-DEPT`; GENERATE A/10, A/20, B/5 → `[A 010]`, `[  020]` (dept blanked on the repeat), `[B 005]`
(re-shown after the break); `[A 020]` never appears.
Guard ALL GREEN: **1047 unit / 429 integration (+1) / 364 NIST**, 0 regressions.

**M1 ('85) spec-fix backlog COMPLETE** (DEVLOG 342–352): COMP-4≡BINARY + reject COMP-n; variable-length MERGE and
SORT RELEASE/RETURN per-record length; cross-program GLOBAL FD I-O; FUNCTION SUM(table(ALL)) over ODO (SUM-scoped);
and the full **Report Writer** module — VALUE-literal body fields, REPORT/PAGE HEADING & FOOTING, data-SOURCE in
auto-presented groups, CONTROL-break detection + CONTROL HEADING/FOOTING, SUM accumulators, and GROUP INDICATE.
The flagship live COBOL-85 module is functionally complete. Next: the M2 (COBOL-2002) worklist (SORT Format-2
self-key, multi-char CURRENCY, CALL RETURNING/BY VALUE, READ PREVIOUS — each dialect-gated) and the M2→M4 march.

