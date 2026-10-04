      *> kb/Work PB1947 - ISO 13.10.3 SR2: "Except in a compiler directive, constant-name-1 may be used
      *>   anywhere that a format specifies a literal of the class and category of constant-name-1."
      *>   cite.py: OK  13.10.3 2)  (Syntax rules)
      *> 5.5 1): an integer-n "refers to a fixed-point integer literal", so every integer-n of a general
      *>   format is a literal position and an integer constant-name stands there.
      *>   cite.py: OK  5.5 1)  (Integer operands)
      *> 13.10.4 GR1: the effect of constant-name-1 is "as if literal-1 ... were written where
      *>   constant-name-1 is written."
      *>   cite.py: OK  13.10.4 1)  (General rules)
      *> The report grammar spelled each integer position of the PAGE, LINE, COLUMN and NEXT GROUP
      *> clauses and of OCCURS ... STEP as a bare integer literal, so every constant-name below was
      *> COBOL0309 "A literal value is expected here, not a data-name".
      *>
      *> DERIVATION. Constants: KP 12 (page limit), KW 40 (page width), KH 2 (HEADING), KF 3 (FIRST
      *> DETAIL), KD 9 (LAST DETAIL), KT 11 (FOOTING), KC 5 (COLUMN), KR 3 (COLUMN PLUS), KL 2 (LINE
      *> PLUS), KA 4 (absolute LINE), KG 1 (NEXT GROUP PLUS), KN 2 (OCCURS), KS 6 (STEP).
      *> The PAGE clause is therefore PAGE LIMIT 12 LINES 40 COLUMNS HEADING 2 FIRST DETAIL 3 LAST
      *> DETAIL 9 FOOTING 11 (13.18.39.2); no page heading or footing group is described.
      *>   13.18.39.4 GR2 a): "Integer-1 is the page limit."
      *>   cite.py: OK  13.18.39.4 2) a)  (General rules)
      *> Line 3 - D1 is the first body group of the page and its first LINE clause is relative, so
      *> 13.18.35.4 GR5 b) 3. puts it on the FIRST DETAIL line (3), ignoring integer-2. COLUMN 5 puts the
      *> PIC 9 "5" at column 5; 13.18.14.4 GR9 leaves the horizontal counter at 5, and COLUMN PLUS 3 puts
      *> the next item's leftmost column at 5 + 3 = 8 (GR8: "The position of the item's leftmost character
      *> is obtained by adding integer-2 to the current line's horizontal counter."): "AB" at columns 8-9,
      *> so the line is four spaces, 5, two spaces, AB.
      *> Line 4 - D2's first LINE clause is absolute (4) and 4 > LINE-COUNTER (3), so the page fit is
      *> successful (13.18.35.4 GR4 b) and the line is printed on line 4 (GR5 a): "ZZ" at column 1. Its NEXT
      *> GROUP PLUS 1 adds 1 to LINE-COUNTER, because 1 + 4 < the FOOTING integer 11 (13.18.37.4 GR4 b):
      *> LINE-COUNTER becomes 5.
      *> Line 7 - D3's LINE PLUS 2 is relative, not the first body group on the page, so its line is
      *> LINE-COUNTER + 2 = 7 (13.18.35.4 GR5 b) 3.), which also fits (trial sum 7 <= LAST DETAIL 9). Its
      *> entry OCCURS 2 TIMES STEP 6 with COLUMN 1 and VALUE 7 prints 7 at columns 1 and 7
      *> (13.18.38.4 GR12 a); 13.18.63.4 GR9 via GR21). Lines 5 and 6 are unoccupied, hence blank (GR7).
      *>   (Without the NEXT GROUP clause D3 would be on line 6, which is what tells the clause was read.)
      *> The read-back numbers each physical line and prints its first 12 bytes.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1947CON.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "PB1947CON.TXT".
           SELECT CHK ASSIGN TO "PB1947CON.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  KP CONSTANT AS 12.
       01  KW CONSTANT AS 40.
       01  KH CONSTANT AS 2.
       01  KF CONSTANT AS 3.
       01  KD CONSTANT AS 9.
       01  KT CONSTANT AS 11.
       01  KC CONSTANT AS 5.
       01  KR CONSTANT AS 3.
       01  KL CONSTANT AS 2.
       01  KA CONSTANT AS 4.
       01  KG CONSTANT AS 1.
       01  KN CONSTANT AS 2.
       01  KS CONSTANT AS 6.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-BYTE PIC X.
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LN   PIC 99    VALUE 0.
       01  WS-LINE PIC X(20) VALUE SPACES.
       REPORT SECTION.
       RD  R PAGE LIMIT IS KP LINES KW COLUMNS HEADING KH
           FIRST DETAIL KF LAST DETAIL KD FOOTING KT.
       01  D1 TYPE DE LINE PLUS KL.
           03  COLUMN KC PIC 9 VALUE KC.
           03  COLUMN PLUS KR PIC X(2) VALUE "AB".
       01  D2 TYPE DE LINE KA NEXT GROUP PLUS KG.
           03  COLUMN 1 PIC X(2) VALUE "ZZ".
       01  D3 TYPE DE LINE PLUS KL.
           03  COLUMN 1 PIC 9 OCCURS KN TIMES STEP KS VALUE 7.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE R.
           GENERATE D1.
           GENERATE D2.
           GENERATE D3.
           TERMINATE R.
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
               WHEN WS-BYTE = X"0A"
                   PERFORM SHOW-LINE
               WHEN WS-BYTE = X"0C"
                   IF WS-I > 0
                       PERFORM SHOW-LINE
                   END-IF
                   MOVE 0 TO WS-LN
               WHEN WS-BYTE = X"0D"
                   CONTINUE
               WHEN OTHER
                   ADD 1 TO WS-I
                   MOVE WS-BYTE TO WS-LINE(WS-I:1)
           END-EVALUATE.
       SHOW-LINE.
           ADD 1 TO WS-LN.
           DISPLAY "LINE " WS-LN " [" WS-LINE(1:12) "]".
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
