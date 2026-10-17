*>--- conformance-case
*> id: pb432-varying-operand-expression
*> rules: [FMT-14.9.28.2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB432 — the §14.9.28.2 varying-phrase prints a BRACE GROUP in each operand slot:
      *> FROM { identifier-3 | index-name-2 | literal-1 }, BY { identifier-4 | literal-2 } (rendered from the
      *> printed page 683 / PDF 713). An arithmetic expression is not one of them. Both slots used to be typed
      *> `arithmeticExpression`, so this program compiled and ran at every --std with no diagnostic.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB432NEGEXPR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9(4) VALUE 2.
       01 B PIC 9(4) VALUE 3.
       01 I PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           PERFORM VARYING I FROM A + B BY B * 2 UNTIL I > 20
               CONTINUE
           END-PERFORM
           STOP RUN.
