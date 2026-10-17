*>--- conformance-case
*> id: pb65-leap-second-below-2002
*> rules: [AR-15.79.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> The >>LEAP-SECOND directive (ISO 7.3.17) is a COBOL-2002 compiler-directive introduction; below 2002 it
      *> is the four-compilers introduction diagnostic (COBOLNET0900), never silently consumed (kb/Work PB65 - the
      *> directive used to be recognized and discarded at every edition).
       >>LEAP-SECOND ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB65NLEAP85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC 9(6).
       PROCEDURE DIVISION.
           MOVE 1 TO R.
           STOP RUN.
