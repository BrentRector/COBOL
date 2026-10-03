      *> kb/Work PB1271 - a REPEATING report entry's sum counter is a TABLE: one counter per occurrence,
      *> referenced from the procedure division with one subscript per repetition level.
      *>
      *> THE RULES.
      *> 13.18.54.4 GR1: "Each entry containing a SUM clause establishes an independent sum counter".
      *>   cite.py: OK  13.18.54.4 1)  (General rules)
      *> 13.18.54.4 GR8 a): "each occurrence of the addend is added into the corresponding occurrence of the
      *> sum counter" - so a sum counter subject to a repetition HAS occurrences.
      *>   cite.py: OK  13.18.54.4 8) a)  (General rules)
      *> 13.18.14.4 GR12: "A multiple COLUMN clause is functionally equivalent to a COLUMN clause with a single
      *> operand, together with a simple OCCURS clause" - and 13.18.35.4 GR9 says the same of a multiple LINE
      *> clause, so every repetition vehicle of 13.15.4 GR3 is an OCCURS level of the counter.
      *>   cite.py: OK  13.18.14.4 12)  /  OK  13.18.35.4 9)  /  OK  13.15.4 3)  (General rules)
      *> 13.18.54.4 GR12: "It is permissible for procedure division statements to alter the content of sum
      *> counters"; 13.8.6.2.3 1): "Sum counters may be inspected or altered in the procedure division."
      *>   cite.py: OK  13.18.54.4 12)  /  OK  13.8.6.2.3 1)
      *> 8.4.2.3.3 SR3: "the number of subscripts shall equal the number of OCCURS clauses"; SR6: ALL may be
      *> used "When the subscripted identifier is used as an intrinsic function argument".
      *>   cite.py: OK  8.4.2.3.3 3)  /  OK  8.4.2.3.3 6)  (Syntax rules)
      *> 13.18.54.4 GR10: "If the entry is associated with an absent data item as a result of a PRESENT WHEN
      *> clause or an OCCURS clause with the DEPENDING phrase, the corresponding sum counter is not printed and
      *> is not reset to zero for the current instance of the report group."
      *>   cite.py: OK  13.18.54.4 10)  (General rules)
      *>
      *> DERIVATION. INITIATE zeroes every counter (GR2). Before any GENERATE the program alters CF-U(2) to 500,
      *> CF-U(3) (qualified IN R1) to 1, CF-V(1, 1) to 40 and CF-V(2, WS-J = 2) to 7. Each of the three GENERATE
      *> statements adds WS-X = 3 into EVERY occurrence of CF-U and CF-V (GR7 c) 1. - an identifier addend is
      *> added on every GENERATE; GR8 is about a repeating ADDEND, and WS-X is not one), so at TERMINATE
      *> CF-U = 9, 509, 10 and CF-V = (49, 9) (9, 16). Hence INTEGER(CF-U(2)) = 509, SUM(CF-U(ALL)) =
      *> 9 + 509 + 10 = 528, SUM(CF-V(ALL, ALL)) = 49 + 9 + 9 + 16 = 83 and SUM(CF-V(2, ALL)) = 25.
      *> The CONTROL FOOTING FINAL prints at TERMINATE: one line with CF-U's three occurrences at columns 1, 13
      *> and 25, then CFL's two lines (13.18.38.4 GR10 - "integer-2 distinct report items") with CF-V's two
      *> occurrences each at columns 1 and 13.
      *> The detail's DL line repeats OCCURS 1 TO 2 DEPENDING ON WS-N (13.18.38.4 GR13), each occurrence with its
      *> own counter DT adding WS-K = 5. GENERATE 1 (N = 2): both print 05 and both reset (GR2). GENERATE 2
      *> (N = 1): occurrence 1 prints 05 and resets; occurrence 2 is ABSENT, so by GR10 it is neither printed nor
      *> reset and keeps its 05. GENERATE 3 (N = 2): occurrence 1 prints 05, occurrence 2 prints 05 + 5 = 10.
      *> Last, DT(1) and DT(2) are set to 11 and 22 and summed with an ALL subscript. 15.3: "If the ALL subscript is
      *> associated with a data item described with an OCCURS DEPENDING ON clause, the range of values is
      *> determined by the object of the OCCURS DEPENDING ON clause" - for a report OCCURS that object counts as
      *> 13.18.38.4 GR13 says: WS-N = 1 lies in integer-1 to (integer-2 - 1) = 1 to 1, so ONE occurrence, DT1 = 11;
      *> WS-N = 7 does not, so "as though the OCCURS clause had been written without the TO and DEPENDING
      *> phrases" - both occurrences, DT2 = 33.
      *>   cite.py: OK  15.3 14)  (Arguments)  /  OK  13.18.38.4 13)  (General rules)
      *> Before the fix every subscripted counter reference was refused COBOLNET1639 "not defined", a multiple
      *> COLUMN entry shared ONE counter among its printable items, and an absent occurrence's counter was reset.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1271SC.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb1271sc.txt".
           SELECT CHK ASSIGN TO "pb1271sc.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R1.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF   PIC X     VALUE "N".
       01  WS-X     PIC 9     VALUE 3.
       01  WS-K     PIC 9     VALUE 5.
       01  WS-N     PIC 9     VALUE 2.
       01  WS-J     PIC 9     VALUE 2.
       01  WS-I     PIC 99    VALUE 0.
       01  WS-LINE  PIC X(40) VALUE SPACES.
       REPORT SECTION.
       RD  R1 CONTROL IS FINAL PAGE LIMIT 30 LINES.
       01  DET TYPE DE.
           03 LINE PLUS 1.
              05 COLUMN 1 PIC X(3) VALUE "DET".
           03 DL LINE PLUS 1 OCCURS 1 TO 2 DEPENDING ON WS-N.
              05 DT COLUMN 1 PIC 99 SUM WS-K.
       01  CFG TYPE IS CONTROL FOOTING FINAL.
           03 LINE PLUS 1.
              05 CF-U COLUMN 1 13 25 PIC 9999 SUM WS-X.
           03 CFL OCCURS 2 TIMES LINE PLUS 1.
              05 CF-V COLUMN 1 13 PIC 9999 SUM WS-X.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT PRT.
           INITIATE R1.
           MOVE 500 TO CF-U (2).
           ADD 1 TO CF-U IN R1 (3).
           MOVE 40 TO CF-V (1, 1).
           ADD 7 TO CF-V (2 WS-J).
           GENERATE DET.
           MOVE 1 TO WS-N.
           GENERATE DET.
           MOVE 2 TO WS-N.
           GENERATE DET.
           DISPLAY "INT=" FUNCTION INTEGER (CF-U (2)).
           DISPLAY "SUM=" FUNCTION SUM (CF-U (ALL)).
           DISPLAY "ALL2=" FUNCTION SUM (CF-V (ALL, ALL)).
           DISPLAY "ROW2=" FUNCTION SUM (CF-V (2, ALL)).
           MOVE 11 TO DT (1).
           MOVE 22 TO DT (2).
           MOVE 1 TO WS-N.
           DISPLAY "DT1=" FUNCTION SUM (DT (ALL)).
           MOVE 7 TO WS-N.
           DISPLAY "DT2=" FUNCTION SUM (DT (ALL)).
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
