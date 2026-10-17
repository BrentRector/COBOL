*>--- conformance-case
*> id: udf_keyword_omitted
*> rules: [GR-12.3.8.4-13]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §8.4.3.2 SR2 / §12.3.8 GR13-GR14 — FUNCTION ALL INTRINSIC in the REPOSITORY lets the word
      *> FUNCTION be OMITTED when referencing an intrinsic (SR6: a '(' after an intrinsic-function-name is
      *> always its argument list). MAX/MIN in COMPUTE and MOD in a MOVE sending position, all without the
      *> FUNCTION keyword. MAX(12,34)=34, MIN(12,34)=12, MOD(34,10)=4.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. UKOMIT.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION ALL INTRINSIC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC 9(4) VALUE 0012.
       01 WS-B PIC 9(4) VALUE 0034.
       01 WS-R PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE WS-R = MAX(WS-A, WS-B).
           DISPLAY "MAX=" WS-R.
           COMPUTE WS-R = MIN(WS-A, WS-B).
           DISPLAY "MIN=" WS-R.
           MOVE MOD(WS-B, 10) TO WS-R.
           DISPLAY "MOD=" WS-R.
           STOP RUN.
       END PROGRAM UKOMIT.
