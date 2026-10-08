      *> kb/Work PB1667 arm 2 + decision R54 -- a report file that stays
      *> open after a TERMINATE stands on that run's last line, and the
      *> first line of the NEXT run (a re-INITIATE of the report)
      *> belongs on the line BELOW it, never on it.
      *>   python scripts/spec/cite.py --check 14.9.21.4 "LINE-COUNTER
      *>   is set to zero"                  -> OK  §14.9.21.4 1) b)
      *>   python scripts/spec/cite.py --check 14.9.46.4 "The TERMINATE
      *>   statement does not close the file associated with
      *>   report-name-1"                   -> OK  §14.9.46.4 6)
      *>   python scripts/spec/cite.py --check 13.18.35.4 "no lines or
      *>   groups of lines overlap each other, except that the non-space
      *>   characters of a relative line specified with an integer-2 of
      *>   zero will overwrite"             -> OK  §13.18.35.4 3)
      *>   python scripts/spec/cite.py --check 14.9.51.4 "If integer-1
      *>   or the value of the data item referenced by identifier-2 is
      *>   zero, no repositioning of the representation of the printed
      *>   page is performed"               -> OK  §14.9.51.4 25) c)
      *> DERIVATION. Each run prints its detail on its page's line 1 and
      *> its FINAL footing on line 2 (LINE PLUS 1, LINE-COUNTER starting
      *> at zero - §14.9.21.4 1) b)). TERMINATE leaves the file open
      *> (§14.9.46.4 6)), so the device stands on the footing line. The
      *> only overlap §13.18.35.4 3) allows is a relative line with
      *> integer-2 of zero inside a report group, so a run's FIRST line
      *> is not one: a zero advance would overprint it on the last line
      *> of the run before (§14.9.51.4 25) c): a zero advance is an
      *> overprint), which the engine did by reading INITIATE's empty
      *> page model as an empty DEVICE. The file is read back through a
      *> one-character record sequential FD with no CLOSE of the report
      *> file between the two runs. A lone CR shows as "<" (an overprint:
      *> the defect) and a line end as "/": the line end of a report file
      *> is the host newline (CR LF on Windows, LF on Linux and macOS:
      *> docs/CONFORMANCE.md A.1 item 159, kb/Work PB1664), so a CR that
      *> is followed by LF is folded into the one "/" and the golden reads
      *> the same on every host:
      *>   run 1   60, S=60
      *>   run 2   05, S=05   (WS-N reset)
      *> => 60/S=60/05/S=05/
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1667REP.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRS ASSIGN TO "pb1667r.txt".
           SELECT CHS ASSIGN TO "pb1667r.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRS REPORT IS R-S.
       FD  CHS.
       01  CHS-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-N    PIC 99    VALUE 60.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-CH   PIC X.
       01  WS-LINE PIC X(60) VALUE SPACES.
       01  WS-PTR  PIC 99    VALUE 1.
       01  WS-CR   PIC X     VALUE "N".
       REPORT SECTION.
       RD  R-S CONTROL IS FINAL.
       01  DET-S TYPE DE LINE PLUS 1.
           02  COLUMN 1 PIC 99 SOURCE WS-N.
       01  TYPE CF FINAL LINE PLUS 1.
           02  COLUMN 1 PIC X(2) VALUE "S=".
           02  COLUMN 3 PIC 99 SUM WS-N.
       PROCEDURE DIVISION.
       M1.
           OPEN OUTPUT PRS.
           INITIATE R-S.
           GENERATE DET-S.
           TERMINATE R-S.
           MOVE 5 TO WS-N.
           INITIATE R-S.
           GENERATE DET-S.
           TERMINATE R-S.
           CLOSE PRS.
           OPEN INPUT CHS.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHS
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END PERFORM SHOW-BYTE
               END-READ
           END-PERFORM.
           IF WS-CR = "Y"
               MOVE "<" TO WS-CH
               PERFORM PUT-CH
           END-IF
           CLOSE CHS.
           DISPLAY "BYTES=" WS-LINE.
           STOP RUN.
       SHOW-BYTE.
           IF WS-CR = "Y"
               MOVE "N" TO WS-CR
               IF CHS-REC NOT = X"0A"
                   MOVE "<" TO WS-CH
                   PERFORM PUT-CH
               END-IF
           END-IF
           EVALUATE CHS-REC
               WHEN X"0D" MOVE "Y" TO WS-CR
               WHEN X"0A" MOVE "/" TO WS-CH
                          PERFORM PUT-CH
               WHEN OTHER MOVE CHS-REC TO WS-CH
                          PERFORM PUT-CH
           END-EVALUATE.
       PUT-CH.
           STRING WS-CH DELIMITED BY SIZE INTO WS-LINE
               WITH POINTER WS-PTR
           END-STRING.
