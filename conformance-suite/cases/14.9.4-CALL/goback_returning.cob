*>--- conformance-case
*> id: goback_returning
*> rules: [SR-14.9.4.3-9, GR-14.9.4.4-4, GR-14.9.18.4-2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> GOBACK RETURNING - an IMPLEMENTOR EXTENSION, gated at the GOBACK statement's own COBOL-2002
      *> introduction (kb/Work PB407). ISO 14.9.18.2's general format has exactly two alternatives inside its
      *> choice-indicator bracket, raising-phrase and status-phrase; there is NO RETURNING or GIVING phrase,
      *> and 14.9.18.4 GR2's RETURNING is the PROCEDURE DIVISION HEADER's item - "If a RETURNING phrase is
      *> specified in the procedure division header of the program containing the GOBACK statement, the value
      *> in the data item referenced by that RETURNING phrase becomes the result of the program activation."
      *> The extension means exactly that: move the operand into the header RETURNING item, then return. This
      *> golden pins the ACCEPTED extension's behaviour; it is not evidence that the standard prints it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. GBRETMAIN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9(4) VALUE 40.
       01 B PIC 9(4) VALUE 2.
       01 R PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           CALL "GBRETSUB" USING A B RETURNING R.
           DISPLAY "SUM=" R.
           MOVE 7 TO A.
           MOVE 6 TO B.
           CALL "GBRETSUB" USING A B RETURNING R.
           DISPLAY "SUM2=" R.
           STOP RUN.
       END PROGRAM GBRETMAIN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. GBRETSUB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-SUM PIC 9(4).
       LINKAGE SECTION.
       01 LK-A PIC 9(4).
       01 LK-B PIC 9(4).
       01 LK-R PIC 9(4).
       PROCEDURE DIVISION USING LK-A LK-B RETURNING LK-R.
       P.
           COMPUTE WS-SUM = LK-A + LK-B.
           GOBACK RETURNING WS-SUM.
       END PROGRAM GBRETSUB.
