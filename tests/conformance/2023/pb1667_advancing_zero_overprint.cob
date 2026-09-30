      *> kb/Work PB1667 arm 1 + decision R54 -- WRITE ... ADVANCING 0 is
      *> an OVERPRINT, written as a bare carriage return.
      *>   python scripts/spec/cite.py --check 14.9.51.4 "If integer-1
      *>   or the value of the data item referenced by identifier-2 is
      *>   zero, no repositioning of the representation of the printed
      *>   page is performed"                  -> OK  §14.9.51.4 25) c)
      *>   python scripts/spec/cite.py --check 14.9.51.4 "the line is
      *>   presented after the representation of the printed page is
      *>   advanced"                           -> OK  §14.9.51.4 25) f)
      *> DERIVATION. A zero amount repositions nothing, so the line is
      *> presented at the SAME vertical position as the line the device
      *> stands on: an overprint, not a line that continues the open
      *> one. The standard leaves the device representation to the
      *> implementor; the project's precedence (CLAUDE.md rule 1, owner
      *> decision R54) follows GnuCOBOL, whose libcob writes a bare
      *> carriage return for AFTER/BEFORE ADVANCING 0 and no line feed
      *> (docs/CONFORMANCE.md "ADVANCING 0"). The return is written when
      *> a line is open; with none the device is already at a line
      *> start and nothing is written. The defect wrote ABCXYZ on one
      *> line with no return between the two records.
      *> The bytes are read back through a one-character record
      *> sequential FD over the same file. CR shows as "<" and LF as
      *> "/" (the print stream's line end is CR LF on every host).
      *>   A  AFTER 1 "ABC"            CRLF, ABC             line open
      *>      AFTER 0 "XYZ"            CR, XYZ
      *>      AFTER ZN (ZN = 0) "PQR"  CR, PQR  (identifier-2)
      *>      AFTER 2 "END"            CRLF CRLF, END; CLOSE ends the line
      *>      => </ABC<XYZ<PQR</</END</
      *>   B  BEFORE 0 "GHI"           GHI, CR  (record, then the zero)
      *>      AFTER 1 "JKL"            CRLF, JKL; CLOSE ends the line
      *>      => GHI<</JKL</
      *>   C  AFTER 0 "AAA" first      no line open: nothing written
      *>      => AAA</
      *>   D  LINAGE 5: OPEN sets LINAGE-COUNTER to 1 (§13.18.34.4 GR7)
      *>      AFTER 1 "AAA" -> counter 2; AFTER 0 "BBB" -> counter 2,
      *>      and the two records overprint: </AAA<BBB</
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1667ADV.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PA ASSIGN TO "pb1667a.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT PB ASSIGN TO "pb1667b.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT PC ASSIGN TO "pb1667c.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT PD ASSIGN TO "pb1667d.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT BY-IN ASSIGN USING WS-NAME
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD PA.
       01 PA-REC PIC X(3).
       FD PB.
       01 PB-REC PIC X(3).
       FD PC.
       01 PC-REC PIC X(3).
       FD PD LINAGE IS 5 LINES.
       01 PD-REC PIC X(3).
       FD BY-IN.
       01 BY-REC PIC X.
       WORKING-STORAGE SECTION.
       01 WS-NAME  PIC X(12).
       01 WS-EOF   PIC X.
       01 WS-LINE  PIC X(40).
       01 WS-PTR   PIC 99.
       01 WS-CH    PIC X.
       01 ZN       PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PA
           MOVE "ABC" TO PA-REC
           WRITE PA-REC AFTER ADVANCING 1 LINE
           MOVE "XYZ" TO PA-REC
           WRITE PA-REC AFTER ADVANCING 0 LINES
           MOVE "PQR" TO PA-REC
           WRITE PA-REC AFTER ADVANCING ZN LINES
           MOVE "END" TO PA-REC
           WRITE PA-REC AFTER ADVANCING 2 LINES
           CLOSE PA
           MOVE "pb1667a.dat" TO WS-NAME
           PERFORM SHOW-FILE
           DISPLAY "A=" WS-LINE
           OPEN OUTPUT PB
           MOVE "GHI" TO PB-REC
           WRITE PB-REC BEFORE ADVANCING 0 LINES
           MOVE "JKL" TO PB-REC
           WRITE PB-REC AFTER ADVANCING 1 LINE
           CLOSE PB
           MOVE "pb1667b.dat" TO WS-NAME
           PERFORM SHOW-FILE
           DISPLAY "B=" WS-LINE
           OPEN OUTPUT PC
           MOVE "AAA" TO PC-REC
           WRITE PC-REC AFTER ADVANCING 0 LINES
           CLOSE PC
           MOVE "pb1667c.dat" TO WS-NAME
           PERFORM SHOW-FILE
           DISPLAY "C=" WS-LINE
           OPEN OUTPUT PD
           MOVE "AAA" TO PD-REC
           WRITE PD-REC AFTER ADVANCING 1 LINE
           DISPLAY "D1=" LINAGE-COUNTER OF PD
           MOVE "BBB" TO PD-REC
           WRITE PD-REC AFTER ADVANCING 0 LINES
           DISPLAY "D2=" LINAGE-COUNTER OF PD
           CLOSE PD
           MOVE "pb1667d.dat" TO WS-NAME
           PERFORM SHOW-FILE
           DISPLAY "D=" WS-LINE
           STOP RUN.
       SHOW-FILE.
           MOVE SPACES TO WS-LINE
           MOVE 1 TO WS-PTR
           MOVE "N" TO WS-EOF
           OPEN INPUT BY-IN
           PERFORM UNTIL WS-EOF = "Y"
               READ BY-IN
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END PERFORM SHOW-BYTE
               END-READ
           END-PERFORM
           CLOSE BY-IN.
       SHOW-BYTE.
           EVALUATE BY-REC
               WHEN X"0D" MOVE "<" TO WS-CH
               WHEN X"0A" MOVE "/" TO WS-CH
               WHEN OTHER MOVE BY-REC TO WS-CH
           END-EVALUATE
           STRING WS-CH DELIMITED BY SIZE INTO WS-LINE
               WITH POINTER WS-PTR
           END-STRING.
