*>--- conformance-case
*> id: pb852-source-operand-parens
*> rules: [SR-13.18.53.3-7]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.53.3 SR7 - "If the SOURCE clause has more than one operand of
      *> which at least one is an arithmetic-expression, each operand shall be
      *> enclosed in parentheses."  This clause writes TWO operands, the first an
      *> arithmetic-expression, and neither is parenthesized.  The rule is what
      *> makes an operand LIST readable at all: operands are separated by nothing
      *> but a space (13.18.53.2's ellipsis repeats the brace pair), so
      *> `SOURCES ARE WS-A + WS-B WS-B` has two readings - one operand or two -
      *> and the standard removes the choice by requiring `(WS-A + WS-B) (WS-B)`.
      *> The rule is ENFORCED (COBOLNET2142) rather than inferred from the
      *> grammar's greedy shape, which would silently pick one reading.
      *> 2002 and above only: SR7 bites on a MULTI-operand clause, which SR6
      *> confines to a repeating entry, and 13.15.4 GR3's repetition vehicles
      *> (multiple COLUMN / LINE, report-group OCCURS) are 2002 introductions -
      *> at 85 the clause is refused for that reason instead (COBOLNET0900).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB852N1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb852n1.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01 WS-A PIC 999 VALUE 100.
       01 WS-B PIC 999 VALUE 23.
       REPORT SECTION.
       RD R-1 PAGE LIMIT IS 20 LINES.
       01 DET-A TYPE DE LINE PLUS 1.
          03 COLUMNS ARE 1 5 PIC 9(3) SOURCES ARE WS-A + WS-B WS-B.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT.
           INITIATE R-1.
           GENERATE DET-A.
           TERMINATE R-1.
           CLOSE RPT.
           STOP RUN.
