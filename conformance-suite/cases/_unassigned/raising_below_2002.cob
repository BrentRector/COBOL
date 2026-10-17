*>--- conformance-case
*> id: raising_below_2002
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> PROCEDURE DIVISION ... RAISING (ISO §14.2.2) is a COBOL-2002 introduction; below 2002 the parse gate
      *> re-diagnoses the RAISING token as COBOLNET0900 (P3 step 10 loud-hole fix — was a generic COBOL0001).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RB.
       PROCEDURE DIVISION RAISING EC-USER-MYERR.
       M. STOP RUN.
