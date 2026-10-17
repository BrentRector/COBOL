*>--- conformance-case
*> id: pb565_report_vertical_repetition
*> rules: [GR-13.15.4-3, SR-13.18.35.3-10, GR-13.18.35.4-9, SR-13.18.38.3-25, SR-13.18.38.3-26, GR-13.18.38.4-10, GR-13.18.38.4-11, GR-13.18.38.4-12, GR-13.18.38.4-13, GR-13.18.63.4-22]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ⛔ A REPORT GROUP DESCRIPTION ENTRY REPEATS ON EITHER AXIS, AND THE VERTICAL ONE IS THE LINE CLAUSE
      *> (kb/Work PB565). The horizontal (COLUMN) half landed first; an OCCURS clause on an entry that contains
      *> or has subordinate to it a LINE clause was still refused by name (COBOLNET0899), and so was its twin
      *> the multiple LINE clause — conforming source rejected, with §13.18.35.4 GR9 and §13.18.38.4
      *> GR10c/GR10d/GR12c/GR12d unreachable. This golden pins all four, plus the multiple LINE clause, at the
      *> edition that introduced them.
      *>
      *> THE TWO VEHICLES ARE ONE. §13.18.35.4 GR9: "A multiple LINE clause is functionally equivalent to a LINE
      *> clause with a single operand, together with a simple OCCURS clause whose integer is equal to the number
      *> of operands of the LINE clause, except that the multiple LINE clause allows the report lines to be
      *> defined at unequal vertical intervals." So both drive the SAME subtree replay.
      *>
      *> DERIVATION OF THE EXPECTED OUTPUT, line by line. The report has no heading groups and DET-V is the
      *> chronologically first body group since the INITIATE, so no page fit test takes place (§13.18.35.4 GR4)
      *> and the group's first line number is given by §13.18.35.4 GR5a — the first LINE clause is absolute, so
      *> integer-1, line 2. Every subsequent line follows GR7: absolute -> integer-1, relative -> LINE-COUNTER
      *> + integer-2. §13.18.35.3 SR6 orders the group: the absolute lines are ascending and none follows a
      *> relative one.
      *>
      *> LINES 2, 4, 7 — THE MULTIPLE LINE CLAUSE. Three operands, so GR9 makes the entry a repeating entry of
      *> three repetitions at unequal intervals. Its subordinate printable item carries a three-operand VALUE
      *> clause, which §13.18.63.3 SR35 admits because the entry "shall be subordinate to a repeating entry"
      *> with a matching operand count, and §13.18.63.4 GR23 assigns "successive operands … to successive
      *> repeating printable items" => X on line 2, Y on line 4, Z on line 7.
      *>
      *> LINES 9, 11, 14 — A MULTIPLE LINE CLAUSE CARRYING THE PRINTABLE ITEM ITSELF, with a VARYING counter.
      *> §13.18.64.3 SR1 names the multiple LINE clause as a VARYING vehicle, and §13.18.64.4 GR3 fixes the
      *> counter per repetition: FROM 2 BY 3 => 2, 5, 8, printed through PIC 9 at column 1.
      *>
      *> LINES 16, 18, 20 — GR12c OVER AN ABSOLUTE LINE. "If the entry contains a LINE clause, each successive
      *> occurrence is positioned integer-3 lines vertically beneath the preceding occurrence." LINE 16 OCCURS 3
      *> TIMES STEP 2 => 16, 18, 20. §13.18.38.3 SR25a REQUIRES the STEP phrase here: without it every
      *> repetition would print on line 16.
      *>
      *> LINES 21, 22 and 25, 26 — GR12d OVER A GROUP OF RELATIVE LINES. "If the entry is a group entry having
      *> subordinate entries with LINE clauses, report lines in successive occurrences are positioned integer-3
      *> lines vertically beneath the line they occupy in the preceding occurrence." The 02 repeats twice with
      *> STEP 4 over two LINE PLUS 1 entries: LINE-COUNTER is 20, so the first occurrence lands on 21 and 22;
      *> the second is each of those plus 4 — 25 and 26 — which is NOT LINE-COUNTER + 4, because the datum is
      *> the line the preceding OCCURRENCE occupied while LINE-COUNTER holds the last line PRINTED
      *> (§13.18.35.4 GR1).
      *>
      *> LINES 27, 28, 29 — GR10c WITH NO STEP PHRASE. "If the entry also contains a relative LINE clause, each
      *> repetition behaves as though it had the same relative LINE clause", and GR12's closing sentence makes
      *> the relative operand itself the interval: LINE PLUS 1 OCCURS 3 TIMES from LINE-COUNTER 26 => 27, 28,
      *> 29. §13.18.38.3 SR25 leaves the STEP phrase optional here, which is why its absence is the point.
      *>
      *> LINES 30, 31 — THE TWO AXES NESTED, AND §13.18.63.4 GR23's WRAP-AROUND. The outer 02 repeats twice
      *> vertically and its subordinate printable item three times horizontally, so the item has SIX placements
      *> and a three-operand VALUE clause. §13.18.63.3 SR35 admits the count — the operands equal "the number of
      *> repetitions of the repeating entry", 3, and the admissible counts are the prefix products 3 and 6 — and
      *> GR23's closing sentence then governs the rest: "If no further operands remain, assignment begins again
      *> from the first operand." So both lines print A, B, C at columns 1, 3, 5 (STEP 2 over PIC X(1)), the
      *> second line by wrapping. This is the shape that makes that sentence REACHABLE at all: it needs a
      *> repeating entry subordinate to another one, which needed both axes.
      *>
      *> THE READ-BACK IS BYTE-WISE AND NUMBERS THE LINES. A report file's lines are newline-delimited and
      *> ORGANIZATION IS LINE SEQUENTIAL is a COBOL-2023 introduction (§12.4.5.10.3 GR2), so a one-character
      *> record on a second SELECT over the same file reassembles them at every edition. The LINE NUMBER is
      *> what this golden is about, so each printed line is shown with the page line it landed on; the blank
      *> lines between them are §13.18.35.4 GR7's "Any unoccupied lines on the page result in a blank line".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB565RVR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb565rvr.txt".
           SELECT CHK ASSIGN TO "pb565rvr.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-VR.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF  PIC X VALUE "N".
       01  WS-I    PIC 99 VALUE 0.
       01  WS-L    PIC 99 VALUE 1.
       01  WS-LINE PIC X(30) VALUE SPACES.
       REPORT SECTION.
       RD  R-VR PAGE LIMIT 35 LINES.
       01  DET-V TYPE DE.
           02  LINE 2 4 7.
               03  COLUMN 1 PIC X(1) VALUE "X" "Y" "Z".
           02  LINE 9 11 14 COLUMN 1 PIC 9
               VARYING V FROM 2 BY 3 SOURCE V.
           02  LINE 16 OCCURS 3 TIMES STEP 2.
               03  COLUMN 1 PIC X(3) VALUE "AAA".
           02  OCCURS 2 TIMES STEP 4.
               03  LINE PLUS 1.
                   04  COLUMN 1 PIC X(1) VALUE "P".
               03  LINE PLUS 1.
                   04  COLUMN 1 PIC X(1) VALUE "Q".
           02  LINE PLUS 1 OCCURS 3 TIMES.
               03  COLUMN 1 PIC X(3) VALUE "BBB".
           02  LINE PLUS 1 OCCURS 2 TIMES.
               03  COLUMN 1 PIC X(1) OCCURS 3 TIMES STEP 2
                   VALUE "A" "B" "C".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT PRT.
           INITIATE R-VR.
           GENERATE DET-V.
           TERMINATE R-VR.
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
               ADD 1 TO WS-L
           ELSE
               IF CHK-REC NOT = X"0D"
                   ADD 1 TO WS-I
                   MOVE CHK-REC TO WS-LINE(WS-I:1)
               END-IF
           END-IF.
       SHOW-LINE.
           IF WS-I > 0
               DISPLAY WS-L " [" WS-LINE(1:WS-I) "]"
               MOVE SPACES TO WS-LINE
               MOVE 0 TO WS-I
           END-IF.
