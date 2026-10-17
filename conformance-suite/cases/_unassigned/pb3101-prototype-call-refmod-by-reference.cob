*>--- conformance-case
*> id: pb3101-prototype-call-refmod-by-reference
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3101 - a reference-modified argument BY REFERENCE to a PROTOTYPED program's elementary formal.
      *> 14.9.4.3 SR25 imports 14.8.2 into the Format-2 CALL, and 14.8.2.3.2 rule 2 (a program "for which there is
      *> a program-specifier in the REPOSITORY paragraph") requires "the same ALIGN, BLANK WHEN ZERO, DYNAMIC
      *> LENGTH, JUSTIFIED, PICTURE, SIGN, and USAGE clauses". The unique data item 8.4.3.3.4 GR5 creates is
      *> described by no PICTURE clause, so it never conforms to an elementary PIC X(5) formal
      *> (docs/CONFORMANCE.md D-RM1) -> COBOLNET1688. (Before the fix it ran and printed P:ABCDE.)
      *> cite.py --check 14.9.4.3 "The rules for conformance specified in 14.8.2, Parameters and 14.8.3,
      *>   Returning items apply" -> OK 14.9.4.3 25)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N3101P PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 A PIC X(5).
       PROCEDURE DIVISION USING A.
       END PROGRAM N3101P.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. N3101M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM N3101P.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 Y PIC X(8) VALUE "ABCDEFGH".
       PROCEDURE DIVISION.
           CALL N3101P USING Y(1:5)
           STOP RUN.
       END PROGRAM N3101M.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. N3101P.
       DATA DIVISION.
       LINKAGE SECTION.
       01 A PIC X(5).
       PROCEDURE DIVISION USING A.
           DISPLAY "P:" A
           GOBACK.
       END PROGRAM N3101P.
