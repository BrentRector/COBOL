*>--- conformance-case
*> id: pb398-evaluate-partial-condition-subject
*> rules: [SR-14.9.13.3-7, SR-14.9.13.3-10]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.13.3 SR10 + Table 15: the Partial-expression ROW is blank under the CONDITION column, and
      *> SR7 d) says the same thing in words - "Partial-expression-1 shall correspond to a selection subject
      *> that is an identifier, a literal, an arithmetic expression, or a boolean expression". A class-test
      *> selection subject is condition-1 (14.9.13.4 GR3 e)), so pairing it with `> 5` is an invalid
      *> combination. kb/Work PB398: until the partial forms parsed, this cell could not be reached at all -
      *> the row was a lookup no operand could classify into, and therefore an UNVERIFIED one.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB398NEG1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X PIC X(3) VALUE "123".
       PROCEDURE DIVISION.
       MAIN-P.
           EVALUATE WS-X NUMERIC
               WHEN > 5   DISPLAY "GT5"
               WHEN OTHER DISPLAY "OTHER"
           END-EVALUATE
           STOP RUN.
