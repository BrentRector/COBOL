*>--- conformance-case
*> id: find-string-four-operands
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.37.2 gives FIND-STRING exactly three argument positions —
      *>   FUNCTION FIND-STRING argument-1 argument-2 [LAST] [[START AFTER] argument-3] [ANYCASE]
      *> — argument-1 and argument-2 required, argument-3 optional and written once. The format has no
      *> repetition, so a fourth operand argument is not a shape it produces and shall be rejected
      *> rather than silently ignored. This is the MULTIPLICITY half of the format; the ordering half
      *> is find-string-anycase-before-last and the introducer half is
      *> find-string-dangling-start-after.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1FSNEGCNT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 H  PIC X(9) VALUE "ABCABCABC".
       01 ND PIC X(3) VALUE "ABC".
       01 P  PIC 9(4).
       PROCEDURE DIVISION.
           MOVE FUNCTION FIND-STRING(H ND 1 2) TO P.
           DISPLAY P.
           STOP RUN.
