      *> kb/Work PB1027 -- a WRITE with no ADVANCING phrase acts as
      *> AFTER ADVANCING 1 LINE in EVERY state of the device, on a
      *> LINE SEQUENTIAL file too (the second arm of the write
      *> dispatch: 2023/pb964_ls_plain_write_after_after covers only
      *> the state after an AFTER write).
      *>   python scripts/spec/cite.py --check 14.9.51.4 "If the
      *>   ADVANCING phrase is not used, automatic advancing shall be
      *>   provided by the implementor to act as if the user has
      *>   specified AFTER ADVANCING 1 LINE"      -> OK  §14.9.51.4 25)
      *>   python scripts/spec/cite.py --check 14.9.51.4 "If the
      *>   physical file does not support vertical positioning, the
      *>   ADVANCING and END-OF-PAGE phrases are ignored"
      *>                                          -> OK  §14.9.51.4 25)
      *>   python scripts/spec/cite.py --check 14.9.51.4 "If the BEFORE
      *>   phrase is used, the line is presented before the
      *>   representation of the printed page is advanced"
      *>                                          -> OK  §14.9.51.4 25) e)
      *> The 85 twin (85/pb1027_plain_write_is_after_advancing_85)
      *> derives the four expected strings for a record sequential
      *> file; a line sequential file ends its lines with the host
      *> newline, which the reader folds away (a CR is dropped; the LF
      *> ends the line), so the same strings hold here.
      *>   A  "BEFORE 1" then plain, then "AFTER 1", then plain:
      *>        AAA LF | LF BBB | LF CCC | LF DDD | LF
      *>        => AAA//BBB/CCC/DDD/
      *>   B  the same with every plain WRITE spelled AFTER ADVANCING
      *>      1 LINE. => AAA//BBB/CCC/DDD/
      *>   C  LINAGE IS 3 LINES TOP 1 BOTTOM 1, three plain WRITEs:
      *>      seven physical lines "" "" AAA BBB "" "" CCC (top margin,
      *>      the advance past body line 1, AAA on body line 2, BBB on
      *>      body line 3, bottom margin, next top margin, CCC on body
      *>      line 1 of page 2 -- AFTER's overflow presents ON that
      *>      line, §14.9.51.4 GR26 a)). => //AAA/BBB///CCC/
      *>   D  the same with every WRITE spelled AFTER ADVANCING 1 LINE.
      *>      => //AAA/BBB///CCC/
      *>   E  A file with NO LINAGE clause that no ADVANCING phrase was
      *>      ever written to does not support vertical positioning
      *>      (Annex A.3 item 37, the implementor's determination in
      *>      docs/CONFORMANCE.md section 4), so GR25's first
      *>      sentence leaves nothing to position: each plain WRITE is
      *>      the record and its line end, with no leading empty line.
      *>      => AAA/BBB/
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1027LS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PA ASSIGN TO "pb1027ls-a.dat"
               ORGANIZATION IS LINE SEQUENTIAL.
           SELECT PB ASSIGN TO "pb1027ls-b.dat"
               ORGANIZATION IS LINE SEQUENTIAL.
           SELECT PC ASSIGN TO "pb1027ls-c.dat"
               ORGANIZATION IS LINE SEQUENTIAL.
           SELECT PD ASSIGN TO "pb1027ls-d.dat"
               ORGANIZATION IS LINE SEQUENTIAL.
           SELECT PE ASSIGN TO "pb1027ls-e.dat"
               ORGANIZATION IS LINE SEQUENTIAL.
           SELECT BY-IN ASSIGN USING WS-NAME
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
       FD PE.
       01 PE-REC PIC X(3).
       FD BY-IN.
       01 BY-REC PIC X.
       WORKING-STORAGE SECTION.
       01 WS-NAME  PIC X(14).
       01 WS-EOF   PIC X.
       01 WS-LINE  PIC X(40).
       01 WS-PTR   PIC 99.
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
           OPEN OUTPUT PE
           MOVE "AAA" TO PE-REC
           WRITE PE-REC
           MOVE "BBB" TO PE-REC
           WRITE PE-REC
           CLOSE PE
           MOVE "pb1027ls-a.dat" TO WS-NAME
           PERFORM SHOW-FILE
           DISPLAY "A=" WS-LINE
           MOVE "pb1027ls-b.dat" TO WS-NAME
           PERFORM SHOW-FILE
           DISPLAY "B=" WS-LINE
           MOVE "pb1027ls-c.dat" TO WS-NAME
           PERFORM SHOW-FILE
           DISPLAY "C=" WS-LINE
           MOVE "pb1027ls-d.dat" TO WS-NAME
           PERFORM SHOW-FILE
           DISPLAY "D=" WS-LINE
           MOVE "pb1027ls-e.dat" TO WS-NAME
           PERFORM SHOW-FILE
           DISPLAY "E=" WS-LINE
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
               WHEN X"0D" CONTINUE
               WHEN X"0A" MOVE "/" TO WS-CH
                          PERFORM APPEND-CH
               WHEN OTHER MOVE BY-REC TO WS-CH
                          PERFORM APPEND-CH
           END-EVALUATE.
       APPEND-CH.
           STRING WS-CH DELIMITED BY SIZE INTO WS-LINE
               WITH POINTER WS-PTR
           END-STRING.
