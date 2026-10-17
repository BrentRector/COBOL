*>--- conformance-case
*> id: l1-integer-of-date-arity2
*> rules: [FMT-15.46.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.46.2 general format — FUNCTION INTEGER-OF-DATE ( argument-1 ).
      *> ONE argument, unbracketed: no optional trailing argument, no repetition
      *> group, no phrase. A second argument is not a form this function has, at
      *> any edition (§15.46 is a COBOL-85 function, so all four run it).
      *> The accepting side is 2023/l1_integer_date_form_returned.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1NEGIODATE2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC 9(7).
       PROCEDURE DIVISION.
           COMPUTE R = FUNCTION INTEGER-OF-DATE(19950215 1)
           STOP RUN.
       END PROGRAM L1NEGIODATE2.
