*>--- conformance-case
*> id: pb485-level-77-screen-section
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.33.3 SR6: "Screen description entries shall have
      *> level-numbers 1 through 49." Like the report arm, the screen arm
      *> admits no special level, so 77 -- legal in working-storage under
      *> SR5 -- is illegal here. screenDescriptionEntry is the fourth
      *> grammar rule that spells a levelNumber. Reject-at omits 85
      *> because the SCREEN SECTION is a COBOL-2002 introduction; the SR6
      *> set itself is unchanged across every edition that has one.
      *> kb/Work PB485.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB485N5.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-X PIC X VALUE "A".
       SCREEN SECTION.
       01  SG.
           05  SI1 PIC X TO WS-X.
       77  S-BAD PIC X.
       PROCEDURE DIVISION.
           DISPLAY WS-X
           STOP RUN.
