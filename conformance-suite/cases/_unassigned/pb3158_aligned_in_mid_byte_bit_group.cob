*>--- conformance-case
*> id: pb3158_aligned_in_mid_byte_bit_group
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3158 - an ALIGNED item (and every bit item that 8.5.1.6.3
      *> puts at "the first bit position of the first available byte") inside a
      *> GROUP-USAGE BIT group that itself begins in the MIDDLE of a byte is
      *> aligned on a byte boundary of the RECORD, not of the group.  Every value
      *> is derived from the standard, none read off a run.
      *>
      *> 8.5.1.6.3: a bit group item immediately following an elementary bit
      *> data item of the same level is "at the next bit position in storage", and
      *> "the alignment of the start of a group item and the alignment of the first
      *> item within that group, when the first item is a bit data item, are at the
      *> same bit position in storage".
      *> 13.18.1.4 GR1: an ALIGNED clause "causes the subject of the entry to be
      *> aligned on the first bit of the first available byte boundary" - a byte
      *> of the record that holds the group, which is bit 8 for the item below
      *> that follows a group begun at bit 2.
      *> 13.18.29.4 GR1b: the group is "treated as though it were an elementary
      *> data item ... described with PICTURE 1(m), where m is the bit length of
      *> the group"; 15.50.4 rule 1 gives that length in boolean positions and
      *> rule 5 counts the implicit filler; rule 9 rounds a partly used
      *> character up (BYTE-LENGTH, 15.14.4 rule 4, the same).
      *>
      *> LAYOUT 1 - BG, bit positions of the record (8 bits to a character):
      *>   BA   0-1        SUB   begins at bit 2 (shares BA's byte)
      *>   SA   2          TA    aligned: bits 8-12  (filler 3-7)
      *>   UA   13-15      CA    follows SUB, bits 16-17
      *> BG is 18 positions, SUB 14 (bits 2-15), BYTE-LENGTH(BG) = 3.
      *> BA=11 SA=1 TA=10101 UA=111 CA=01 give BG
      *>   11 1 00000 10101 111 01 = 111000001010111101
      *> and SUB = bits 2-15 = 10000010101111.
      *> LAYOUT 2 - CG is the same members in an ALPHANUMERIC group (no
      *> GROUP-USAGE clause, 13.18.29.4 GR3), followed by CZ PIC X, which starts
      *> the next character after the trailing filler (rule 3 of 8.5.1.6.3):
      *> bytes 11100000 10101111 01000000 'Z' = 224 175 64 90, ORD is one more:
      *> 225 176 65 91; LENGTH(CG) = 3 + 1 = 4.
      *> LAYOUT 3 - G3, an OCCURS DEPENDING table inside the group at bit 2:
      *>   TA(k) at bits 8+8(k-1) .. 12+8(k-1); LENGTH(G3) in positions:
      *>   N=1 13   N=2 21   N=3 29  (the last occurrence has no filler after it,
      *>   8.5.1.6.3), BYTE-LENGTH 2 3 4; the fixed twin FG with OCCURS 3 is 29.
      *>   BA=11 SA=1 TA(1..3)=10101 01010 11111 gives G3 at N=3
      *>   11 1 00000 10101 000 01010 000 11111.
      *> LAYOUT 4 - G4, the same members in a group with no GROUP-USAGE clause with N=2: 21
      *> positions, LENGTH 3, bytes 11100000 10101000 01010000 = 225 169 81 and
      *> a space (R4 is X(4)) 33 as ORD.
      *> LAYOUT 5 - G5, a group within a group within a group: M begins at bit 2,
      *> INN at bit 5 (3 positions into M), IT is aligned at bit 8:
      *>   BA 0-1  MA 2-4  IA 5  filler 6-7  IT 8-9      G5 = 10 positions
      *> BA=11 MA=101 IA=1 IT=01 give 11 101 1 00 01 = 1110110001, M = bits
      *> 2-9 = 10110001 (8 positions), BYTE-LENGTH(G5) = 2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3158AL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BG GROUP-USAGE BIT.
          05 BA PIC 1(2) USAGE BIT.
          05 SUB GROUP-USAGE BIT.
             10 SA PIC 1 USAGE BIT.
             10 TA PIC 1(5) USAGE BIT ALIGNED.
             10 UA PIC 1(3) USAGE BIT.
          05 CA PIC 1(2) USAGE BIT.
       01 CG.
          05 CBA PIC 1(2) USAGE BIT.
          05 CSUB GROUP-USAGE BIT.
             10 CSA PIC 1 USAGE BIT.
             10 CTA PIC 1(5) USAGE BIT ALIGNED.
             10 CUA PIC 1(3) USAGE BIT.
          05 CCA PIC 1(2) USAGE BIT.
          05 CZ PIC X.
       01 X14 PIC 1(14) USAGE BIT.
       01 R4 PIC X(4).
       01 N3 PIC 9.
       01 G3 GROUP-USAGE BIT.
          05 B3 PIC 1(2) USAGE BIT.
          05 S3 GROUP-USAGE BIT.
             10 A3 PIC 1 USAGE BIT.
             10 T3 PIC 1(5) USAGE BIT ALIGNED
                OCCURS 1 TO 3 DEPENDING ON N3.
       01 FG GROUP-USAGE BIT.
          05 FB PIC 1(2) USAGE BIT.
          05 FS GROUP-USAGE BIT.
             10 FA PIC 1 USAGE BIT.
             10 FT PIC 1(5) USAGE BIT ALIGNED OCCURS 3.
       01 N4 PIC 9.
       01 G4.
          05 B4 PIC 1(2) USAGE BIT.
          05 S4 GROUP-USAGE BIT.
             10 A4 PIC 1 USAGE BIT.
             10 T4 PIC 1(5) USAGE BIT ALIGNED
                OCCURS 1 TO 3 DEPENDING ON N4.
       01 G5 GROUP-USAGE BIT.
          05 B5 PIC 1(2) USAGE BIT.
          05 M5 GROUP-USAGE BIT.
             10 MA5 PIC 1(3) USAGE BIT.
             10 INN5 GROUP-USAGE BIT.
                15 IA5 PIC 1 USAGE BIT.
                15 IT5 PIC 1(2) USAGE BIT ALIGNED.
       01 X8 PIC 1(8) USAGE BIT.
       01 R PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           MOVE B"11" TO BA
           MOVE B"1" TO SA
           MOVE B"10101" TO TA
           MOVE B"111" TO UA
           MOVE B"01" TO CA
           DISPLAY "BG " BG
           DISPLAY "SUB " SUB
           MOVE SUB TO X14
           DISPLAY "X14 " X14
           DISPLAY "TA " TA " UA " UA " CA " CA
           MOVE FUNCTION LENGTH(BG) TO R
           DISPLAY "BG " R WITH NO ADVANCING
           MOVE FUNCTION LENGTH(SUB) TO R
           DISPLAY " SUB " R WITH NO ADVANCING
           MOVE FUNCTION BYTE-LENGTH(BG) TO R
           DISPLAY " BYTES " R
           MOVE B"11" TO CBA
           MOVE B"1" TO CSA
           MOVE B"10101" TO CTA
           MOVE B"111" TO CUA
           MOVE B"01" TO CCA
           MOVE "Z" TO CZ
           MOVE CG TO R4
           DISPLAY "CG " FUNCTION ORD(R4(1:1)) " "
                   FUNCTION ORD(R4(2:1)) " " FUNCTION ORD(R4(3:1)) " "
                   FUNCTION ORD(R4(4:1))
           MOVE FUNCTION LENGTH(CG) TO R
           DISPLAY "CG " R " " CTA " " CUA " " CCA " " CZ
           MOVE 1 TO N3
           MOVE FUNCTION LENGTH(G3) TO R
           DISPLAY "G3 N=1 " R WITH NO ADVANCING
           MOVE FUNCTION BYTE-LENGTH(G3) TO R
           DISPLAY " " R
           MOVE 2 TO N3
           MOVE FUNCTION LENGTH(G3) TO R
           DISPLAY "G3 N=2 " R WITH NO ADVANCING
           MOVE FUNCTION BYTE-LENGTH(G3) TO R
           DISPLAY " " R
           MOVE 3 TO N3
           MOVE FUNCTION LENGTH(G3) TO R
           DISPLAY "G3 N=3 " R WITH NO ADVANCING
           MOVE FUNCTION BYTE-LENGTH(G3) TO R
           DISPLAY " " R
           MOVE FUNCTION LENGTH(FG) TO R
           DISPLAY "FG " R
           MOVE B"11" TO B3
           MOVE B"1" TO A3
           MOVE B"10101" TO T3(1)
           MOVE B"01010" TO T3(2)
           MOVE B"11111" TO T3(3)
           DISPLAY "G3 " G3
           MOVE 2 TO N4
           MOVE B"11" TO B4
           MOVE B"1" TO A4
           MOVE B"10101" TO T4(1)
           MOVE B"01010" TO T4(2)
           MOVE FUNCTION LENGTH(G4) TO R
           DISPLAY "G4 " R
           MOVE G4 TO R4
           DISPLAY "G4 " FUNCTION ORD(R4(1:1)) " "
                   FUNCTION ORD(R4(2:1)) " " FUNCTION ORD(R4(3:1)) " "
                   FUNCTION ORD(R4(4:1))
           DISPLAY "T4 " T4(1) " " T4(2)
           MOVE B"11" TO B5
           MOVE B"101" TO MA5
           MOVE B"1" TO IA5
           MOVE B"01" TO IT5
           DISPLAY "G5 " G5
           DISPLAY "M5 " M5
           MOVE M5 TO X8
           DISPLAY "X8 " X8
           MOVE FUNCTION LENGTH(G5) TO R
           DISPLAY "G5 " R WITH NO ADVANCING
           MOVE FUNCTION BYTE-LENGTH(G5) TO R
           DISPLAY " BYTES " R
           STOP RUN.
