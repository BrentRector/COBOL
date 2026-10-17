*>--- conformance-case
*> id: pb569-picture-comma-not-last
*> rules: [SR-13.18.40.3-7]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.40.3 SR7: "If the symbol ',' or the symbol '.' is the last symbol of
      *> character-string-1, the PICTURE clause shall be the last clause of the data description entry
      *> and shall be followed immediately (without an intervening separator space) by the separator
      *> period." In "999,, USAGE" the first ',' is followed by ',' - a picture symbol - and the second
      *> by a space - a separator (8.3.5 rule 2) - so character-string-1 is "999," and USAGE follows it.
      *> kb/Work PB569: PICMODE trims ONE trailing separator and nothing re-examined SR7, so this bound
      *> the legal "PIC 999,." item silently at every edition. Refused COBOLNET2419.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB569N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W1 PIC 999,, USAGE DISPLAY.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 12 TO W1
           DISPLAY "[" W1 "]"
           STOP RUN.
