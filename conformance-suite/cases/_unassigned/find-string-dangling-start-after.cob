*>--- conformance-case
*> id: find-string-dangling-start-after
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work R20 (ledger F28) - START AFTER with no argument-3: the introducer words were silently
      *> DISCARDED and the call degraded to the plain two-argument form. 15.37.2's bracket makes
      *> [START AFTER] argument-3 one unit.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R20NEG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 H PIC X(9) VALUE "ABCABCABC".
       01 N PIC X(3) VALUE "ABC".
       01 P PIC 9.
       PROCEDURE DIVISION.
           MOVE FUNCTION FIND-STRING(H N START AFTER) TO P.
           DISPLAY P.
           STOP RUN.
