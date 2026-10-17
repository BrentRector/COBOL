      *>--- conformance-case
      *> id: pb1557_full_alphabet_ordinals_85
      *> rules: [SR-12.3.7.3-14, GR-12.3.7.4-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1557 - an ALPHABET literal phrase that names EVERY
      *> native character, used as the program collating sequence.
      *>
      *> ISO 12.3.7.3 SR14 b4 (cite.py --check OK 12.3.7.3 14)): "The
      *> number of characters specified shall not exceed the number of
      *> characters in the native alphanumeric character set" - EQUAL is
      *> legal. The native set is the 65,536 UTF-16 code units (D-N1),
      *> ordinal n = code unit n-1 (DOC-A.1-8), so 65536 THRU 1 names
      *> every character, descending (12.3.7.4 GR7 k5 - "may specify
      *> characters of the native character set in either ascending or
      *> descending sequence"; cite.py OK 12.3.7.4 7) 2.). GR7 k2 ("The
      *> order in which the literals appear ... specifies, in ascending
      *> sequence, the ordinal number of the character") puts native
      *> ordinal n at position 65537-n.
      *>
      *> WHY EACH LEG CAN FAIL: the binder counted the positions in a
      *> 16-bit counter, which wrapped 65,536 to 0, so the table claimed
      *> an EMPTY specified block. CHAR then read every position as an
      *> "unspecified" one and SYMBOLIC CHARACTERS ... IN found no
      *> ordinal at all. Before the fix this program was REJECTED
      *> (COBOLNET1670 on the SYMBOLIC CHARACTERS clause); without that
      *> clause it ran and printed CHAR-65471=" " and RT66=65504.
      *>
      *>   ORD("A")   (15.70.1 - "the ordinal position of argument-1 in
      *>              the program collating sequence"): "A" = native
      *>              ordinal 66 -> 65537-66           -> ORD-A=65471
      *>   CHAR(65471) (15.15.4 1) - "the character ... having the
      *>              ordinal position specified")      -> CHAR-65471=A
      *>   ORD(CHAR(66)) - the round trip              -> RT66=00066
      *>   S66 IS 66 IN AREV (12.3.7.4 GR11 b - "the coded character at
      *>              ordinal position integer-1" of AREV's set, whose
      *>              ordinals are its collating positions, DOC-A.1-186):
      *>              native ordinal 65471 (U+FFBE); its position under
      *>              AREV is 66                         -> ORD-S66=00066
      *>   "B" < "A" under AREV (8.8.4.2.7), native says N -> B-LT-A=Y
      *>   HIGH-VALUE (12.3.7.4 GR8 - "The character that has the highest
      *>              ordinal position in the program collating sequence")
      *>              = U+0000, the last position        -> ORD-HV=65536
      *>   LOW-VALUE (GR9) = U+FFFF, position 1         -> ORD-LV=00001
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1557F85.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. X PROGRAM COLLATING SEQUENCE IS AREV.
       SPECIAL-NAMES.
           ALPHABET AREV IS 65536 THRU 1
           SYMBOLIC CHARACTERS S66 IS 66 IN AREV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R    PIC 9(5).
       01 W    PIC X.
       01 W-A  PIC X VALUE "A".
       01 W-B  PIC X VALUE "B".
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = FUNCTION ORD("A")
           DISPLAY "ORD-A=" R
           MOVE FUNCTION CHAR(65471) TO W
           DISPLAY "CHAR-65471=" W
           COMPUTE R = FUNCTION ORD(FUNCTION CHAR(66))
           DISPLAY "RT66=" R
           MOVE S66 TO W
           COMPUTE R = FUNCTION ORD(W)
           DISPLAY "ORD-S66=" R
           IF W-B < W-A
               DISPLAY "B-LT-A=Y"
           ELSE
               DISPLAY "B-LT-A=N"
           END-IF
           MOVE HIGH-VALUE TO W
           COMPUTE R = FUNCTION ORD(W)
           DISPLAY "ORD-HV=" R
           MOVE LOW-VALUE TO W
           COMPUTE R = FUNCTION ORD(W)
           DISPLAY "ORD-LV=" R
           STOP RUN.
