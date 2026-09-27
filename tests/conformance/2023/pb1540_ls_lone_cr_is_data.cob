      *> kb/Work PB1540 -- the LINE SEQUENTIAL line delimiter, as READ
      *> sees it (Annex A.1 item 114, docs/CONFORMANCE.md DOC-A.1-114).
      *>   python scripts/spec/cite.py --check 12.4.5.10.3 "Each record
      *>   in a line sequential file is terminated by an
      *>   implementor-defined line delimiter"
      *>                                        -> OK  §12.4.5.10.3 2)
      *>   python scripts/spec/cite.py --check 14.9.30.4 "contains one
      *>   or more characters not in the implementor-defined character
      *>   set for a line sequential file, the I-O status in the read
      *>   file connector is set to '09'"      -> OK  §14.9.30.4 16)
      *> The determination: a READ ends a record at an LF, or at a CR LF
      *> pair, on every host; a lone CR is record DATA (GnuCOBOL's rule,
      *> CLAUDE.md rule 1). The file is written byte by byte through a
      *> one-character record sequential FD (§12.4.5.10.3 GR3):
      *>   A B CR C D LF E F CR LF G H        (no final delimiter)
      *> so the line sequential READs deliver ABxCD (x = the CR, shown
      *> here as "<") with '09' -- CR is outside the line sequential
      *> character set (item 115, §14.9.30.4 GR16) -- then EF with
      *> '00' (its CR LF pair is one delimiter), then GH with '00' (a
      *> final line ends at the end of the file), then the at-end '10'.
      *> Before PB1540 the lone CR ended a record: AB, CD, EF, GH.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1540CR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT BY-OUT ASSIGN TO "pb1540cr.txt"
               ORGANIZATION IS SEQUENTIAL.
           SELECT LS-IN ASSIGN TO "pb1540cr.txt"
               ORGANIZATION IS LINE SEQUENTIAL FILE STATUS IS ST.
       DATA DIVISION.
       FILE SECTION.
       FD BY-OUT.
       01 BY-REC PIC X.
       FD LS-IN.
       01 LS-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01 ST     PIC XX.
       01 WS-BYTES.
          05 FILLER PIC X VALUE "A".
          05 FILLER PIC X VALUE "B".
          05 FILLER PIC X VALUE X"0D".
          05 FILLER PIC X VALUE "C".
          05 FILLER PIC X VALUE "D".
          05 FILLER PIC X VALUE X"0A".
          05 FILLER PIC X VALUE "E".
          05 FILLER PIC X VALUE "F".
          05 FILLER PIC X VALUE X"0D".
          05 FILLER PIC X VALUE X"0A".
          05 FILLER PIC X VALUE "G".
          05 FILLER PIC X VALUE "H".
       01 WS-BYTE-TAB REDEFINES WS-BYTES.
          05 WS-BYTE PIC X OCCURS 12 TIMES.
       01 WS-I   PIC 99.
       01 WS-EOF PIC X VALUE "N".
       01 WS-SHOW PIC X(5).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT BY-OUT
           PERFORM VARYING WS-I FROM 1 BY 1 UNTIL WS-I > 12
               MOVE WS-BYTE(WS-I) TO BY-REC
               WRITE BY-REC
           END-PERFORM
           CLOSE BY-OUT
           OPEN INPUT LS-IN
           PERFORM UNTIL WS-EOF = "Y"
               MOVE SPACES TO LS-REC
               READ LS-IN
                   AT END MOVE "Y" TO WS-EOF
                          DISPLAY "AT-END=" ST
                   NOT AT END
                       PERFORM SHOW-REC
                       DISPLAY "REC=" WS-SHOW " ST=" ST
               END-READ
           END-PERFORM
           CLOSE LS-IN
           STOP RUN.
       SHOW-REC.
           PERFORM VARYING WS-I FROM 1 BY 1 UNTIL WS-I > 5
               IF LS-REC(WS-I:1) = X"0D"
                   MOVE "<" TO WS-SHOW(WS-I:1)
               ELSE
                   MOVE LS-REC(WS-I:1) TO WS-SHOW(WS-I:1)
               END-IF
           END-PERFORM.
