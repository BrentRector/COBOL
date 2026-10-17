*>--- conformance-case
*> id: pb398-evaluate-partial-true-subject
*> rules: [SR-14.9.13.3-7, SR-14.9.13.3-8, SR-14.9.13.3-10]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.13.3 SR10 + Table 15: the Partial-expression ROW is blank under the TRUE-or-FALSE column.
      *> SR8's own rewrite says why - it makes the corresponding selection subject "treated as though it were
      *> specified by the word TRUE", which is only meaningful when the subject supplies the expression's
      *> missing leftmost operand; the word TRUE supplies none. The pair is refused at COMPILE time (SR10 is a
      *> syntax rule), never as the run-time fault kb/Work PB47 found for the other blank cells.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB398NEG2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-N PIC S9(3) VALUE 7.
       PROCEDURE DIVISION.
       MAIN-P.
           EVALUATE TRUE
               WHEN > 5   DISPLAY "GT5"
               WHEN OTHER DISPLAY "OTHER"
           END-EVALUATE
           STOP RUN.
