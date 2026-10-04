      *> kb/Work PB1261 - the OCCURS DEPENDING ON objects ISO 13.18.38.3 ADMITS, beside the
      *> three negatives pb1261-depending-*. SR18: under the GLOBAL record G the counter NG is a
      *> global name (8.4.6.2.2 - its own GLOBAL clause). SR21: under the EXTERNAL record E the
      *> counter NE possesses the external attribute (8.6.3). SR20 over the FD's ONE area (13.18.33.4
      *> GR3 - the 01s are implicit redefinitions of one area): N1 in R2 sits at byte 1, before T's
      *> first byte (2); N3 in R3 sits at byte 7, past R4's last byte (5) - neither lies in its
      *> table's range, so both are legal. Expected values (13.18.38.4 GR8 - the group's size is set
      *> by the counter's current value): G holds NG = 3 occurrences, so MOVE "ABCDE" TO G keeps
      *> ABC; E holds NE = 2, so MOVE "XYZ" TO E keeps XY. Fails if any admitted counter is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1261OK.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "PB1261OK.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R1.
           05 H PIC X.
           05 T PIC X OCCURS 1 TO 5 DEPENDING ON N1.
       01 R2.
           05 N1 PIC 9.
       01 R3.
           05 FILLER PIC X(6).
           05 N3 PIC 9.
       01 R4.
           05 T4 PIC X OCCURS 1 TO 5 DEPENDING ON N3.
       WORKING-STORAGE SECTION.
       01 NG PIC 9 VALUE 3 GLOBAL.
       01 G GLOBAL.
           05 TG PIC X OCCURS 1 TO 5 DEPENDING ON NG.
       01 NE PIC 9 EXTERNAL.
       01 E EXTERNAL.
           05 TE PIC X OCCURS 1 TO 5 DEPENDING ON NE.
       PROCEDURE DIVISION.
           MOVE "ABCDE" TO G
           MOVE 2 TO NE
           MOVE "XYZ" TO E
           DISPLAY "G=" G " E=" E
           STOP RUN.
