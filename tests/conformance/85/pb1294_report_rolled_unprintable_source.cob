      *> kb/Work PB1294 - ISO 13.18.54.4 GR6 with 13.18.53.4 GR3: a SUM clause names an UNPRINTABLE SOURCE entry.
      *> 13.18.53.4 GR3: "If the entry containing the SOURCE clause contains no COLUMN clause and therefore defines an
      *>   unprintable item, the SOURCE clause causes no action, except where the entry is referred to by means of a
      *>   SUM clause."   cite.py: OK  13.18.53.4 3)  (General rules)
      *> 13.18.54.3 SR4: "Data-name-1 shall be the name of a numeric data item in the report section."
      *>   cite.py: OK  13.18.54.3 4)  (Syntax rules)
      *> 13.18.54.4 GR6: "If data-name-1 specifies an item whose entry has a SOURCE or VALUE clause, the value added is
      *>   that of the operand of the SOURCE or VALUE clause."   cite.py: OK  13.18.54.4 6)  (General rules)
      *> GR7 a): data-name-1 in a different report group description is added "when the report group description
      *>   containing data-name-1 is processed".   cite.py: OK  13.18.54.4 7) a)  (General rules)
      *> The report writer's SUM data-name-1 is no later addition: the rule is the same at every edition, so this
      *> program (nothing in it is newer than COBOL-85) is the introducing-edition witness and the 2002 programs
      *> pb1294_report_rolled_* the later-construct ones.
      *>
      *> DERIVATION. The detail DET has UNP (no COLUMN: unprintable, SOURCE WS-AMT) and PRN (COLUMN 23, SOURCE
      *> WS-AMT). The FINAL control footing sums both. WS-AMT is 5 for the first GENERATE and 7 for the second, so each
      *> counter holds 5 + 7 = 12 when TERMINATE prints the footing (UNP prints nothing itself: GR3). Line 1 DET(5)
      *> prints PRN PIC 9(4) at column 23: `0005`; line 2 DET(7): `0007`; line 3, the footing, prints both counters,
      *> PIC 9(5) at columns 1 and 7: `00012 00012`.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1294UNP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0A SYM-X0C SYM-X0D
               ARE 11 13 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "PB1294UNP.TXT".
           SELECT CHK ASSIGN TO "PB1294UNP.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R2.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-AMT  PIC 99 VALUE 0.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-BYTE PIC X.
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LN   PIC 99    VALUE 0.
       01  WS-LINE PIC X(30) VALUE SPACES.
       REPORT SECTION.
       RD  R2 CONTROL IS FINAL.
       01  DET TYPE DE LINE PLUS 1.
           05  UNP PIC 9(3) SOURCE WS-AMT.
           05  PRN COLUMN 23 PIC 9(4) SOURCE WS-AMT.
       01  CFT TYPE CF FINAL LINE PLUS 1.
           05  COLUMN 1 PIC 9(5) SUM UNP.
           05  COLUMN 7 PIC 9(5) SUM PRN.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE R2.
           MOVE 5 TO WS-AMT.
           GENERATE DET.
           MOVE 7 TO WS-AMT.
           GENERATE DET.
           TERMINATE R2.
           CLOSE PRT.
           OPEN INPUT CHK.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END
                       MOVE CHK-REC TO WS-BYTE
                       PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHK.
           IF WS-I > 0
               PERFORM SHOW-LINE
           END-IF.
           STOP RUN.
       TAKE-BYTE.
           EVALUATE TRUE
               WHEN WS-BYTE = SYM-X0A
                   PERFORM SHOW-LINE
               WHEN WS-BYTE = SYM-X0C
                   IF WS-I > 0
                       PERFORM SHOW-LINE
                   END-IF
                   MOVE 0 TO WS-LN
               WHEN WS-BYTE = SYM-X0D
                   CONTINUE
               WHEN OTHER
                   ADD 1 TO WS-I
                   MOVE WS-BYTE TO WS-LINE(WS-I:1)
           END-EVALUATE.
       SHOW-LINE.
           ADD 1 TO WS-LN.
           DISPLAY "LINE " WS-LN " [" WS-LINE(1:28) "]".
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
