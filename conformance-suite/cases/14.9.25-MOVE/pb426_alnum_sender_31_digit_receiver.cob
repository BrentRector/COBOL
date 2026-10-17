*>--- conformance-case
*> id: pb426_alnum_sender_31_digit_receiver
*> rules: [GR-14.9.25.4-6]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.25.4 GR6 d) 3 — THE SIZE RULE SEEN WHOLE, at the earliest edition with a receiver wide
      *> enough to hold the entire windowed operand (a 19-31 digit fixed-point item is COBOL-2002,
      *> §8.3.3.3.2; at COBOL-85 the eighteen-digit ceiling truncates the window before it can be read
      *> back, which is why 85/pb426_alnum_sender_31_character_cap measures the rule a different way).
      *> Checking is DELIBERATELY not enabled here — leg 2's sender is not all digits, and the condition
      *> that would raise for it is the subject of the sibling golden
      *> 2002/pb426_alnum_sender_data_incompatible.
      *>
      *> THE RULES, --check validated:
      *>   cite.py --check 14.9.25.4 "When the sending operand is described as alphanumeric or national,
      *>     the sending operand is treated as if it were an unsigned integer of category numeric"
      *>     -> OK §14.9.25.4 6) 3.
      *>   cite.py --check 14.9.25.4 "in which case the rightmost 31 character positions are used"
      *>     -> OK §14.9.25.4 6) 3.  (sub-rule a — the DATA ITEM size rule)
      *>   cite.py --check 14.9.25.4 "only the rightmost 31 characters in the literal are used"
      *>     -> OK §14.9.25.4 6) 3.  (sub-rule c — the LITERAL size rule, leg 3)
      *>
      *> WHY EACH LEG CAN FAIL:
      *>   1  A40 -> PIC 9(31).  The receiver holds the whole windowed operand, so the window is visible in
      *>      full: A40's rightmost 31 character positions are "0123456789012345678901234567890".  Before
      *>      kb/Work PB426 the decode read all forty positions into a signed Int128, WRAPPED, and stored
      *>      7560297064841152750825838277934 — the low-order 31 digits of the two's-complement value.
      *>   2  EMB -> PIC 9(31).  THE DISCRIMINATOR BETWEEN THE TWO READINGS OF THE CAP.  EMB holds 34
      *>      character positions of which 29 are digits.  The rule caps CHARACTER POSITIONS, so the
      *>      operand is EMB's rightmost 31 positions, "456789012345678901234567890ABCD", whose digits are
      *>      the 27-digit 456789012345678901234567890 -> 0000456789012345678901234567890.  Capping at 31
      *>      DIGIT characters instead would never fire (there are only 29) and would answer
      *>      0012456789012345678901234567890.  §14.6.13.2 is what makes the two differ: a non-digit
      *>      position contributes no digit but still OCCUPIES a position.
      *>   3  A 40-character alphanumeric LITERAL -> PIC 9(31).  Sub-rule c is a separate sentence of the
      *>      standard from sub-rule a and a separate emit site in the compiler (the literal operand, not a
      *>      field read); it must window identically -> 0123456789012345678901234567890.
      *>   4  A31 -> PIC 9(31).  Exactly AT the cap, so unwindowed: the control that fails an off-by-one
      *>      window (a rightmost-30 window would drop its leading 1).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB426W2002.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A40 PIC X(40) VALUE "1234567890123456789012345678901234567890".
       01 A31 PIC X(31) VALUE "1234567890123456789012345678901".
       01 EMB PIC X(34) VALUE "12X456789012345678901234567890ABCD".
       01 R31 PIC 9(31).
       PROCEDURE DIVISION.
       MAIN.
           MOVE A40 TO R31
           DISPLAY "1-A40-TO-31=" R31
           MOVE EMB TO R31
           DISPLAY "2-EMB-TO-31=" R31
           MOVE "1234567890123456789012345678901234567890" TO R31
           DISPLAY "3-LIT-TO-31=" R31
           MOVE A31 TO R31
           DISPLAY "4-A31-TO-31=" R31
           STOP RUN.
