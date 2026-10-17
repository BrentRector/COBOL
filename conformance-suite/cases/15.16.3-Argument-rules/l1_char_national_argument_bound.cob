*>--- conformance-case
*> id: l1_char_national_argument_bound
*> rules: [AR-15.16.3-2, DOC-A.1-90]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.16.3 r2 FUNCTION CHAR-NATIONAL - "The value of argument-1 shall be greater than zero and less
      *> than or equal to the number of positions in the national program collating sequence." This is the
      *> NATIVE arm; the non-native (ALPHABET ... FOR NATIONAL) arm is a SEPARATE runtime body and is pinned by
      *> l1_char_national_also_positions - one rule, two dispatch arms, both measured.
      *>
      *> THE BOUND. No PROGRAM COLLATING SEQUENCE is declared, and §12.3.6.4 GR10 says that then "the initial
      *> program collating sequences are the native alphanumeric collating sequence and the native national
      *> collating sequence". The native national character set is the 65 536 UTF-16
      *> code units, one code unit per character position - §8.5.1.4: "Each two-octet code element of UTF-16 is
      *> treated in COBOL as though it were itself a character" (the CONFORMANCE.md item 188 determination) - so
      *> the sequence has 65 536 positions and §15.16.3 r2's window is 1 .. 65 536.
      *> §15.16.4 r1 puts the character of ordinal position n at code unit n-1.
      *>
      *> THE ORACLE. FUNCTION DISPLAY-OF is the alphanumeric<->national character correspondence, not a
      *> collating sequence (§15.26.4 r1; CONFORMANCE.md item 33 - the total UTF-16 identity), and with no
      *> ALPHABET the alphanumeric program collating sequence is native (§12.3.6.4 GR10 again), so by §15.70.4 r1
      *> read with §15.70.1's "The lowest ordinal position is 1", FUNCTION ORD of the converted character is its
      *> code unit + 1. Nothing wide is ever DISPLAYed.
      *>
      *> LOW1  - r2 lower bound, INSIDE: 1 is greater than zero, so position 1 = U+0000 -> ORD 1.
      *> MID66 - r1 control inside the window: position 66 = U+0041 "A" -> ORD 66.
      *> HIGH  - r2 upper bound, INSIDE: 65 536 is the last position = U+FFFF -> ORD 65536.
      *> OVER  - r2 upper bound, OUTSIDE: 65 537 exceeds the number of positions, so §15.3 sets
      *>         EC-ARGUMENT-FUNCTION and, checking being disabled, "the implementor defines the result of the
      *>         function reference" (Annex A.1 item 90). COBOL.NET's determination is the ZERO VALUE OF THE
      *>         RETURNED TYPE - numeric 0, alphanumeric/national spaces - which for the one-character
      *>         CHAR-NATIONAL is ONE NATIONAL SPACE, recorded at CONFORMANCE.md §7 item 90 -> ORD 33.
      *>         The load-bearing half of this leg is not the 33 but that it is NOT 65537's character.
      *> ZERO  - r2 lower bound, OUTSIDE: 0 is not greater than zero -> the same item-90 default, 33.
      *> WRAP  - THE DISCRIMINATOR. §15.3's screen has to see the argument's TRUE value, so it must sit above
      *>         any narrowing of the wide argument carrier. P * 100 + 52 = 18 446 744 073 709 617 152 =
      *>         2**64 + 65 536, whose low 64 bits are exactly 65 536 - the LEGAL maximum of the native window.
      *>         A guard placed after a modulo-2**64 narrowing would therefore accept it and answer 65536 (the
      *>         character U+FFFF) for an argument nineteen orders of magnitude outside the sequence. The
      *>         required answer is the §15.3 / item-90 default, 33, because 2**64 + 65 536 is not "less than or
      *>         equal to the number of positions in the national program collating sequence".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1CNBND.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P      PIC 9(18) VALUE 184467440737096171.
       01 N-X    PIC N.
       01 A-X    PIC X.
       01 ORD-R  PIC 9(5).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION CHAR-NATIONAL(1) TO N-X.
           MOVE FUNCTION DISPLAY-OF(N-X) TO A-X.
           COMPUTE ORD-R = FUNCTION ORD(A-X).
           DISPLAY "LOW1=" ORD-R.
           MOVE FUNCTION CHAR-NATIONAL(66) TO N-X.
           MOVE FUNCTION DISPLAY-OF(N-X) TO A-X.
           COMPUTE ORD-R = FUNCTION ORD(A-X).
           DISPLAY "MID66=" ORD-R.
           MOVE FUNCTION CHAR-NATIONAL(65536) TO N-X.
           MOVE FUNCTION DISPLAY-OF(N-X) TO A-X.
           COMPUTE ORD-R = FUNCTION ORD(A-X).
           DISPLAY "HIGH=" ORD-R.
           MOVE FUNCTION CHAR-NATIONAL(65537) TO N-X.
           MOVE FUNCTION DISPLAY-OF(N-X) TO A-X.
           COMPUTE ORD-R = FUNCTION ORD(A-X).
           DISPLAY "OVER=" ORD-R.
           MOVE FUNCTION CHAR-NATIONAL(0) TO N-X.
           MOVE FUNCTION DISPLAY-OF(N-X) TO A-X.
           COMPUTE ORD-R = FUNCTION ORD(A-X).
           DISPLAY "ZERO=" ORD-R.
           MOVE FUNCTION CHAR-NATIONAL(P * 100 + 52) TO N-X.
           MOVE FUNCTION DISPLAY-OF(N-X) TO A-X.
           COMPUTE ORD-R = FUNCTION ORD(A-X).
           DISPLAY "WRAP=" ORD-R.
           STOP RUN.
