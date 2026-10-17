*>--- conformance-case
*> id: pb3101-nested-refmod-into-any-length
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3101 - a reference-modified argument BY REFERENCE into an ANY LENGTH formal of a contained
      *> program activated with the NESTED phrase. 14.8.2.3.2 "Additionally" d) relaxes the formal's LENGTH only;
      *> its PICTURE clause is still a clause rule 2 requires the argument to share, and the slice 8.4.3.3.4 GR5
      *> creates has none - "a slice matches no ANY LENGTH formal" (docs/CONFORMANCE.md D-RM1 b)), as the INVOKE
      *> lanes already answer -> COBOLNET1688. (Before the fix the CALL lane admitted it.)
      *> cite.py --check 14.8.2.3.2 "its length is considered to match the length of the corresponding
      *>   argument" -> OK 14.8.2.3.2 2)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N3101R.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 Y PIC X(8) VALUE "ABCDEFGH".
       PROCEDURE DIVISION.
           CALL "N3101S" AS NESTED USING Y(2:3)
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. N3101S.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC X ANY LENGTH.
       PROCEDURE DIVISION USING L.
           DISPLAY "S:" L
           GOBACK.
       END PROGRAM N3101S.
       END PROGRAM N3101R.
