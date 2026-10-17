      *>--- conformance-case
      *> id: pb429_page_counter_receiving
      *> rules: [SR-8.4.3.1.3-12, SR-8.4.3.15.3-1, SR-14.9.25.3-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ⛔ PAGE-COUNTER IS A LEGAL RECEIVING OPERAND, IN EVERY CONTEXT THAT ADMITS AN INTEGER DATA ITEM
      *> (kb/Work PB429). Before this, `MOVE 7 TO PAGE-COUNTER` drew COBOLNET0899 "PAGE-COUNTER as a
      *> receiving operand (legal) is not yet implemented" — the compiler refusing a program while conceding
      *> in its own diagnostic that the program was right.
      *>
      *> THE RULE. §8.4.3.15.3 SR1: "In the report section, PAGE-COUNTER and LINE-COUNTER may be referenced
      *> only in a SOURCE clause. In the procedure division, PAGE-COUNTER and LINE-COUNTER may be referenced
      *> in any context where an integer data item may appear." SR3 then subtracts one of them, and only one:
      *> "LINE-COUNTER shall not be referenced as a receiving operand." PAGE-COUNTER is therefore admitted as
      *> a receiving operand by the explicit construction of the two rules — and §13.18.37.4 GR6's
      *> parenthetical, "this phrase ensures that PAGE-COUNTER will be one, effective from the start of the
      *> next page (unless procedurally altered)", is the standard naming the very use. §8.4.3.15.4 GR1 makes
      *> the counters "temporary unsigned integer data items of class and category numeric", which is the
      *> receiving profile every store below goes through. The complement is
      *> conformance:cases/8.4.3.15-Report-counters/pb429-line-counter-receiving, which pins SR3 at every edition.
      *>
      *> SR1 NAMES NO STATEMENT, so this golden runs the COUNTER THROUGH THE CONTEXTS, not through MOVE:
      *> each line below is one context that admits an integer data item, and each expected value is the
      *> arithmetic of the statement itself. INITIATE sets PAGE-COUNTER to 1 (§14.9.21.4 GR1 c) and no page
      *> advance occurs here (one GENERATE on a 60-line page), so the counter's whole history is the program's.
      *>
      *>   MOVE 7 TO PAGE-COUNTER OF RPT1        -> 0007   (§14.9.25 — the qualified spelling, §8.4.3.15 SR2)
      *>   ADD 3 TO PAGE-COUNTER                 -> 0010
      *>   SUBTRACT 4 FROM PAGE-COUNTER          -> 0006
      *>   MULTIPLY 2 BY PAGE-COUNTER            -> 0012
      *>   DIVIDE 3 INTO PAGE-COUNTER            -> 0004   (truncation, §14.9.12.4 — 12 / 3 = 4 exactly)
      *>   COMPUTE PAGE-COUNTER = PAGE-COUNTER + 40 -> 0044  (it is a SENDING operand in the same statement)
      *>   ADD 1 2 GIVING PAGE-COUNTER           -> 0003   (a GIVING resultant)
      *>   INITIALIZE PAGE-COUNTER               -> 0000   (§14.9.20.4 GR6 c: the receiving operand is of
      *>                                                   category Numeric, so the sending operand is the
      *>                                                   figurative constant ZEROES)
      *>   INSPECT WT TALLYING PAGE-COUNTER FOR ALL "X" -> 0002  ("AXBXC" holds two X's; §14.9.22.4
      *>                                                   GR12 a: "the content of the data item
      *>                                                   referenced by identifier-2 is incremented by
      *>                                                   one for each occurrence of literal-1 matched",
      *>                                                   and it starts at 0 from the INITIALIZE above)
      *>   UNSTRING ... WITH POINTER PAGE-COUNTER -> 0007  (§14.9.48.4 GR13: the pointer is "incremented by
      *>                                                   one for each character examined"; it starts at 1,
      *>                                                   "AA," and "BB," are 6 characters, so it ends at 7)
      *>
      *> The report itself is incidental: it exists so the program HAS a PAGE-COUNTER (§8.4.3.15.1 — "generated
      *> automatically and exist independently for each report"), and one GENERATE proves the engine still runs
      *> after the counter has been assigned.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB429PCR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb429pcr.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS RPT1.
       WORKING-STORAGE SECTION.
       01  W      PIC 9999 VALUE 0.
       01  WT     PIC X(5) VALUE "AXBXC".
       01  WS-SRC PIC X(11) VALUE "AA,BB,CC,DD".
       01  WS-A   PIC X(2).
       01  WS-B   PIC X(2).
       REPORT SECTION.
       RD  RPT1 PAGE LIMIT 60 LINES.
       01  DETAIL-LINE TYPE IS DETAIL LINE PLUS 1.
           02  COLUMN 1 PIC X(3) VALUE "ABC".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT.
           INITIATE RPT1.
           GENERATE DETAIL-LINE.
           MOVE 7 TO PAGE-COUNTER OF RPT1.
           MOVE PAGE-COUNTER TO W. DISPLAY "MOVE=" W.
           ADD 3 TO PAGE-COUNTER.
           MOVE PAGE-COUNTER TO W. DISPLAY "ADD=" W.
           SUBTRACT 4 FROM PAGE-COUNTER.
           MOVE PAGE-COUNTER TO W. DISPLAY "SUB=" W.
           MULTIPLY 2 BY PAGE-COUNTER.
           MOVE PAGE-COUNTER TO W. DISPLAY "MUL=" W.
           DIVIDE 3 INTO PAGE-COUNTER.
           MOVE PAGE-COUNTER TO W. DISPLAY "DIV=" W.
           COMPUTE PAGE-COUNTER = PAGE-COUNTER + 40.
           MOVE PAGE-COUNTER TO W. DISPLAY "COMPUTE=" W.
           ADD 1 2 GIVING PAGE-COUNTER.
           MOVE PAGE-COUNTER TO W. DISPLAY "GIVING=" W.
           INITIALIZE PAGE-COUNTER.
           MOVE PAGE-COUNTER TO W. DISPLAY "INITIALIZE=" W.
           INSPECT WT TALLYING PAGE-COUNTER FOR ALL "X".
           MOVE PAGE-COUNTER TO W. DISPLAY "TALLY=" W.
           MOVE 1 TO PAGE-COUNTER.
           UNSTRING WS-SRC DELIMITED BY ","
               INTO WS-A WS-B
               WITH POINTER PAGE-COUNTER.
           MOVE PAGE-COUNTER TO W. DISPLAY "POINTER=" W.
           TERMINATE RPT1.
           CLOSE RPT.
           STOP RUN.
