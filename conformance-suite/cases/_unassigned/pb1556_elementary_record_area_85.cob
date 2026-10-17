      *>--- conformance-case
      *> id: pb1556_elementary_record_area_85
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1556 - a record description entry that is ONE
      *> elementary numeric item, of every 85 usage, through every
      *> record-area transfer; and the GROUP MOVE into an elementary
      *> numeric receiver, which shares the one storage-area decode.
      *> ISO 14.9.30.4 GR13 c): "the record is made available in the
      *>   record area and any implicit move resulting from the presence
      *>   of an INTO phrase is executed"; GR4 b): the INTO move is "from
      *>   the record area ... according to the rules for the MOVE".
      *> ISO 14.9.34.4 GR3 (RETURN): the record is "made available in the
      *>   record area"; GR5 b) the same INTO move.
      *> ISO 14.9.51.4 GR5 / 14.9.35.4 GR7 / 14.9.32.4 GR4: WRITE /
      *>   REWRITE / RELEASE FROM = MOVE identifier TO record, then the
      *>   statement without FROM.
      *> ISO 14.9.51.4 GR4: after a WRITE the record is "no longer
      *>   available in the record area" - so each value shown after a
      *>   READ / RETURN came back through the file, not the area.
      *> ISO 14.9.25.4 GR4: a group move is an alphanumeric move with
      *>   "no conversion of data from one form of internal
      *>   representation to another" - the receiver's BINARY / PACKED
      *>   bytes are the sender's bytes, so its value is the sender's.
      *> Values are shown through PIC -9(5) (13.18.40.5 ER5, Table 8:
      *> fixed '-' prints a space when the value is positive or zero).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1556E85.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "PB1556A.DAT".
           SELECT F2 ASSIGN TO "PB1556B.DAT".
           SELECT F3 ASSIGN TO "PB1556C.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS RK.
           SELECT S4 ASSIGN TO "PB1556D.TMP".
           SELECT S5 ASSIGN TO "PB1556E.TMP".
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1 PIC 9.
       FD F2.
       01 R2 PIC S9(4) BINARY.
       FD F3.
       01 R3 PIC S9(5) PACKED-DECIMAL.
       SD S4.
       01 R4 PIC 9.
       SD S5.
       01 R5 PIC S9(5) PACKED-DECIMAL.
       WORKING-STORAGE SECTION.
       01 RK PIC 9(4).
       01 W2 PIC S9(4) BINARY.
       01 W3 PIC S9(5) PACKED-DECIMAL.
       01 W5 PIC S9(5) PACKED-DECIMAL.
       01 E1 PIC -9(5).
       01 E2 PIC -9(5).
       01 EOF-SW PIC 9.
       01 GB.
          05 GB-V PIC S9(4) BINARY VALUE -7.
       01 GP.
          05 GP-V PIC S9(5) PACKED-DECIMAL VALUE 321.
       01 B PIC S9(4) BINARY.
       01 P PIC S9(5) PACKED-DECIMAL.
       PROCEDURE DIVISION.
       MAIN-SEC SECTION.
       M-1.
      *> FD, elementary unsigned DISPLAY record: WRITE, then READ.
           OPEN OUTPUT F1.
           MOVE 7 TO R1.
           WRITE R1.
           CLOSE F1.
           OPEN INPUT F1.
           READ F1 AT END DISPLAY "F1 AT END".
           DISPLAY "F1 " R1.
           CLOSE F1.
      *> FD, elementary BINARY record: WRITE FROM, READ, READ INTO.
           OPEN OUTPUT F2.
           MOVE -1234 TO W2.
           WRITE R2 FROM W2.
           MOVE 815 TO W2.
           WRITE R2 FROM W2.
           CLOSE F2.
           MOVE 0 TO W2.
           OPEN INPUT F2.
           READ F2 AT END DISPLAY "F2 AT END".
           MOVE R2 TO E1.
           DISPLAY "F2 " E1.
           READ F2 INTO W2 AT END DISPLAY "F2 AT END".
           MOVE R2 TO E1.
           MOVE W2 TO E2.
           DISPLAY "F2 " E1 " " E2.
           CLOSE F2.
      *> Relative FD, elementary PACKED-DECIMAL record: WRITE FROM,
      *> READ, REWRITE FROM, READ INTO.
           OPEN OUTPUT F3.
           MOVE 1 TO RK.
           MOVE -54321 TO W3.
           WRITE R3 FROM W3 INVALID KEY DISPLAY "F3 INVALID".
           CLOSE F3.
           OPEN I-O F3.
           MOVE 1 TO RK.
           READ F3 INVALID KEY DISPLAY "F3 INVALID".
           MOVE R3 TO E1.
           DISPLAY "F3 " E1.
           MOVE 12345 TO W3.
           REWRITE R3 FROM W3 INVALID KEY DISPLAY "F3 INVALID".
           MOVE 0 TO W3.
           READ F3 INTO W3 INVALID KEY DISPLAY "F3 INVALID".
           MOVE R3 TO E1.
           MOVE W3 TO E2.
           DISPLAY "F3 " E1 " " E2.
           CLOSE F3.
      *> SD, elementary DISPLAY record: RELEASE, RETURN.
           SORT S4 ON DESCENDING KEY R4
               INPUT PROCEDURE IS IN-4
               OUTPUT PROCEDURE IS OUT-4.
      *> SD, elementary PACKED-DECIMAL record: RELEASE FROM,
      *> RETURN INTO.
           SORT S5 ON ASCENDING KEY R5
               INPUT PROCEDURE IS IN-5
               OUTPUT PROCEDURE IS OUT-5.
      *> Group move into elementary BINARY and PACKED-DECIMAL items.
           MOVE GB TO B.
           MOVE B TO E1.
           DISPLAY "GB " E1.
           MOVE GP TO P.
           MOVE P TO E1.
           DISPLAY "GP " E1.
           STOP RUN.
       IN-4 SECTION.
       I4-1.
           MOVE 1 TO R4.
           RELEASE R4.
           MOVE 3 TO R4.
           RELEASE R4.
           MOVE 2 TO R4.
           RELEASE R4.
       OUT-4 SECTION.
       O4-1.
           MOVE 0 TO EOF-SW.
           PERFORM UNTIL EOF-SW = 1
               RETURN S4
                   AT END MOVE 1 TO EOF-SW
                   NOT AT END DISPLAY "S4 " R4
               END-RETURN
           END-PERFORM.
       IN-5 SECTION.
       I5-1.
           MOVE -7 TO W5.
           RELEASE R5 FROM W5.
           MOVE 300 TO W5.
           RELEASE R5 FROM W5.
           MOVE 5 TO W5.
           RELEASE R5 FROM W5.
       OUT-5 SECTION.
       O5-1.
           MOVE 0 TO EOF-SW.
           PERFORM UNTIL EOF-SW = 1
               MOVE 0 TO W5
               RETURN S5 INTO W5
                   AT END MOVE 1 TO EOF-SW
                   NOT AT END
                       MOVE R5 TO E1
                       MOVE W5 TO E2
                       DISPLAY "S5 " E1 " " E2
               END-RETURN
           END-PERFORM.
