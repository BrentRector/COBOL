*>--- conformance-case
*> id: pb12-find-string-integer-position
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO 15.37.3 r3: "argument-3 shall be an integer data item or integer
      *> literal." An alphanumeric literal in that position is illegal.
      *>
      *> THIS IS THE ROW THE OLD TABLE SHAPE COULD NOT EXPRESS (fix-queue PB12).
      *> The screen carried ONE kind per FUNCTION, and FIND-STRING's argument-1
      *> and argument-2 are the STRING family (r1/r2) while argument-3 is an
      *> INTEGER — so a single-kind row would either have screened argument-3 as
      *> a string, rejecting the legal FIND-STRING(a b 2), or left the whole
      *> function unscreened. It was left unscreened. The per-position schema is
      *> what lets both halves be enforced at once.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB12NEGFIND.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC X(8) VALUE "ABCDABCD".
       01 N PIC S9(9)V99.
       PROCEDURE DIVISION.
           COMPUTE N = FUNCTION FIND-STRING(A "BC" "x")
           STOP RUN.
