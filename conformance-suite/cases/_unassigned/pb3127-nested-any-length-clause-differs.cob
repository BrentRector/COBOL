*>--- conformance-case
*> id: pb3127-nested-any-length-clause-differs
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3127 - an ANY LENGTH formal relaxes the LENGTH and nothing else. 14.8.2.3.2 rule 2 (a program
      *> "and the NESTED phrase is specified on the CALL statement") requires "the same ALIGN, BLANK WHEN ZERO,
      *> DYNAMIC LENGTH, JUSTIFIED, PICTURE, SIGN, and USAGE clauses", and "Additionally" d) says only that the ANY
      *> LENGTH formal's "length is considered to match the length of the corresponding argument". J is described
      *> with JUSTIFIED and L is not, so the CALL is refused -> COBOLNET1688 naming JUSTIFIED. The run-time CALL
      *> relation this lane asks compared only the category and the usage of such a pair, and admitted it.
      *> cite.py --check 14.8.2.3.2 "its length is considered to match the length of the corresponding
      *>   argument" -> OK 14.8.2.3.2 2)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N3127M.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 J PIC X(4) JUSTIFIED RIGHT VALUE "AB".
       PROCEDURE DIVISION.
           CALL "N3127S" AS NESTED USING J
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. N3127S.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC X ANY LENGTH.
       PROCEDURE DIVISION USING L.
           DISPLAY "S:" L
           GOBACK.
       END PROGRAM N3127S.
       END PROGRAM N3127M.
