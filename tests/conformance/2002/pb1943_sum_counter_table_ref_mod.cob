       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1943B.
      *> kb/Work PB1943 + PB2520 - a REPEATING sum counter is reference-modified after its subscripts, a counter is
      *> reference-modified inside a report group entry's own SOURCE clause, and a counter has the length of its digits.
      *>
      *> 13.18.54.4 GR1 and GR5 as in 85/pb1943_sum_counter_ref_mod: the counter is "a conceptual data item that
      *> behaves as a data item of the category numeric" with the digits of the PICTURE and no stated usage, which
      *> the implementor makes USAGE DISPLAY, signed (docs/CONFORMANCE.md A.4.11); 8.4.3.3.3 SR1 then admits it.
      *>   cite.py: OK  13.18.54.4 1)  /  OK  13.18.54.4 5)  /  OK  8.4.3.3.3 1)
      *> 8.4.2.3.3 SR3: "the number of subscripts shall equal the number of OCCURS clauses"; a multiple COLUMN
      *> entry's counter is a table (13.18.54.4 GR8 a; kb/Work PB1271), so the subscript comes first and the reference
      *> modifier follows it: CF-U (2) (2:3).
      *>   cite.py: OK  8.4.2.3.3 3)  /  OK  13.18.54.4 8) a)
      *> 13.18.53.3 SR4: identifier-1 of a SOURCE clause, if it specifies a report section item, "shall be a report
      *> counter identifier or a sum counter defined in the current report"; as an identifier it is reference-modified
      *> like any other (8.4.3.3.3). The counter is read when the footing prints (TERMINATE), after the procedural
      *> alterations below.
      *>   cite.py: OK  13.18.53.3 4)  (Syntax rules)
      *> 15.50.1: "The LENGTH function returns an integer equal to the length of the argument in alphanumeric
      *> character positions ...": a PIC 9999 counter has 4 digits (GR1), so 4 positions, and a reference-modified
      *> one has the modifier's length. It used to answer 2, the size of a binary register (PB2520).
      *>   cite.py: OK  15.50.1
      *>
      *> DERIVATION. WS-X = 1234; the one GENERATE adds it into every occurrence of CF-U (3 columns): 1234 each.
      *>   CF-U (2) (2:3)      = digits 2-4 of the second occurrence = "2", "3" and the last digit, which carries the
      *>   counter's sign (GR1: signed; positive 4 is "D" in the default IBM overpunch convention) = "23D"
      *>   MOVE "77" TO CF-U (3) (1:2) makes the third occurrence 7734 (digits 3-4 keep their value)
      *>   FUNCTION LENGTH (CF-U (1))     = 4 (GR1's digits)
      *>   FUNCTION LENGTH (CF-U (1) (2:3)) = 3
      *> The CONTROL FOOTING FINAL prints at TERMINATE one line: CF-U at columns 1, 13 and 25 = 1234, 1234, 7734,
      *> then the SOURCE CF-U (2) (2:3) item at column 37 = "23D".
      *> Before the fix a subscripted reference-modified counter drew COBOLNET1639 "not defined", and LENGTH answered 2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "PB1943B.TXT".
           SELECT CHK ASSIGN TO "PB1943B.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R1.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF   PIC X     VALUE "N".
       01  WS-X     PIC 9999  VALUE 1234.
       01  WS-C     PIC XXX   VALUE SPACES.
       01  WS-I     PIC 99    VALUE 0.
       01  WS-LINE  PIC X(40) VALUE SPACES.
       REPORT SECTION.
       RD  R1 CONTROL IS FINAL PAGE LIMIT 30 LINES.
       01  DET TYPE DE.
           03 LINE PLUS 1.
              05 COLUMN 1 PIC X(3) VALUE "DET".
       01  CFG TYPE IS CONTROL FOOTING FINAL.
           03 LINE PLUS 1.
              05 CF-U COLUMN 1 13 25 PIC 9999 SUM WS-X.
              05 COLUMN 37 PIC XXX SOURCE CF-U (2) (2:3).
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT PRT.
           INITIATE R1.
           GENERATE DET.
           MOVE CF-U (2) (2:3) TO WS-C.
           DISPLAY "C=[" WS-C "]".
           MOVE "77" TO CF-U (3) (1:2).
           DISPLAY "LEN=" FUNCTION LENGTH (CF-U (1)).
           DISPLAY "LENRM=" FUNCTION LENGTH (CF-U (1) (2:3)).
           TERMINATE R1.
           CLOSE PRT.
           OPEN INPUT CHK.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHK.
           PERFORM SHOW-LINE.
           STOP RUN.
       TAKE-BYTE.
           IF CHK-REC = X"0A"
               PERFORM SHOW-LINE
           ELSE
               IF CHK-REC NOT = X"0D"
                   ADD 1 TO WS-I
                   MOVE CHK-REC TO WS-LINE (WS-I:1)
               END-IF
           END-IF.
       SHOW-LINE.
           IF WS-I > 0
               DISPLAY "[" WS-LINE (1:WS-I) "]"
               MOVE SPACES TO WS-LINE
               MOVE 0 TO WS-I
           END-IF.
