*>--- conformance-case
*> id: l1c23-program-address-boolean-literal
*> rules: [SR-8.4.3.13.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §8.4.3.13.3 2) — literal-1 of ADDRESS OF PROGRAM shall be alphanumeric or national
      *> Rule: "Literal-1 shall be an alphanumeric or national literal whose length is not zero."
      *>   cite.py: OK  §8.4.3.13.3 2)  (Syntax rules)
      *> B"1" is a boolean literal of length 1 (boolean literals exist from COBOL 2002): legal as a
      *> literal, non-zero length, but neither alphanumeric nor national, so only this rule rejects it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C23F.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  PP USAGE PROGRAM-POINTER.
       PROCEDURE DIVISION.
       MAIN.
           SET PP TO ADDRESS OF PROGRAM B"1"
           STOP RUN.
       END PROGRAM L1C23F.
