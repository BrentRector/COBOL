      *> reject-at: 2002 2014
      *> kb/Work PB1412. ISO 1989:2023 8.8.2 rule 8: the boolean SHIFT operators are a COBOL-2023 introduction
      *> (Annex E.2), so an EVALUATE whose selection SUBJECT is a boolean expression with a shift operator -
      *> `EVALUATE A B-SHIFT-L 1 WHEN B"1000"` (14.9.13.2's boolean-expression-1) - is refused below 2023 by the
      *> introduction gate (COBOLNET0900), as the IF and COMPUTE forms are. The EVALUATE subject and object are two more
      *> grammar sites that admit a boolean expression, gated by VersionConformancePass.VisitEvaluateSubject /
      *> VisitEvaluateWhenItem (BooleanExpressionGateSiteDriftTests derives the list from the grammar).
      *>   cite.py --check 8.8.2 "Boolean shift operations shall be performed without regard for the usage of
      *>     the first operand." -> OK 8)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1412NES.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 1(4) VALUE B"1100".
       PROCEDURE DIVISION.
       MAIN.
           EVALUATE A B-SHIFT-L 1
              WHEN B"1000" DISPLAY "Y"
              WHEN OTHER DISPLAY "N"
           END-EVALUATE
           STOP RUN.
