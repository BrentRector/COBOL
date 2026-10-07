      *> kb/Work PB1291 - ISO 13.4.6.3 SR4: "A record description entry associated with file-name-1 shall not
      *> be specified in an input-output statement other than following the word FROM or the word INTO."
      *> cite.py: OK 13.4.6.3 4). THE PERMITTED ARM: an SD record after INTO (READ) and after FROM (WRITE)
      *> binds and runs. The forbidden arm is the negative pb1291-sd-record-advancing. Expected values follow
      *> from the SORT (records are returned in ascending key order) and the implicit MOVEs of the FROM and
      *> INTO phrases.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W1031GPB1291POS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OUT1 ASSIGN TO "w1031gpb1291.dat".
           SELECT SRT ASSIGN TO "w1031gpb1291s.dat".
       DATA DIVISION.
       FILE SECTION.
       FD OUT1.
       01 OUT-REC PIC X(4).
       SD SRT.
       01 SRT-REC.
          05 SRT-K PIC X(2).
          05 SRT-D PIC X(2).
       WORKING-STORAGE SECTION.
       01 WS-REC PIC X(4).
       01 WS-EOF PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-PARA SECTION.
       M-1.
           SORT SRT ON ASCENDING KEY SRT-K
               INPUT PROCEDURE IS IN-PROC
               OUTPUT PROCEDURE IS OUT-PROC.
           OPEN INPUT OUT1.
           MOVE "N" TO WS-EOF.
           PERFORM UNTIL WS-EOF = "Y"
               READ OUT1 INTO SRT-REC
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END DISPLAY "READ INTO " SRT-REC
               END-READ
           END-PERFORM.
           CLOSE OUT1.
           STOP RUN.
       IN-PROC SECTION.
       I-1.
           MOVE "BBxx" TO SRT-REC.
           RELEASE SRT-REC.
           MOVE "AAyy" TO SRT-REC.
           RELEASE SRT-REC.
       OUT-PROC SECTION.
       O-1.
           OPEN OUTPUT OUT1.
           PERFORM UNTIL WS-EOF = "Y"
               RETURN SRT INTO WS-REC
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END
                       DISPLAY "RETURNED " WS-REC
                       WRITE OUT-REC FROM SRT-REC
               END-RETURN
           END-PERFORM.
           CLOSE OUT1.
