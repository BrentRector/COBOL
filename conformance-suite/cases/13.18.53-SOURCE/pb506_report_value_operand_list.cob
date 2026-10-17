*>--- conformance-case
*> id: pb506_report_value_operand_list
*> rules: [SR-13.18.53.3-1, GR-13.18.53.4-4, SR-13.18.63.3-34, SR-13.18.63.3-35, GR-13.18.63.4-21, GR-13.18.63.4-23, FMT-13.18.53.2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ⛔ A REPORT-SECTION VALUE CLAUSE IS AN OPERAND LIST, AND A VALUE IS AN INITIALIZATION — NOT A MOVE
      *> (kb/Work PB506).  Two rules, one lane, and both of them used to be wrong.
      *>
      *> (1) THE OPERAND LIST.  ISO §13.18.63.2 format 4 is `{ VALUE IS | VALUES ARE } { literal-1 } …`, so the
      *> clause takes one or MORE literals, and §13.18.63.4 GR23 distributes them: "If the VALUE clause has more
      *> than one operand, successive operands are assigned to successive repeating printable items, horizontally
      *> and then vertically, as applicable, in that hierarchy.  If no further operands remain, assignment begins
      *> again from the first operand."  §13.18.63.3 SR35 confines the multi-operand form to a repeating entry
      *> and fixes the count: "The number of operands of the VALUE clause shall be equal to the number of
      *> repetitions of the repeating entry or the same number multiplied by the number of repetitions of any
      *> number of successive repeating entries at higher levels than the repeating entry."  §13.15.4 GR3 says
      *> what a repeating entry is: "An entry that contains either an OCCURS clause or a LINE or COLUMN clause
      *> with more than one operand is said to be a repeating entry, and the number of repetitions is defined to
      *> be integer-2 of the OCCURS clause or the number of operands of the LINE or COLUMN clause, whichever is
      *> applicable."  The SOURCE clause carries the SAME two rules verbatim — §13.18.53.3 SR6 and §13.18.53.4
      *> GR4 — so both clauses ride one operand list and one cycling reader.
      *>
      *> (2) THE VALUE IS NOT A MOVE.  §13.18.63.4 GR21 imports GR7 into format 4: "Each literal is aligned in
      *> the associated data item in accordance with 14.6.8, Alignment and transfer of data into data items,
      *> except that initialization is not affected by a JUSTIFIED clause and no editing takes place", and GR8
      *> ("When a numeric-edited data description includes the BLANK WHEN ZERO clause and the VALUE clause uses
      *> either an alphanumeric or national literal, the BLANK WHEN ZERO clause has no effect on initialization").
      *> §13.18.63.3 SR34 imports SR11, whose second sentence is "Editing characters in a picture character-string
      *> for an alphanumeric-edited or national-edited data item do not cause editing of the initial value when
      *> the data item is initialized."  The report emitter used to push a VALUE operand through the SOURCE
      *> clause's implicit MOVE (§13.18.53.4 GR1), which applies exactly the three transforms those rules
      *> exclude.  EVERY printable item below has its IDENTICAL twin in WORKING-STORAGE, and the program prints
      *> both: the two lanes are one rule and shall answer alike.
      *>
      *> DERIVATION OF THE EXPECTED OUTPUT.  One TYPE DE group, one LINE, six printable entries; §13.18.14.4
      *> GR1/GR2 place each field's leftmost character at its COLUMN.
      *>   cols  1/ 6/11  PIC X(3) VALUE "AAA" "BBB" "CCC"  — GR23 sentence 1: AAA, BBB, CCC in that order.
      *>   cols 16/21     PIC X(3) SOURCES ARE WS-P WS-Q    — §13.18.53.4 GR4, the same rule for SOURCE: PPP, QQQ.
      *>   col  26        PIC XXBXX VALUE "AB CD"           — SR34→SR11 / GR21→GR7: stored as written, `AB CD`
      *>                                                      (the MOVE would edit it to `AB  C`).
      *>   col  34        PIC X(5) JUSTIFIED VALUE "AB"     — GR7: JUSTIFIED does not affect initialization, so
      *>                                                      `AB   ` (the MOVE would right-justify to `   AB`).
      *>   col  40        PIC ZZZ9 BLANK WHEN ZERO
      *>                           VALUE "0000"             — GR8: an alphanumeric literal, BWZ has no effect, so
      *>                                                      `0000` (the MOVE would blank it).
      *>   col  46        PIC ZZ9  VALUE 12                 — SR11 sentence 1 / SR6: a numeric literal into a
      *>                                                      numeric-edited item IS edited, so ` 12`.
      *> The check paragraph reads the report file back and displays the windows, then displays the five
      *> working-storage twins, which §13.18.63.4 GR7/GR8 and §13.18.63.3 SR11 give the same values.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB506RVL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb506rvl.txt".
           SELECT CHK ASSIGN TO "pb506rvl.txt"
               ORGANIZATION IS LINE SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-VL.
       FD  CHK.
       01  CHK-REC PIC X(60).
       WORKING-STORAGE SECTION.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-P    PIC X(3)  VALUE "PPP".
       01  WS-Q    PIC X(3)  VALUE "QQQ".
      *> The working-storage twins of the five non-list printable items (format 1 of the same clause).
       01  WS-AE   PIC XXBXX VALUE "AB CD".
       01  WS-J    PIC X(5)  JUSTIFIED RIGHT VALUE "AB".
       01  WS-BZ   PIC ZZZ9  BLANK WHEN ZERO VALUE "0000".
       01  WS-NE   PIC ZZ9   VALUE 12.
       REPORT SECTION.
       RD  R-VL PAGE LIMIT 20 LINES.
       01  DET TYPE DE LINE PLUS 1.
           03  COLUMNS ARE 1 6 11 PIC X(3) VALUES ARE "AAA" "BBB" "CCC".
           03  COLUMNS ARE 16 21  PIC X(3) SOURCES ARE WS-P WS-Q.
           03  COLUMN 26 PIC XXBXX VALUE "AB CD".
           03  COLUMN 34 PIC X(5) JUSTIFIED RIGHT VALUE "AB".
           03  COLUMN 40 PIC ZZZ9 BLANK WHEN ZERO VALUE "0000".
           03  COLUMN 46 PIC ZZ9 VALUE 12.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT PRT.
           INITIATE R-VL.
           GENERATE DET.
           TERMINATE R-VL.
           CLOSE PRT.
           OPEN INPUT CHK.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END PERFORM SHOW-LINE
               END-READ
           END-PERFORM.
           CLOSE CHK.
           DISPLAY "WS AE=[" WS-AE "] J=[" WS-J "] BZ=[" WS-BZ
               "] NE=[" WS-NE "]".
           STOP RUN.
       SHOW-LINE.
           IF CHK-REC NOT = SPACES
               DISPLAY "V=[" CHK-REC(1:3) "][" CHK-REC(6:3) "]["
                   CHK-REC(11:3) "]"
               DISPLAY "S=[" CHK-REC(16:3) "][" CHK-REC(21:3) "]"
               DISPLAY "E=[" CHK-REC(26:5) "] J=[" CHK-REC(34:5)
                   "] BZ=[" CHK-REC(40:4) "] NE=[" CHK-REC(46:3) "]"
           END-IF.
