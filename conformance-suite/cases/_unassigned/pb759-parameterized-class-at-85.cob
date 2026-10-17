*>--- conformance-case
*> id: pb759-parameterized-class-at-85
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB759 -- a parameterized class and its REPOSITORY expansion below the introducing edition.
      *> CLASS-ID (11.3) and the REPOSITORY class-specifier (12.3.8) are COBOL-2002 introductions, and the
      *> USING clause (11.3.2) and the EXPANDS phrase (12.3.8.2) ride them, so at COBOL-85 the program draws
      *> the OO introduction diagnostic (COBOLNET0900) and not a parse error about a missing period.
       IDENTIFICATION DIVISION.
       CLASS-ID. PB759N85BOX.
       END CLASS PB759N85BOX.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB759N85HOLD USING ELEM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS ELEM.
       END CLASS PB759N85HOLD.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB759N85.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB759N85BOX
           CLASS PB759N85HOLD
           CLASS PB759N85HB EXPANDS PB759N85HOLD USING PB759N85BOX.
       PROCEDURE DIVISION.
           STOP RUN.
       END PROGRAM PB759N85.
