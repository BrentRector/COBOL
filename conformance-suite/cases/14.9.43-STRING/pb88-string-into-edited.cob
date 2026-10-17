*>--- conformance-case
*> id: pb88-string-into-edited
*> rules: [SR-14.9.43.3-5]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.43.3 SR5: "Identifier-3 shall not reference an edited data item and shall not be described with
      *> the JUSTIFIED clause." Both halves: a numeric-edited receiver, and a JUSTIFIED one (the second was not
      *> checked at all). kb/Work PB88: COBOLNET1651 at bind (a run-time stage before).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB88NSTRED.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 E PIC ZZ9.
       01 J PIC X(5) JUSTIFIED RIGHT.
       PROCEDURE DIVISION.
           STRING "1" DELIMITED SIZE INTO E.
           STRING "AB" DELIMITED SIZE INTO J.
           STOP RUN.
