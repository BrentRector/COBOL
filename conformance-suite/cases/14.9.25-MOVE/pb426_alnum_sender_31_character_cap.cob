      *>--- conformance-case
      *> id: pb426_alnum_sender_31_character_cap
      *> rules: [GR-14.9.25.4-6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.25.4 GR6 d) 3 — THE SIZE OF AN ALPHANUMERIC SENDING OPERAND MOVING TO A NUMERIC
      *> RECEIVER, at the earliest edition COBOL.NET compiles. The rule is not edition-gated: it reads
      *> identically at 85, 2002, 2014 and 2023, and the 2002 golden pb426_alnum_sender_data_incompatible
      *> carries the legs that need a 19-31 digit receiver or the >>TURN checking model (both 2002+).
      *>
      *> THE RULE, --check validated:
      *>   cite.py --check 14.9.25.4 "in which case the rightmost 31 character positions are used"
      *>     -> OK §14.9.25.4 6) 3.  "a. If the sending operand is a data item, the number of digits is
      *>        the number of character positions in the sending data item unless the number of character
      *>        positions is greater than 31, in which case the rightmost 31 character positions are used."
      *>   cite.py --check 14.9.25.4 "only the rightmost 31 characters in the literal are used"
      *>     -> OK §14.9.25.4 6) 3.  (sub-rule c, the LITERAL twin of the same window)
      *>   cite.py --check 14.6.8.2 "aligned by decimal point"
      *>     -> OK §14.6.8.2 4)  "If the receiving operand is a fixed-point numeric item, the data is
      *>        aligned by decimal point and is transferred to the receiving digits with zero fill or
      *>        truncation on either end as required."
      *>   cite.py --check 14.9.48.4 "shall be moved into the current receiving area according to the
      *>     rules for the MOVE statement" -> OK §14.9.48.4 11) c)
      *>   (⚠ cite.py prints the approximate path "6) 3." for both MOVE sub-rules; the PRINTED rule numbers
      *>    are 6) d) 3) a. and 6) d) 3) c.  The CLAUSE and the TEXT are what the check establishes.)
      *>
      *> WHY EACH LEG CAN FAIL — the window is over CHARACTER POSITIONS, and it is a CAP, not a hope:
      *>   1  A40 -> PIC 9(9).  The operand is A40's rightmost 31 character positions,
      *>      "0123456789012345678901234567890" = 123456789012345678901234567890; §14.6.8.2 GR4 aligns it by
      *>      decimal point into nine digit positions, truncating high-order -> 234567890. Before kb/Work
      *>      PB426 the decode consumed all FORTY positions into a signed Int128 and WRAPPED: 838277934.
      *>   2  A40 -> PIC 9(18).  The same operand, eighteen digit positions -> 345678901234567890.
      *>   3  S40 -> PIC 9(9).  S40's rightmost 31 character positions hold only the four digits "1234", so
      *>      the operand is 1234 -> 000001234.  Reading all forty positions instead would give 9876543211234
      *>      -> 543211234.  This leg is what makes the CAP itself observable at nine digit positions: it
      *>      fails for an implementation with no cap, AND for one that caps at 31 DIGIT characters rather
      *>      than 31 character positions (S40 holds only 13 digits, so a digit cap never fires) -> 543211234.
      *>   4  A35 -> PIC 9(9).  Thirty-five characters is over the cap too: the window drops the leading
      *>      four, "5678901234567890123456789012345" -> 789012345.  A31 is exactly AT the cap and is
      *>      therefore unwindowed, "1234567890123456789012345678901" -> 345678901 — the control that fails
      *>      an off-by-one window (a rightmost-30 window would answer 456789012 there).
      *>   5  A40 -> PIC ZZZZZZZZ9.  The numeric-edited receiving arm is a SECOND emit site for the same
      *>      rule (§14.9.25.3 Table 16 admits alphanumeric -> numeric-edited); it carried its own uncapped
      *>      decode.  The edited image of 234567890 suppresses no zero (its high-order digit is 2).
      *>   6  S40 (1:40) -> PIC 9(9).  A reference-modified result is category alphanumeric (§8.4.3.3.4
      *>      GR6), so it is an alphanumeric SENDING OPERAND and takes the same window -> 000001234.
      *>   7  UNSTRING ... INTO PIC 9(9).  §14.9.48.4 GR11 c) transfers the examined characters "according
      *>      to the rules for the MOVE statement", so the window applies there too -> 000001234.
      *>
      *> NOTE Legs 3, 6 and 7 send content that is not all digits. At COBOL-85 no checking can be enabled
      *> (>>TURN is COBOL-2002 — negative/pb844-ec-data-incompatible-turn-below-2002), so §14.9.25.4 GR6 d) 1
      *> sets no condition here and the deterministic decode stands; the 2002 golden measures the checked
      *> reading.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB426C85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A40 PIC X(40) VALUE "1234567890123456789012345678901234567890".
       01 A35 PIC X(35) VALUE "12345678901234567890123456789012345".
       01 A31 PIC X(31) VALUE "1234567890123456789012345678901".
       01 S40 PIC X(40) VALUE "987654321AAAAAAAAAAAAAAAAAAAAAAAAAAA1234".
       01 USRC PIC X(41) VALUE "987654321AAAAAAAAAAAAAAAAAAAAAAAAAAA1234,".
       01 R9  PIC 9(9).
       01 R18 PIC 9(18).
       01 NE  PIC ZZZZZZZZ9.
       PROCEDURE DIVISION.
       MAIN.
           MOVE A40 TO R9
           DISPLAY "1-A40-TO-9=" R9
           MOVE A40 TO R18
           DISPLAY "2-A40-TO-18=" R18
           MOVE S40 TO R9
           DISPLAY "3-S40-TO-9=" R9
           MOVE A35 TO R9
           DISPLAY "4-A35-TO-9=" R9
           MOVE A31 TO R9
           DISPLAY "4-A31-TO-9=" R9
           MOVE A40 TO NE
           DISPLAY "5-A40-TO-EDITED=" NE
           MOVE S40 (1:40) TO R9
           DISPLAY "6-REFMOD-TO-9=" R9
           MOVE ZERO TO R9
           UNSTRING USRC DELIMITED BY "," INTO R9
           DISPLAY "7-UNSTRING-TO-9=" R9
           STOP RUN.
