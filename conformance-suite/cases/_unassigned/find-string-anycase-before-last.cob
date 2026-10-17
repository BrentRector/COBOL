*>--- conformance-case
*> id: find-string-anycase-before-last
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.37.2 fixes the phrase ORDER as well as the phrase set:
      *>   FUNCTION FIND-STRING argument-1 argument-2 [LAST] [[START AFTER] argument-3] [ANYCASE]
      *> LAST stands in the bracket BEFORE argument-3's bracket, which stands before ANYCASE's, so a
      *> call writing ANYCASE and then LAST is not a shape this format produces and shall be rejected.
      *> The existing negative fixture find-string-dangling-start-after pins the OTHER way this format
      *> can be misread (a START AFTER with no argument-3); this one pins the ordering itself, which
      *> matters because an order-free reading accepts it and computes an answer.
      *> FIND-STRING itself exists only from COBOL-2023 (§E.3.3 item 27), so 2023 is the only edition
      *> at which this rejection is §15.37.2's rather than the function's own edition gate.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1FSNEGORD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 H  PIC X(9) VALUE "ABCABCABC".
       01 ND PIC X(3) VALUE "ABC".
       01 P  PIC 9(4).
       PROCEDURE DIVISION.
           MOVE FUNCTION FIND-STRING(H ND ANYCASE LAST) TO P.
           DISPLAY P.
           STOP RUN.
