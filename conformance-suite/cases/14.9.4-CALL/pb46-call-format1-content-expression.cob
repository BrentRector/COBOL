*>--- conformance-case
*> id: pb46-call-format1-content-expression
*> rules: [FMT-14.9.4.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.4.2 Format 1's BY CONTENT is `{ identifier-2 } ...` and nothing
      *> else. The expression operand belongs to FORMAT 2, which the AS phrase
      *> selects - so widening the shared callByContent GRAMMAR rule alone would
      *> have traded a rejection of legal Format-2 source for an acceptance of
      *> illegal Format-1 source. PB46's note refused that trade and was right to;
      *> the rule is parsed wide and narrowed here by whether AS was written.
      *> pb46_call_format2_as_nested assertion 5 is the same operand made legal by
      *> adding `AS NESTED`.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB46F1EXPR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC S9(4) VALUE 5.
       PROCEDURE DIVISION.
       MAIN.
           CALL "PB46F1IN" USING BY CONTENT N + 1.
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB46F1IN.
       DATA DIVISION.
       LINKAGE SECTION.
       01 P PIC S9(4).
       PROCEDURE DIVISION USING P.
       M.
           DISPLAY "GOT " P.
       END PROGRAM PB46F1IN.
       END PROGRAM PB46F1EXPR.
