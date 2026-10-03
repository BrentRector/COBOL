      *> kb/Work PB1129 - the RD CODE clause (ISO 13.18.12), identifier form and WHEN it is evaluated.
      *>
      *> RULES (cite.py --check, all OK):
      *> 13.18.12.4 GR3: "If identifier-1 is specified, it is evaluated at the start of the processing for each
      *> body group, either during page advance processing, as detailed in the GENERATE statement, or whenever page
      *> advance processing is not performed. The resultant value is used until the next evaluation."
      *> 14.9.16.4 GR6: a page advance is a) the page footing is printed, b) an advance is made to the next
      *> physical page, c) "If the associated report description entry contains a CODE clause with an identifier
      *> operand, the identifier is evaluated.", d) PAGE-COUNTER, e) LINE-COUNTER, f) the page heading is printed.
      *> 13.18.12.4 GR1: the value stands in the first characters of each logical record of the report.
      *>
      *> DERIVATION. PAGE LIMIT 4, HEADING 1, FIRST DETAIL 2, LAST DETAIL 3, FOOTING 4: a page holds the heading
      *> (line 1), two details (lines 2-3) and the footing (line 4).
      *>   G1 (WS-CD "A", WS-N 1): the first GENERATE evaluates at the start of its processing: A. PH "APH",
      *>      detail "AD1" on line 2.
      *>   G2 (WS-CD "B", WS-N 2): trial line 2 + 1 = 3 <= LAST DETAIL 3, so it fits and NO page advance is
      *>      performed: evaluated at the start of the body group: B. "BD2" on line 3.
      *>   G3 (WS-CD "C", WS-N 3): trial 3 + 1 = 4 > 3, so the page fit fails and the page advance runs: a) the
      *>      page footing prints with the code STILL IN FORCE, B, although WS-CD already holds C ("BPF", line
      *>      4); b) the feed; c) the evaluation: C; f) the page heading "CPH"; then the detail "CD3" on line 2.
      *>   WS-CD "D" then TERMINATE: no body group is processed (no control footing), so nothing evaluates, and the
      *>      last page's footing prints with C ("CPF", line 4).
      *> Fails with "CPF" -> "DPF" if the footing at TERMINATE re-evaluates, and with "BPF" -> "CPF" if the
      *> evaluation precedes the page advance's footing instead of following it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1129I.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb1129i.txt".
           SELECT CHK ASSIGN TO "pb1129i.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R-B.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-CD   PIC X     VALUE SPACE.
       01  WS-N    PIC 9     VALUE 0.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-B    PIC X.
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LINE PIC X(6)  VALUE SPACES.
       REPORT SECTION.
       RD  R-B CODE IS WS-CD
           PAGE LIMIT IS 4 LINES HEADING 1 FIRST DETAIL 2
           LAST DETAIL 3 FOOTING 4.
       01  PH-B TYPE PAGE HEADING.
           02  LINE 1.
               03  COLUMN 1 PIC X(2) VALUE "PH".
       01  DE-B TYPE DETAIL LINE PLUS 1.
           02  COLUMN 1 PIC X VALUE "D".
           02  COLUMN 2 PIC 9 SOURCE WS-N.
       01  PF-B TYPE PAGE FOOTING.
           02  LINE 4.
               03  COLUMN 1 PIC X(2) VALUE "PF".
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT RPT.
           INITIATE R-B.
           MOVE "A" TO WS-CD.
           MOVE 1 TO WS-N.
           GENERATE DE-B.
           MOVE "B" TO WS-CD.
           MOVE 2 TO WS-N.
           GENERATE DE-B.
           MOVE "C" TO WS-CD.
           MOVE 3 TO WS-N.
           GENERATE DE-B.
           MOVE "D" TO WS-CD.
           TERMINATE R-B.
           CLOSE RPT.
           OPEN INPUT CHK.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END MOVE CHK-REC TO WS-B PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHK.
           IF WS-I > 0
               PERFORM SHOW-LINE
           END-IF.
           STOP RUN.
       TAKE-BYTE.
           IF WS-B = X"0A"
               PERFORM SHOW-LINE
           ELSE
               IF WS-B = X"0C"
                   IF WS-I > 0
                       PERFORM SHOW-LINE
                   END-IF
                   DISPLAY "(page)"
               ELSE
                   IF WS-B NOT = X"0D"
                       ADD 1 TO WS-I
                       MOVE WS-B TO WS-LINE(WS-I:1)
                   END-IF
               END-IF
           END-IF.
       SHOW-LINE.
           IF WS-LINE NOT = SPACES
               DISPLAY "[" WS-LINE "]"
           END-IF.
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
