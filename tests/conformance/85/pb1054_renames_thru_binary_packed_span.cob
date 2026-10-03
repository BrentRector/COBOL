      *> kb/Work PB1054 - a RENAMES ... THROUGH alias over a span holding BINARY and
      *> PACKED-DECIMAL members. ISO 13.18.45.4 GR2: "data-name-1 defines an
      *> alphanumeric group item that includes all elementary items starting with
      *> data-name-2 ... and concluding with data-name-3", so RN is the 7-byte
      *> storage area RA RB RP RC: two characters, the 2 bytes of the binary item,
      *> the 2 bytes of the packed item, one character. Sending RN moves those
      *> bytes; receiving into RN stores bytes that each renamed item then reads
      *> as its own usage (13.18.45.3 SR8 bars only class object, message-tag
      *> and pointer members, variable-length items and occurs-depending tables).
      *> R2 has the same layout, so a byte-for-byte round trip through SAVE shows
      *> the alias carries the binary and packed bytes in both directions.
      *> MEASURED BEFORE THE FIX: the span was DEFERRED - a COBOLNET1756 warning
      *> and NotImplementedCobolFeatureException at run time - for every member
      *> that was not usage display or national.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1054SPAN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R.
          05 RA PIC X(2) VALUE "AB".
          05 RB PIC 9(4) BINARY VALUE 12.
          05 RP PIC S9(3) PACKED-DECIMAL VALUE -5.
          05 RC PIC X VALUE "C".
       66 RN RENAMES RA THRU RC.
       66 RM RENAMES RA THRU RB.
       01 R2.
          05 R2A PIC X(2).
          05 R2B PIC 9(4) BINARY.
          05 R2P PIC S9(3) PACKED-DECIMAL.
          05 R2C PIC X.
       01 SAVE PIC X(7).
       01 OK1 PIC X.
       PROCEDURE DIVISION.
       MAIN.
      *> 1 - sending: the alias's bytes land in R2 and read back by usage.
           MOVE RN TO SAVE.
           MOVE SAVE TO R2.
           DISPLAY "SENT=" R2A " " R2B " " R2P " " R2C.
           IF RN = SAVE DISPLAY "EQUAL" ELSE DISPLAY "DIFFERENT".
      *> 2 - receiving: bytes stored through the alias read back by usage.
           MOVE "XY" TO R2A.
           MOVE 345 TO R2B.
           MOVE -77 TO R2P.
           MOVE "Z" TO R2C.
           MOVE R2 TO SAVE.
           MOVE SAVE TO RN.
           DISPLAY "RECEIVED=" RA " " RB " " RP " " RC.
      *> 3 - the alias as one receiver of several (kb/Work PB70's receiver
      *> list - it used to be the negative that pinned this span as deferred):
      *> "X" fills the 4-byte group RM with "X" and three spaces, so RB's two
      *> binary bytes are X"2020", the value 8224.
           MOVE "X" TO OK1 RM.
           DISPLAY "LIST=" OK1 " " RA " " RB.
           STOP RUN.
