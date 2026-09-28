      *> kb/Work PB1284 — ISO/IEC 1989:2023 §13.18.45.3 SR10: "The area described by data-name-2 THROUGH data-name-3
      *> shall define an integral number of bytes." §8.5.1.6.3 packs same-level USAGE BIT items into shared bytes,
      *> so A (3 bits) + B (5 bits) is ONE byte and C + D (4 + 4 bits) is the next: X1 = A THRU B and X2 = A THRU D
      *> are whole bytes (8 and 16 bits), so both entries are legal and the program compiles and runs. (A THRU C,
      *> 11 bits, is not — the negative pb1284-renames-bit-area-fractional.) This golden pins the ACCEPTANCE side of
      *> SR10 only: the aliases are not referenced, because the alias window over packed bit items is still
      *> measured in character positions (one per bit leaf), which is a separate recorded defect.
      *> Hand-derived stdout:
      *>   101 11001 0110 1111
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1284BW.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R.
          05 A PIC 1(3) USAGE BIT VALUE B"101".
          05 B PIC 1(5) USAGE BIT VALUE B"11001".
          05 C PIC 1(4) USAGE BIT VALUE B"0110".
          05 D PIC 1(4) USAGE BIT VALUE B"1111".
       66 X1 RENAMES A THRU B.
       66 X2 RENAMES A THRU D.
       PROCEDURE DIVISION.
           DISPLAY A " " B " " C " " D.
           STOP RUN.
