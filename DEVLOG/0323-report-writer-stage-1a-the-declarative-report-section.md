## Entry 323 — Report Writer Stage 1a: the declarative REPORT SECTION grammar (RD + report groups) now parses

With the header unblocked (Entry 322), RW101A reached the genuine Report Writer constructs. Implemented the
**declarative** half of the grammar (the report data description — committable on its own; the executable verbs
INITIATE/GENERATE/TERMINATE ship later WITH their runtime + codegen, per the parse-and-emit-together rule):

1. **FD `reportClause`** (§13.18.46) — `(REPORT IS? | REPORTS ARE?) reportName+` added to the FD clause list
   in `CobolData.g4` (`reportSection?` was already wired into `dataDivision`).
2. **Replaced the non-functional report-section stub.** The old `CobolReportWriter.g4` leaned on
   `genericClause : IDENTIFIER (IDENTIFIER|literal)*`, which cannot match real RW syntax (`PAGE LIMIT IS 20
   LINES` is all keyword tokens, not IDENTIFIERs). New COBOL-85 grammar: `reportDescriptionEntry` (RD +
   GLOBAL/CODE/CONTROL/PAGE-LIMIT[HEADING/FIRST DETAIL/LAST DETAIL/FOOTING] clauses) and `reportGroupEntry`
   (level + name + `TYPE IS` all 7 variants incl. RH/PH/CH/DE/CF/PF/RF abbreviations, `LINE NUMBER`
   absolute/`PLUS`/`NEXT PAGE`, `NEXT GROUP`, `COLUMN NUMBER`, `SOURCE`, `SUM…UPON…RESET`, `GROUP INDICATE`),
   reusing the existing `pictureClause`/`usageClause`/`signClause`/`justifiedClause`/`blankWhenZeroClause`/
   `occursClause`/`valueClause`. Removed the dead `VisitGenericReportGroupClause` override.
3. **Two reserved-word lexer tokens** (pre-authorized for the NIST effort; logged + full-guard): `GROUP`
   (`'GROUP'`, for GROUP INDICATE / NEXT GROUP) and `PLUSWORD` (`'PLUS'` — the reserved WORD, distinct from the
   `PLUS` `'+'` symbol). The word PLUS previously lexed as IDENTIFIER; the screen section had a documented hack
   (`(IDENTIFIER | PLUS)?` in `screenLineClause`/`screenColumnClause`) that relied on that — caught by the guard
   (`ScreenSection_LinePlusColPlus_ParsesSuccessfully` failed), then fixed cleanly by adding `PLUSWORD` to those
   clauses (no more IDENTIFIER hack). Swept the grammar + semantics for any other PLUS/GROUP-as-IDENTIFIER
   dependency — only those two screen clauses.

Result: RW101A's ENTIRE data division — including the full REPORT SECTION (RD, PAGE LIMIT IS 20 LINES, the
01/03 report groups with TYPE IS DETAIL / LINE NUMBER IS PLUS 1 / COLUMN NUMBER IS 5 / PICTURE X(76) / SOURCE IS
…) — now parses. The only remaining errors are the executable verbs (`INITIATE` at line 346), which are
Increment B. Guard **ALL GREEN** — 1040 unit / 347 integration / 360 NIST, 0 regressions (the lexer-token
additions were the gate). NOT baselining RW yet (correctly — it cannot run without the verbs + runtime).
Next (Increment B): bound nodes + `ReportWriterRuntime` + IR + CilEmitter dispatch for INITIATE/GENERATE/
TERMINATE, lowering GENERATE to evaluate SOURCE and place it by LINE/COLUMN, maintain LINE-COUNTER/PAGE-COUNTER.

