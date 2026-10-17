*>--- conformance-case
*> id: pb565-report-multiple-line-at-85
*> rules: [SR-13.18.35.3-10]
*> source-format: free
*> portable: unverified
*>---
*> ⛔ THE MULTIPLE LINE CLAUSE ENTERED AT COBOL-2002 (kb/Work PB565), WITH THE REST OF THE REPEATING-ENTRY
*> FAMILY. ISO/IEC 1989:2023 §13.18.35.3 SR10 defines it — "If more than one integer-1 or integer-2 operand is
*> specified, the clause is referred to as a multiple LINE clause" — and §13.18.35.4 GR9 makes it "functionally
*> equivalent to a LINE clause with a single operand, together with a simple OCCURS clause whose integer is
*> equal to the number of operands of the LINE clause, except that the multiple LINE clause allows the report
*> lines to be defined at unequal vertical intervals". The COBOL-85 LINE clause took ONE operand, which is why
*> the whole family gates together here: `report-occurs-2002`, `report-multi-line-2002`, the multiple/relative
*> COLUMN forms, VARYING and PRESENT WHEN.
*>
*> So a COBOL-85 compilation shall name the edition rather than silently print one line: construct
*> `report-multi-line-2002`, diagnostic COBOLNET0900. Above 85 the same source compiles and prints M on page
*> lines 2, 4 and 7.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB565L85.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb565l85.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R-L85.
       REPORT SECTION.
       RD  R-L85 PAGE LIMIT IS 20 LINES.
       01  D-L85 TYPE DE.
           03  LINE 2 4 7.
               05  COLUMN 1 PIC X(1) VALUE "M".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT.
           INITIATE R-L85.
           GENERATE D-L85.
           TERMINATE R-L85.
           CLOSE RPT.
           STOP RUN.
