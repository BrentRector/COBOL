      *> kb/Work PB964 -- a WRITE with no ADVANCING phrase, after a
      *> WRITE ... AFTER ADVANCING, on a LINE SEQUENTIAL file.
      *>   python scripts/spec/cite.py --check 14.9.51.4 "If the
      *>   ADVANCING phrase is not used, automatic advancing shall be
      *>   provided by the implementor to act as if the user has
      *>   specified AFTER ADVANCING 1 LINE."    -> OK  §14.9.51.4 25)
      *>   python scripts/spec/cite.py --check 14.9.51.4 "the line is
      *>   presented after the representation of the printed page is
      *>   advanced"                             -> OK  §14.9.51.4 25) f)
      *> The AFTER write advances, then presents AAAAA on a line it
      *> leaves open; each plain WRITE advances one line FIRST, ending
      *> that line, and presents its record on the next one. So the
      *> file holds four lines: an empty one, AAAAA, BBBBB, CCCCC --
      *> never AAAAABBBBB on one line (the weld PB964 removed).
      *> LINE SEQUENTIAL is a COBOL-2023 organization (§12.4.5.10.3
      *> GR2), so the observation lives here. The physical bytes are
      *> read back through a second FD over the same file with a
      *> ONE-character record sequential record (§12.4.5.10.3 GR3),
      *> because a same-width line sequential read-back splits a
      *> welded line again and hides the defect. Each byte displays as
      *> itself and LF as "/"; a CR is not shown, because the line
      *> delimiter is the HOST newline -- CR LF on Windows, LF on Linux
      *> and macOS (docs/CONFORMANCE.md DOC-A.1-114, kb/Work PB1540) --
      *> and it is the LF that ends the line on every host.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB964LSW.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT LS-OUT ASSIGN TO "pb964lsw.txt"
               ORGANIZATION IS LINE SEQUENTIAL.
           SELECT BY-IN ASSIGN TO "pb964lsw.txt"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD LS-OUT.
       01 LS-REC PIC X(5).
       FD BY-IN.
       01 BY-REC PIC X.
       WORKING-STORAGE SECTION.
       01 WS-EOF   PIC X VALUE "N".
       01 WS-LINE  PIC X(40) VALUE SPACES.
       01 WS-PTR   PIC 99 VALUE 1.
       01 WS-LINES PIC 9 VALUE 0.
       01 WS-CH    PIC X.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT LS-OUT
           MOVE "AAAAA" TO LS-REC
           WRITE LS-REC AFTER ADVANCING 1 LINE
           MOVE "BBBBB" TO LS-REC
           WRITE LS-REC
           MOVE "CCCCC" TO LS-REC
           WRITE LS-REC
           CLOSE LS-OUT
           OPEN INPUT BY-IN
           PERFORM UNTIL WS-EOF = "Y"
               READ BY-IN
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END PERFORM SHOW-BYTE
               END-READ
           END-PERFORM
           CLOSE BY-IN
           DISPLAY "BYTES=" WS-LINE
           DISPLAY "LINES=" WS-LINES
           STOP RUN.
       SHOW-BYTE.
           EVALUATE BY-REC
               WHEN X"0D" CONTINUE
               WHEN X"0A" MOVE "/" TO WS-CH
                          ADD 1 TO WS-LINES
                          PERFORM APPEND-CH
               WHEN OTHER MOVE BY-REC TO WS-CH
                          PERFORM APPEND-CH
           END-EVALUATE.
       APPEND-CH.
           STRING WS-CH DELIMITED BY SIZE INTO WS-LINE
               WITH POINTER WS-PTR
           END-STRING.
