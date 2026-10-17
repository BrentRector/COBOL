*>--- conformance-case
*> id: pb961_signaling_nan_binary32
*> rules: [GR-14.9.39.4-35, DOC-A.1-176]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 14.9.39.4 GR35 - SET CONTENT ... TO FLOAT-NOT-A-NUMBER-SIGNALING sets the item to "a
      *> canonical representation of a signaling NaN ... for the basic interchange format corresponding to the
      *> usage of identifier-14"; 14.9.25.4 GR6 c) - a MOVE between items of the same usage transfers the data
      *> "without change" (NOTE 3: NaN representations are preserved), endianness aside (kb/Work PB961).
      *> The payload is the documented Annex A.1 item 176 value (docs/CONFORMANCE.md DOC-A.1-176): binary32
      *> signaling = 0x7F800001 = 2139095041, with SIGN NEGATIVE 0xFF800001 = 4286578689; binary64 signaling =
      *> 0x7FF0000000000001 = 9218868437227405313; binary32 quiet = 0x7FC00000 = 2143289344. The bits are read
      *> back through a BINARY-LONG / BINARY-DOUBLE UNSIGNED REDEFINES of the float's own bytes, and every copy
      *> is also asked 8.8.4.4.4 GR3 k) FLOAT-NOT-A-NUMBER-SIGNALING, so no line can pass on a QUIET NaN:
      *> a binary32 -> binary64 widening sets the quiet bit (0x7FC00001 = 2143289345), which is the defect.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB961SNAN32.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S   USAGE FLOAT-BINARY-32.
       01 T   USAGE FLOAT-BINARY-32.
       01 G1.
          05 W1 USAGE FLOAT-BINARY-32.
       01 G1X REDEFINES G1 USAGE BINARY-LONG UNSIGNED.
       01 G2.
          05 W2 USAGE FLOAT-BINARY-32.
       01 G2X REDEFINES G2 USAGE BINARY-LONG UNSIGNED.
       01 G3.
          05 W3 USAGE FLOAT-BINARY-32.
       01 G3X REDEFINES G3 USAGE BINARY-LONG UNSIGNED.
       01 G4.
          05 W4 USAGE FLOAT-BINARY-64.
       01 G4X REDEFINES G4 USAGE BINARY-DOUBLE UNSIGNED.
       01 L   USAGE FLOAT-BINARY-32 HIGH-ORDER-RIGHT.
       01 GA.
          05 FA USAGE FLOAT-BINARY-32.
          05 NA PIC X(2) VALUE "AB".
       01 GB.
          05 FB USAGE FLOAT-BINARY-32.
          05 NB PIC X(2).
       PROCEDURE DIVISION.
       MAIN-PARA.
      *>   GR35 into a typed item, then a same-usage MOVE (GR6 c)) into a typed item and into a window.
           SET CONTENT OF S TO FLOAT-NOT-A-NUMBER-SIGNALING
           IF S IS FLOAT-NOT-A-NUMBER-SIGNALING
              DISPLAY "S-SNAN=Y" ELSE DISPLAY "S-SNAN=N" END-IF
           MOVE S TO T
           IF T IS FLOAT-NOT-A-NUMBER-SIGNALING
              DISPLAY "T-SNAN=Y" ELSE DISPLAY "T-SNAN=N" END-IF
           MOVE S TO W1
           DISPLAY "MOVE-TO-WINDOW=" G1X
      *>   GR35 straight into a window, positive then negative sign.
           SET CONTENT OF W2 TO FLOAT-NOT-A-NUMBER-SIGNALING
           DISPLAY "SET-WINDOW=" G2X
           SET CONTENT OF W3 TO FLOAT-NOT-A-NUMBER-SIGNALING
               SIGN NEGATIVE
           DISPLAY "SET-WINDOW-NEG=" G3X
      *>   A window back into a typed item, and a whole-group copy.
           MOVE W2 TO T
           IF T IS FLOAT-NOT-A-NUMBER-SIGNALING
              DISPLAY "WINDOW-TO-T-SNAN=Y" ELSE DISPLAY "WINDOW-TO-T-SNAN=N" END-IF
           MOVE G2 TO G3
           DISPLAY "GROUP-MOVE=" G3X
      *>   A group move of a record whose float member is not windowed (the group image codec's lanes).
           MOVE S TO FA
           MOVE GA TO GB
           IF FB IS FLOAT-NOT-A-NUMBER-SIGNALING
              DISPLAY "RECORD-MOVE-SNAN=Y" ELSE DISPLAY "RECORD-MOVE-SNAN=N" END-IF
      *>   GR6 c)'s endianness clause: a different byte order, no other change.
           MOVE S TO L
           IF L IS FLOAT-NOT-A-NUMBER-SIGNALING
              DISPLAY "L-SNAN=Y" ELSE DISPLAY "L-SNAN=N" END-IF
      *>   GR34 is still quiet, and the binary64 lane is unchanged.
           SET CONTENT OF W1 TO FLOAT-NOT-A-NUMBER
           DISPLAY "QUIET-WINDOW=" G1X
           SET CONTENT OF W4 TO FLOAT-NOT-A-NUMBER-SIGNALING
           DISPLAY "SET-WINDOW-64=" G4X
           STOP RUN.
