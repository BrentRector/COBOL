      *> ⛔ A REPORT LINE NUMBER IS A PAGE LINE NUMBER, AND LINE 1 IS THE FIRST LINE OF THE FILE
      *> (kb/Work PB484). Before this, every line of every report printed ONE LINE TOO LOW: the engine
      *> translated line number n into n line advances from a page whose print stream was already resting
      *> ON line 1, so an unpaged report whose first body group is LINE PLUS 1 opened with a blank line.
      *>
      *> THE RULES, in the order this program exercises them.
      *>
      *> §14.9.21.4 GR1 b) — "LINE-COUNTER is set to zero." INITIATE leaves the counter at 0.
      *>
      *> §13.18.39.4 GR2 a) — "If integer-1 is not specified, the report consists of a single page of
      *> indefinite length": `RD R-FL.` with no PAGE clause is an UNPAGED report, so §13.18.35.4 GR5 c) —
      *> "If the first LINE NUMBER clause of the report group is relative and the report is not divided
      *> into pages, the report group's first line number is obtained by adding integer-2 to the current
      *> value of the report's LINE-COUNTER" — governs every one of its body groups. (GR5 b) 3's FIRST
      *> DETAIL arm cannot apply: an unpaged report has no page regions. §13.18.35.3 SR5 is why every LINE
      *> clause here is relative: "If the report is not divided into pages, all its LINE clauses shall be
      *> relative.")
      *>
      *> §13.18.35.4 GR6 — "the report's LINE-COUNTER is set equal to that line number and the line is now
      *> printed on the page at that vertical location" — is the sentence that makes the line NUMBER a
      *> physical PAGE POSITION, and GR7's "Any unoccupied lines on the page result in a blank line" fixes
      *> how many blank lines separate two printed ones.
      *>
      *> DERIVATION, line by line. LINE-COUNTER starts at 0.
      *>   GENERATE DET-A  — first LINE clause relative, unpaged ⇒ GR5 c): 0 + 1 = 1. FIRST prints on the
      *>                     FIRST line of the file, with nothing above it. LINE-COUNTER = 1 (GR6).
      *>   GENERATE DET-B  — GR5 c): 1 + 2 = 3 ⇒ SECOND on line 3, line 2 unoccupied ⇒ blank (GR7).
      *>                     Its second LINE clause is subsequent, so §13.18.35.4 GR7 b) applies:
      *>                     3 + 1 = 4 ⇒ MORE on line 4. LINE-COUNTER = 4.
      *>   GENERATE DET-C  — GR5 c): 4 + 3 = 7 ⇒ THIRD on line 7, lines 5 and 6 blank (GR7).
      *>   §13.18.35.4 GR8 — "The final line number for the report group is given by the value contained in
      *>                     the report's LINE-COUNTER when the last line of the report group has been
      *>                     printed" ⇒ LINE-COUNTER = 7, read through §8.4.3.15.3 SR1 (the procedure
      *>                     division may reference it wherever an integer data item may appear).
      *> TERMINATE prints nothing: the report has no CONTROL, no page footing and no report footing.
      *>
      *> THE READ-BACK NUMBERS EVERY PHYSICAL LINE, blanks included — that is the whole point, and it is
      *> why this golden cannot use the blank-skipping reader the other report goldens share. A second
      *> SELECT over the same file with a one-character record reassembles the bytes at any edition.
      *> (ORGANIZATION LINE SEQUENTIAL — §12.4.5.10.3 GR2, "The LINE SEQUENTIAL phrase specifies that the
      *> file organization is line sequential" — would pin the content just as well, but the compiler gates
      *> it at 2023, construct `file-organization-line-sequential-2023`, and Report Writer is a COBOL-85
      *> facility, so this golden runs at the placement rule's own introducing edition.)
      *> The last report line carries no trailing newline, so the loop flushes it after end-of-file.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB484RLP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0A SYM-X0D
               ARE 11 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb484rlp.txt".
           SELECT CHK ASSIGN TO "pb484rlp.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-FL.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LN   PIC 99    VALUE 0.
       01  WS-LC   PIC 9999  VALUE 0.
       01  WS-LINE PIC X(20) VALUE SPACES.
       REPORT SECTION.
       RD  R-FL.
       01  DET-A TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC X(6) VALUE "FIRST".
       01  DET-B TYPE DE.
           02  LINE PLUS 2.
               03  COLUMN 1 PIC X(6) VALUE "SECOND".
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X(6) VALUE "MORE".
       01  DET-C TYPE DE LINE PLUS 3.
           03  COLUMN 1 PIC X(6) VALUE "THIRD".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT PRT.
           INITIATE R-FL.
           GENERATE DET-A.
           GENERATE DET-B.
           GENERATE DET-C.
           MOVE LINE-COUNTER IN R-FL TO WS-LC.
           TERMINATE R-FL.
           CLOSE PRT.
           OPEN INPUT CHK.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHK.
           IF WS-I > 0
               PERFORM SHOW-LINE
           END-IF.
           DISPLAY "LINE-COUNTER=[" WS-LC "]".
           STOP RUN.
       TAKE-BYTE.
           IF CHK-REC = SYM-X0A
               PERFORM SHOW-LINE
           ELSE
               IF CHK-REC NOT = SYM-X0D
                   ADD 1 TO WS-I
                   MOVE CHK-REC TO WS-LINE(WS-I:1)
               END-IF
           END-IF.
       SHOW-LINE.
           ADD 1 TO WS-LN.
           DISPLAY "LINE " WS-LN " [" WS-LINE(1:6) "]".
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
