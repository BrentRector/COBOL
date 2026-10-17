*>--- conformance-case
*> id: pb303_as_function_id_literal
*> rules: [SR-11.5.3-1]
*> source-format: free
*> portable: unverified
*>---
       *> kb/Work PB303 - the AS-phrase literal screen is ONE screen serving five
       *> clauses that state one rule.  Each paragraph gets its own negative so a
       *> regression that unwires ONE call site cannot hide behind the other four.
       *> ISO 11.5.3 syntax rule 1: "Literal-1 shall be an alphanumeric literal or a
       *> national literal and shall be neither a figurative constant nor a zero-length
       *> literal."  QUOTE is a figurative constant (ISO 8.3.3.6).
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB303FQ AS QUOTE.
       DATA DIVISION.
       LINKAGE SECTION.
       01  FN-R PIC 9(4).
       PROCEDURE DIVISION RETURNING FN-R.
       FN-P.
           MOVE 1 TO FN-R.
           GOBACK.
       END FUNCTION PB303FQ.
