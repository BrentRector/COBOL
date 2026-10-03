      *> kb/Work PB1292 + PB1129 - the RD CODE clause's identifier-1 is an IDENTIFIER (ISO 13.18.12), so it may be
      *> subscripted or reference-modified, at the rule's INTRODUCING edition ('85).
      *>
      *> RULES (cite.py --check, all OK):
      *> 13.18.12.3 SR2: "Identifier-1 shall reference an alphanumeric data item that shall not be an
      *> occurs-depending-on group item, a variable-length group, or a dynamic-length elementary item."
      *> 13.18.12.4 GR1: "When the CODE clause is specified, literal-1 or identifier-1 is automatically placed in the
      *> first characters of each logical record written to the report file for this report."
      *> 13.18.12.4 GR3: "If identifier-1 is specified, it is evaluated at the start of the processing for each body
      *> group, either during page advance processing, as detailed in the GENERATE statement, or whenever page
      *> advance processing is not performed. The resultant value is used until the next evaluation."
      *> 8.4.3.1.2 / 8.4.3.3.4: an identifier carries its subscripts and reference modification; a reference-modified
      *> identifier references the unique data item of the slice (an alphanumeric item, GR6 c)).
      *>
      *> DERIVATION (both reports are unpaged, so no page advance is performed and each body group evaluates the code
      *> at its own start):
      *>   R-A, CODE IS WS-CD(2): WS-CD(2) = A then B   => records `ADA1`, `BDA2` (code, the VALUE "DA", the SOURCE).
      *>   R-B, CODE IS WS-CT(2:1): WS-CT = xyz then XYZ => the one-character slice y then Y => `yDB1`, `YDB2`.
      *> Fails (COBOLNET0899 'a subscripted or reference-modified CODE operand ... is not yet implemented') on a binder
      *> whose CODE identifier keeps only a base name.
      *>
      *> THE READ-BACK IS BYTE-WISE: a one-character record on a second SELECT over each report file is legal at every
      *> edition (see pb565_report_repeating_entry).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1292CF.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PA ASSIGN TO "pb1292cfa.txt".
           SELECT PB ASSIGN TO "pb1292cfb.txt".
           SELECT CA ASSIGN TO "pb1292cfa.txt".
           SELECT CB ASSIGN TO "pb1292cfb.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PA REPORT IS R-A.
       FD  PB REPORT IS R-B.
       FD  CA.
       01  CA-REC PIC X.
       FD  CB.
       01  CB-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF  PIC X VALUE "N".
       01  WS-I    PIC 99 VALUE 0.
       01  WS-LINE PIC X(30) VALUE SPACES.
       01  WS-BYTE PIC X.
       01  WS-N    PIC 9 VALUE 0.
       01  WS-CODES.
           05  WS-CD PIC X OCCURS 3.
       01  WS-CT   PIC X(3) VALUE "xyz".
       REPORT SECTION.
       RD  R-A CODE IS WS-CD(2).
       01  DE-A TYPE DETAIL LINE PLUS 1.
           02  COLUMN 1 PIC X(2) VALUE "DA".
           02  COLUMN 3 PIC 9 SOURCE WS-N.
       RD  R-B CODE IS WS-CT(2:1).
       01  DE-B TYPE DETAIL LINE PLUS 1.
           02  COLUMN 1 PIC X(2) VALUE "DB".
           02  COLUMN 3 PIC 9 SOURCE WS-N.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "-A-" TO WS-CODES.
           OPEN OUTPUT PA PB.
           INITIATE R-A R-B.
           MOVE 1 TO WS-N.
           GENERATE DE-A.
           GENERATE DE-B.
           MOVE "B" TO WS-CD(2).
           MOVE "XYZ" TO WS-CT.
           MOVE 2 TO WS-N.
           GENERATE DE-A.
           GENERATE DE-B.
           TERMINATE R-A R-B.
           CLOSE PA PB.
           OPEN INPUT CA.
           PERFORM UNTIL WS-EOF = "Y"
               READ CA
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END MOVE CA-REC TO WS-BYTE PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CA.
           PERFORM SHOW-LINE.
           MOVE "N" TO WS-EOF.
           OPEN INPUT CB.
           PERFORM UNTIL WS-EOF = "Y"
               READ CB
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END MOVE CB-REC TO WS-BYTE PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CB.
           PERFORM SHOW-LINE.
           STOP RUN.
       TAKE-BYTE.
           IF WS-BYTE = X"0A"
               PERFORM SHOW-LINE
           ELSE
               IF WS-BYTE NOT = X"0D"
                   ADD 1 TO WS-I
                   MOVE WS-BYTE TO WS-LINE(WS-I:1)
               END-IF
           END-IF.
       SHOW-LINE.
           IF WS-I > 0
               DISPLAY "[" WS-LINE(1:WS-I) "]"
               MOVE SPACES TO WS-LINE
               MOVE 0 TO WS-I
           END-IF.
