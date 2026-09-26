       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB690RPT.
      *> ISO/IEC 1989:2023 §13.4.5 FD entry GR4 (the report writer
      *> logical record structure, Annex A.1 item 159) with the file
      *> coded character set of Annex A.1 item 31 (owner decision
      *> kb/Work R47; kb/Work PB690).
      *>
      *> The determination (docs/CONFORMANCE.md DOC-A.1-159): one print
      *> line is one record, one byte per column in ISO/IEC 8859-1; a
      *> character U+0000-U+00FF is written as the byte of the same
      *> value, and a print line holding a character above U+00FF is
      *> NOT written — the write the report writer performs is
      *> unsuccessful with I-O status '91' in the report file's
      *> connector (§9.1.13.11 item 1). GENERATE is not among the
      *> statements §9.1.13.1 lists as setting an I-O status, so the
      *> FILE STATUS item is not stored by it; what the program can
      *> observe is the report file itself. §14.9.51.4 GR15: an
      *> unsuccessful write "does not take place", its advance
      *> included — so the device stays where it was and the NEXT
      *> line's advance covers the refused line's slot: the refused
      *> detail leaves a blank line, and every later line of the page
      *> still lands on its own LINE-COUNTER line.
      *>
      *> Why each leg can fail:
      *>  LINE 1 - "A1B", ORD of column 2 = 50 ("1").
      *>  LINE 2 - BLANK: the U+20AC detail was refused. The defect
      *>           wrote "A?B" (the old print map, and Latin-1's
      *>           replacement); a device that advanced on the refused
      *>           write would close the gap and shift LINE 3 up.
      *>  LINE 3 - "AéB": U+00E9 is its own byte 0xE9, ORD 234 — the
      *>           old print map wrote every character above U+007F
      *>           as '?' (ORD 64).
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb690rpt.txt".
           SELECT CHK ASSIGN TO "pb690rpt.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-A.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-A    PIC X(3).
       01  WS-N    PIC 9     VALUE 0.
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LINE PIC X(30) VALUE SPACES.
       REPORT SECTION.
       RD  R-A PAGE LIMIT 20 LINES.
       01  DET TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC X(3) SOURCE WS-A.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT PRT.
           INITIATE R-A.
           MOVE "A1B" TO WS-A.
           GENERATE DET.
           MOVE "A€B" TO WS-A.
           GENERATE DET.
           MOVE "AéB" TO WS-A.
           GENERATE DET.
           TERMINATE R-A.
           CLOSE PRT.
           OPEN INPUT CHK.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHK.
           STOP RUN.
       TAKE-BYTE.
           IF CHK-REC = X"0A"
               PERFORM SHOW-LINE
           ELSE
               IF CHK-REC NOT = X"0D"
                   ADD 1 TO WS-I
                   MOVE CHK-REC TO WS-LINE(WS-I:1)
               END-IF
           END-IF.
       SHOW-LINE.
           ADD 1 TO WS-N.
           IF WS-I = 0
               DISPLAY "LINE " WS-N " BLANK"
           ELSE
               DISPLAY "LINE " WS-N " [" WS-LINE(1:WS-I) "] "
                   FUNCTION ORD(WS-LINE(2:1))
           END-IF.
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
