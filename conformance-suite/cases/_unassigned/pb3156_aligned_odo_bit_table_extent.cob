*>--- conformance-case
*> id: pb3156_aligned_odo_bit_table_extent
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3156 - FUNCTION LENGTH of a bit group (and the group's image
      *> and bit string) whose OCCURS DEPENDING table of USAGE BIT items is
      *> ALIGNED.  Every value is derived from the standard, none read off a run.
      *>
      *> 13.18.1.4 GR1: "An ALIGNED clause causes the subject of the entry to be
      *> aligned on the first bit of the first available byte boundary."
      *> GR2: the clause "applies to each occurrence of that item", so every
      *> occurrence of the table starts a byte of its own.
      *> 8.5.1.6.3: implicit filler is generated "as needed to advance alignment
      *> to a required natural boundary for the next item within that group",
      *> as "a filler elementary bit data item ... of the same level number as
      *> the next item" - so the filler in front of the table is an item of its
      *> own, and the LAST occurrence has no next item to align, hence no filler
      *> after it.  A NOTE there: no filler follows a bit item at the end of a
      *> record that is entirely a bit group.
      *> 15.50.4 rule 1: a bit group's length is "in boolean positions"; rule 5
      *> counts the implicit filler; rule 9 rounds a partly used character up.
      *> 15.14.4 rule 4 rounds BYTE-LENGTH up the same way.
      *> 13.18.38.4 GR8: the current length of an OCCURS DEPENDING group is the
      *> length of the table as many occurrences as data-name-1 gives.
      *>
      *> LAYOUT (8 bits to a character position, docs/CONFORMANCE.md 4.2.16):
      *>   BA          bit 0
      *>   filler      bits 1-7        (TA starts a byte)
      *>   TA(k)       bits 8+8(k-1) .. 10+8(k-1), 3 bits each, 5 filler between
      *> LENGTH of the bit group G1, in positions:
      *>   N=0   8                       (BA and its filler; no occurrence)
      *>   N=1   8 + 3          = 11
      *>   N=2   8 + 8 + 3      = 19     = LENGTH of the fixed twin FG
      *>   N=4   8 + 3*8 + 3    = 35
      *> BYTE-LENGTH of G1: ceil(n/8) = 1, 2, 3, 5.
      *> LENGTH of the ALPHANUMERIC group G2 of the same members, in character
      *> positions (rule 9): 1, 2, 3, 5.
      *> DISPLAY of G1 at N=2 with BA=1, TA(1)=101, TA(2)=011 is its 19
      *> boolean positions: 1 0000000 101 00000 011.
      *> G2's image at N=2: bytes 10000000, 10100000, 01100000 =
      *> 128, 160, 96; FUNCTION ORD is one more: 129 161 97.
      *> SG's VALUE reaches TA(1) and TA(2) alike (13.18.63.4 GR9): bytes
      *> 128, 160, 160 -> ORD 129 161 161, and its 19 positions
      *> 1 0000000 101 00000 101.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3156AL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N1 PIC 9.
       01 G1 GROUP-USAGE BIT.
          05 BA PIC 1 USAGE BIT.
          05 TA PIC 1(3) USAGE BIT ALIGNED
             OCCURS 0 TO 4 DEPENDING ON N1.
       01 N1B PIC 9.
       01 G1B GROUP-USAGE BIT.
          05 BB PIC 1 USAGE BIT.
          05 TB PIC 1(3) USAGE BIT ALIGNED
             OCCURS 0 TO 4 DEPENDING ON N1B.
       01 FG GROUP-USAGE BIT.
          05 FA PIC 1 USAGE BIT.
          05 FT PIC 1(3) USAGE BIT ALIGNED OCCURS 2.
       01 N2 PIC 9.
       01 G2.
          05 CA PIC 1 USAGE BIT.
          05 CT PIC 1(3) USAGE BIT ALIGNED
             OCCURS 1 TO 4 DEPENDING ON N2.
       01 R3 PIC X(3).
       01 SG GROUP-USAGE BIT.
          05 SA PIC 1 USAGE BIT VALUE B"1".
          05 ST PIC 1(3) USAGE BIT ALIGNED OCCURS 2 VALUE B"101".
       01 SV REDEFINES SG PIC X(3).
       01 R PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           MOVE 0 TO N1
           MOVE FUNCTION LENGTH(G1) TO R
           DISPLAY "G1 N=0 " R WITH NO ADVANCING
           MOVE FUNCTION BYTE-LENGTH(G1) TO R
           DISPLAY " " R
           MOVE 1 TO N1
           MOVE FUNCTION LENGTH(G1) TO R
           DISPLAY "G1 N=1 " R WITH NO ADVANCING
           MOVE FUNCTION BYTE-LENGTH(G1) TO R
           DISPLAY " " R
           MOVE 2 TO N1
           MOVE FUNCTION LENGTH(G1) TO R
           DISPLAY "G1 N=2 " R WITH NO ADVANCING
           MOVE FUNCTION BYTE-LENGTH(G1) TO R
           DISPLAY " " R
           MOVE 4 TO N1
           MOVE FUNCTION LENGTH(G1) TO R
           DISPLAY "G1 N=4 " R WITH NO ADVANCING
           MOVE FUNCTION BYTE-LENGTH(G1) TO R
           DISPLAY " " R
           MOVE FUNCTION LENGTH(FG) TO R
           DISPLAY "FG " R
           MOVE 1 TO N2
           MOVE FUNCTION LENGTH(G2) TO R
           DISPLAY "G2 N=1 " R
           MOVE 2 TO N2
           MOVE FUNCTION LENGTH(G2) TO R
           DISPLAY "G2 N=2 " R
           MOVE 4 TO N2
           MOVE FUNCTION LENGTH(G2) TO R
           DISPLAY "G2 N=4 " R
           MOVE 2 TO N1
           MOVE 2 TO N1B
           MOVE B"1" TO BA
           MOVE B"101" TO TA(1)
           MOVE B"011" TO TA(2)
           DISPLAY "G1 " G1
           MOVE G1 TO G1B
           DISPLAY "G1B " G1B
           MOVE 2 TO N2
           MOVE B"1" TO CA
           MOVE B"101" TO CT(1)
           MOVE B"011" TO CT(2)
           MOVE G2 TO R3
           DISPLAY "G2 " FUNCTION ORD(R3(1:1)) " "
                   FUNCTION ORD(R3(2:1)) " " FUNCTION ORD(R3(3:1))
           DISPLAY "SG " SG
           DISPLAY "SV " FUNCTION ORD(SV(1:1)) " "
                   FUNCTION ORD(SV(2:1)) " " FUNCTION ORD(SV(3:1))
           STOP RUN.
