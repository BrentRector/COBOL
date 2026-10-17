*>--- conformance-case
*> id: pb584_mixed_bit_group_image
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB584 - A MIXED BIT/CHARACTER GROUP'S COMPILE-TIME IMAGE IS COMPOSED BY 8.5.1.6.3's RUNS,
      *> not member by member, and the SEED a REDEFINES alias starts from is the same composition the run-time
      *> AsImage() produces.  Every expected value below is computed from the rule text before the run.
      *>
      *> ISO 8.5.1.6.3 (Alignment of data items of usage bit) - alignment "is at the next bit position in
      *>   storage when that item is: - an elementary bit data item immediately following an elementary bit data
      *>   item or bit group item of the same level".  Two same-level bit members therefore SHARE a byte; an
      *>   item that is not a bit item goes at the first bit position of the first available byte, and implicit
      *>   filler bits are generated "as needed to increase the number of bits to fill an integral number of
      *>   characters".
      *> ISO 13.18.44.4 GR1 - "Storage association for the subject of the entry starts at the first bit of the
      *>   data item referenced by data-name-2 and continues over an area sufficient to contain the number of
      *>   bits required by the data item referenced by the subject of the entry."  So the alias sees exactly
      *>   the bytes the group lays out.
      *> ISO 8.3.3.6.4 GR4 - the zero format "represents the numeric value '0', one or more of the boolean
      *>   character '0', or one or more of the character '0' ... depending on context" (a bit leaf's VALUE ZERO
      *>   is boolean zeros, one per declared position).
      *> ISO 8.3.3.6.4 GR2 - a figurative constant in a VALUE clause has "the string of characters ... repeated
      *>   character by character until the size of the resultant string is greater than or equal to the number
      *>   of character positions in the associated data item" (ALL B"1" over four boolean positions is 1111).
      *> ISO 15.44.4 - FUNCTION ORD returns "the ordinal position of the argument in the collating sequence",
      *>   which is the byte value + 1; it is how this file reads the composed BYTES rather than trusting a
      *>   DISPLAY of a control character.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE SPEC:
      *>   MIX   H1=0100 H2=0001 (one run, bits 0..7 = 0100 0001 = 0x41) H3="B" = 0x42.
      *>         ORD of the alias bytes: 65+1=66 and 66+1=67.   BEFORE: 0x40 and 0x10, H2 read 0000 and the
      *>         character member was lost - each bit member had been padded to its OWN byte.
      *>   CTRL  the byte-identical group WITHOUT the alias - the record-struct lane, which has always walked
      *>         the runs - must read the same members.  A control that drifts is the failure this file catches.
      *>   RT    MOVE "Ca" TO the alias then read the members: 0x43 = 0100 0011, so H1=0100 H2=0011 H3="a".
      *>         The SEED and the run-time image are one composition or this leg disagrees with MIX.
      *>   FIG   F1 VALUE ZERO = 0000, F2 VALUE ALL B"1" = 1111, byte 0 = 0000 1111 = 0x0F -> ORD 16;
      *>         F3 = "C" = 0x43 -> ORD 68.   BEFORE: the figurative seeded FOUR carrier characters where the
      *>         packed image is ONE byte, so the bytes were '0' (0x30) and '1' (0x31).
      *>   PURE  a PURE bit group behind an alias: K1 VALUE ALL B"1" = 1111, K2 VALUE ZERO = 0000, the packed
      *>         byte is 1111 0000 = 0xF0 -> ORD 241.   BEFORE: K1 came back 0000 through the alias and 1111
      *>         without one - the bit-carrier lane had no ALL-literal arm.
      *>   ROOT  a root-level bit leaf behind an alias: VALUE ZERO over four positions, packed with four filler
      *>         zero bits = 0x00 -> ORD 1.
      *>   RUN2  a THREE-member run crossing a byte: T1=1100 T2=0011 T3=1010 is 12 bits, ceil(12/8) = 2
      *>         characters, packed high-order first as 1100 0011 / 1010 0000 = 0xC3 0xA0 -> ORD 196 and 161;
      *>         T4 is not a bit item, so it starts at the first AVAILABLE byte, byte 2 = "D" = 0x44 -> ORD 69.
      *>
      *> EDITIONS: boolean data (the PICTURE symbol 1, USAGE BIT) and GROUP-USAGE are COBOL-2002 introductions,
      *> so 2002 is the introducing edition and this file lives here; the composition rule does not vary by
      *> edition, so the corpus keeps ONE copy (the negative below 2002 is
      *> tests/conformance/negative/pb584-mixed-bit-group-below-2002).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB584MIX02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  CTL.
           05  H1  PIC 1(4) USAGE BIT VALUE B"0100".
           05  H2  PIC 1(4) USAGE BIT VALUE B"0001".
           05  H3  PIC X(1) VALUE "B".
       01  CV  REDEFINES CTL PIC X(2).
       01  CTLC.
           05  C1  PIC 1(4) USAGE BIT VALUE B"0100".
           05  C2  PIC 1(4) USAGE BIT VALUE B"0001".
           05  C3  PIC X(1) VALUE "B".
       01  FIG.
           05  F1  PIC 1(4) USAGE BIT VALUE ZERO.
           05  F2  PIC 1(4) USAGE BIT VALUE ALL B"1".
           05  F3  PIC X(1) VALUE "C".
       01  FV  REDEFINES FIG PIC X(2).
       01  BG  GROUP-USAGE BIT.
           05  K1  PIC 1(4) VALUE ALL B"1".
           05  K2  PIC 1(4) VALUE ZERO.
       01  BV  REDEFINES BG PIC X(1).
       01  RB  PIC 1(4) USAGE BIT VALUE ZERO.
       01  RV  REDEFINES RB PIC X(1).
       01  RUNG.
           05  T1  PIC 1(4) USAGE BIT VALUE B"1100".
           05  T2  PIC 1(4) USAGE BIT VALUE B"0011".
           05  T3  PIC 1(4) USAGE BIT VALUE B"1010".
           05  T4  PIC X(1) VALUE "D".
       01  RUNV  REDEFINES RUNG PIC X(3).
       PROCEDURE DIVISION.
           DISPLAY "MIX=[" H1 "][" H2 "][" H3 "] "
               FUNCTION ORD (CV (1:1)) " " FUNCTION ORD (CV (2:1))
           DISPLAY "CTRL=[" C1 "][" C2 "][" C3 "]"
           MOVE "Ca" TO CV
           DISPLAY "RT=[" H1 "][" H2 "][" H3 "]"
           DISPLAY "FIG=[" F1 "][" F2 "][" F3 "] "
               FUNCTION ORD (FV (1:1)) " " FUNCTION ORD (FV (2:1))
           DISPLAY "PURE=[" K1 "][" K2 "] " FUNCTION ORD (BV (1:1))
           DISPLAY "ROOT=[" RB "] " FUNCTION ORD (RV (1:1))
           DISPLAY "RUN2=[" T1 "][" T2 "][" T3 "][" T4 "] "
               FUNCTION ORD (RUNV (1:1)) " " FUNCTION ORD (RUNV (2:1))
               " " FUNCTION ORD (RUNV (3:1))
           STOP RUN.
