      *> kb/Work PB1027 -- a WRITE with no ADVANCING phrase acts as
      *> AFTER ADVANCING 1 LINE in EVERY state of the device, not only
      *> after an AFTER write.
      *>   python scripts/spec/cite.py --check 14.9.51.4 "If the
      *>   ADVANCING phrase is not used, automatic advancing shall be
      *>   provided by the implementor to act as if the user has
      *>   specified AFTER ADVANCING 1 LINE"      -> OK  §14.9.51.4 25)
      *>   python scripts/spec/cite.py --check 14.9.51.4 "If the BEFORE
      *>   phrase is used, the line is presented before the
      *>   representation of the printed page is advanced"
      *>                                          -> OK  §14.9.51.4 25) e)
      *>   python scripts/spec/cite.py --check 14.9.51.4 "If the AFTER
      *>   phrase is specified or implied, the device is repositioned to
      *>   the first line that may be written on the next logical page"
      *>                                          -> OK  §14.9.51.4 26)
      *>   python scripts/spec/cite.py --check 13.18.34.4 "When the
      *>   ADVANCING phrase of the WRITE statement is not specified, the
      *>   LINAGE-COUNTER is incremented by the value one"
      *>                                          -> OK  §13.18.34.4 7) c) 3
      *> Four record sequential files, each written twice over: once
      *> with the plain WRITE and once with the phrase spelled out,
      *> and the two must hold the same bytes.
      *>
      *>   A  no LINAGE, "BEFORE 1" then plain: BEFORE presents AAA and
      *>      then advances (GR25 e), ending its line. The plain WRITE
      *>      is AFTER 1 (GR25): it advances AGAIN (one empty line),
      *>      then presents BBB (GR25 f) on a line it leaves open. The
      *>      next AFTER 1 ends that line, CCC follows, and the plain
      *>      WRITE after it ends CCC's line and presents DDD.
      *>      CLOSE ends the open line.
      *>        AAA LF | LF BBB | LF CCC | LF DDD | LF
      *>        => AAA//BBB/CCC/DDD/       (a line end shows as "/")
      *>      This is a print stream, not a LINAGE file, so its line
      *>      end is LF on every host (DOC-A.1-146 (c'), kb/Work PB1664).
      *>   B  the same, with every plain WRITE spelled AFTER ADVANCING
      *>      1 LINE. => AAA//BBB/CCC/DDD/
      *>
      *>   C  LINAGE IS 3 LINES, TOP 1, BOTTOM 1: the logical page is
      *>      1 + 3 + 1 = 5 lines (GR1), physical lines 1-5, the next
      *>      page 6-10. OPEN sets LINAGE-COUNTER to 1 (GR7 d): the
      *>      device is on body line 1. Three plain WRITEs, AAA BBB CCC:
      *>        AAA: AFTER 1, counter 1 -> 2: top margin (line 1), one
      *>             advance past body line 1 (line 2), AAA on body
      *>             line 2 = physical line 3.
      *>        BBB: counter 3 = the page size, no overflow (PB686):
      *>             BBB on body line 3 = physical line 4.
      *>        CCC: counter 4 would pass the page size: page overflow
      *>             (GR26 a) with the AFTER phrase IMPLIED -- the device
      *>             is repositioned to the first line that may be
      *>             written on the next page = physical line 7 (line 5
      *>             is the bottom margin, line 6 the next top margin)
      *>             and CCC is presented ON it; the counter is 1.
      *>      The file is seven physical lines, each ended: "" "" AAA
      *>      BBB "" "" CCC (lines 5 and 6 are the bottom margin and the
      *>      next top margin, GR4/GR5 and GR8's contiguous pages).
      *>        => //AAA/BBB///CCC/
      *>      The line end of a LINAGE file is the host newline (CR LF
      *>      on Windows, LF on Linux), so the reader below drops a CR:
      *>      it is the LF that ends a line on every host.
      *>   D  the same with every WRITE spelled AFTER ADVANCING 1 LINE.
      *>        => //AAA/BBB///CCC/
      *> The bytes are read back through a ONE-character record
      *> sequential record, so a welded or misplaced line shows.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1027A.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0D SYM-X0A
               ARE 14 11.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PA ASSIGN TO "pb1027a-a.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT PB ASSIGN TO "pb1027a-b.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT PC ASSIGN TO "pb1027a-c.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT PD ASSIGN TO "pb1027a-d.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT QA ASSIGN TO "pb1027a-a.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT QB ASSIGN TO "pb1027a-b.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT QC ASSIGN TO "pb1027a-c.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT QD ASSIGN TO "pb1027a-d.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD PA.
       01 PA-REC PIC X(3).
       FD PB.
       01 PB-REC PIC X(3).
       FD PC LINAGE IS 3 LINES LINES AT TOP 1 LINES AT BOTTOM 1.
       01 PC-REC PIC X(3).
       FD PD LINAGE IS 3 LINES LINES AT TOP 1 LINES AT BOTTOM 1.
       01 PD-REC PIC X(3).
       FD QA.
       01 QA-REC PIC X.
       FD QB.
       01 QB-REC PIC X.
       FD QC.
       01 QC-REC PIC X.
       FD QD.
       01 QD-REC PIC X.
       WORKING-STORAGE SECTION.
       01 WS-EOF   PIC X.
       01 WS-LINE  PIC X(40).
       01 WS-PTR   PIC 99.
       01 WS-BYTE  PIC X.
       01 WS-CH    PIC X.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PA
           MOVE "AAA" TO PA-REC
           WRITE PA-REC BEFORE ADVANCING 1 LINE
           MOVE "BBB" TO PA-REC
           WRITE PA-REC
           MOVE "CCC" TO PA-REC
           WRITE PA-REC AFTER ADVANCING 1 LINE
           MOVE "DDD" TO PA-REC
           WRITE PA-REC
           CLOSE PA
           OPEN OUTPUT PB
           MOVE "AAA" TO PB-REC
           WRITE PB-REC BEFORE ADVANCING 1 LINE
           MOVE "BBB" TO PB-REC
           WRITE PB-REC AFTER ADVANCING 1 LINE
           MOVE "CCC" TO PB-REC
           WRITE PB-REC AFTER ADVANCING 1 LINE
           MOVE "DDD" TO PB-REC
           WRITE PB-REC AFTER ADVANCING 1 LINE
           CLOSE PB
           OPEN OUTPUT PC
           MOVE "AAA" TO PC-REC
           WRITE PC-REC
           MOVE "BBB" TO PC-REC
           WRITE PC-REC
           MOVE "CCC" TO PC-REC
           WRITE PC-REC
           CLOSE PC
           OPEN OUTPUT PD
           MOVE "AAA" TO PD-REC
           WRITE PD-REC AFTER ADVANCING 1 LINE
           MOVE "BBB" TO PD-REC
           WRITE PD-REC AFTER ADVANCING 1 LINE
           MOVE "CCC" TO PD-REC
           WRITE PD-REC AFTER ADVANCING 1 LINE
           CLOSE PD
           PERFORM SHOW-A
           DISPLAY "A=" WS-LINE
           PERFORM SHOW-B
           DISPLAY "B=" WS-LINE
           PERFORM SHOW-C
           DISPLAY "C=" WS-LINE
           PERFORM SHOW-D
           DISPLAY "D=" WS-LINE
           STOP RUN.
       SHOW-A.
           PERFORM START-LINE
           OPEN INPUT QA
           PERFORM UNTIL WS-EOF = "Y"
               READ QA
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END
                       MOVE QA-REC TO WS-BYTE
                       PERFORM SHOW-BYTE
               END-READ
           END-PERFORM
           CLOSE QA.
       SHOW-B.
           PERFORM START-LINE
           OPEN INPUT QB
           PERFORM UNTIL WS-EOF = "Y"
               READ QB
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END
                       MOVE QB-REC TO WS-BYTE
                       PERFORM SHOW-BYTE
               END-READ
           END-PERFORM
           CLOSE QB.
       SHOW-C.
           PERFORM START-LINE
           OPEN INPUT QC
           PERFORM UNTIL WS-EOF = "Y"
               READ QC
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END
                       MOVE QC-REC TO WS-BYTE
                       PERFORM SHOW-BYTE
               END-READ
           END-PERFORM
           CLOSE QC.
       SHOW-D.
           PERFORM START-LINE
           OPEN INPUT QD
           PERFORM UNTIL WS-EOF = "Y"
               READ QD
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END
                       MOVE QD-REC TO WS-BYTE
                       PERFORM SHOW-BYTE
               END-READ
           END-PERFORM
           CLOSE QD.
       START-LINE.
           MOVE SPACES TO WS-LINE
           MOVE 1 TO WS-PTR
           MOVE "N" TO WS-EOF.
       SHOW-BYTE.
           EVALUATE WS-BYTE
               WHEN SYM-X0D CONTINUE
               WHEN SYM-X0A MOVE "/" TO WS-CH
                            PERFORM APPEND-CH
               WHEN OTHER MOVE WS-BYTE TO WS-CH
                          PERFORM APPEND-CH
           END-EVALUATE.
       APPEND-CH.
           STRING WS-CH DELIMITED BY SIZE INTO WS-LINE
               WITH POINTER WS-PTR
           END-STRING.
