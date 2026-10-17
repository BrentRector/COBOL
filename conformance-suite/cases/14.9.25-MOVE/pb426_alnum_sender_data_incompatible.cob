*>--- conformance-case
*> id: pb426_alnum_sender_data_incompatible
*> rules: [GR-14.9.25.4-6]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.25.4 GR6 d) 1 — THE POSITIVE CONTROL FOR THE ALPHANUMERIC SENDER'S EXCEPTION CONDITION,
      *> at the earliest edition that has the checking model it needs (>>TURN is COBOL-2002, §7.3.25 —
      *> negative/pb844-ec-data-incompatible-turn-below-2002 pins its refusal below that).
      *>
      *> THE RULES, --check validated:
      *>   cite.py --check 14.9.25.4 "Otherwise, if the content of the sending operand would result in a
      *>     false value in a numeric class condition, the EC-DATA-INCOMPATIBLE exception condition is set
      *>     to exist" -> OK §14.9.25.4 6) 1.
      *>   cite.py --check 8.8.4.4.4 "If the category of the data item referenced by identifier-1 is not
      *>     numeric, the condition is true if the content of the data item referenced by identifier-1
      *>     consists entirely of the characters" -> OK §8.8.4.4.4 3) n) 2 — the class condition 14.9.25.4 GR6 d) 1
      *>     defers to, and therefore the exact reason every sender in this program is all digits.
      *>
      *> WHY EACH LEG CAN FAIL.  EC-DATA-INCOMPATIBLE CHECKING is ON for the whole program and every
      *> sending operand below consists entirely of the characters 0-9, so §8.8.4.4.4 GR3 n) 2 makes each
      *> content TRUE in a numeric class condition and GR6 d) 1 sets NO condition: the run completes and
      *> FUNCTION EXCEPTION-STATUS stays spaces.
      *>   1  MOVE "12" TO PIC 9(3) -> 012.  The shape kb/Work PB844 names as the positive beside its
      *>      negative: the move is valid (§14.9.25.3 Table 16) and nothing is raised.
      *>   2  A40 -> PIC 9(9) -> 234567890.  FORTY character positions, every one a digit: the class
      *>      condition is over the WHOLE content, so an implementation that tested only the rightmost 31
      *>      would pass this leg — and one that raised merely because the operand is over-long, or because
      *>      it is alphanumeric at all, fails it.  The value is still §14.9.25.4 GR6 d) 3's windowed one.
      *>   3  The same into PIC ZZZ9 -> 7890, the numeric-EDITED receiving arm of GR6 d) under checking.
      *>   4  FUNCTION EXCEPTION-STATUS is spaces — the standing evidence that no condition was set to
      *>      exist by any leg above.  Without it a silent raise-and-ignore would read as a pass.
      *> The RAISING half of GR6 d) 1 is MoveAlphanumericSenderTests: EC-DATA-INCOMPATIBLE is fatal
      *> (§14.6.13.1.1 Table 13), so the run unit terminates abnormally and no .out can record it.
       >>TURN EC-DATA-INCOMPATIBLE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB426E2002.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A40 PIC X(40) VALUE "1234567890123456789012345678901234567890".
       01 N3  PIC 9(3).
       01 R9  PIC 9(9).
       01 NE  PIC ZZZ9.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "12" TO N3
           DISPLAY "1-LIT-TO-3=" N3
           MOVE A40 TO R9
           DISPLAY "2-A40-TO-9=" R9
           MOVE A40 TO NE
           DISPLAY "3-A40-TO-EDITED=" NE
           IF FUNCTION EXCEPTION-STATUS = SPACE
               DISPLAY "4-STATUS=NONE"
           ELSE
               DISPLAY "4-STATUS=" FUNCTION EXCEPTION-STATUS
           END-IF
           STOP RUN.
