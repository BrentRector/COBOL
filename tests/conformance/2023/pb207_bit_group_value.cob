      *> kb/Work PB207 - the 13.18.63.4 GR5 group-VALUE AREA of a BIT GROUP, in BOOLEAN POSITIONS.
      *> 13.18.29.4 GR1 a) makes a GROUP-USAGE BIT group "a bit group and also a bit data item; its class and
      *> category are boolean", and GR1 b) describes it "as though it were an elementary data item of usage bit
      *> and class and category boolean described with PICTURE 1(m), where m is the bit length of the group".
      *> So GR5's "the group area is initialized without consideration for the individual elementary or group
      *> items contained within this group" deposits m BOOLEAN POSITIONS, laid out by the 8.5.1.6.3 walk - not
      *> the ceil(m/8) CHARACTERS the group occupies.  13.18.63.3 SR13 admits the operand (a boolean literal is
      *> "of the same category as the group item"); SR10 caps it at "the size of the group item", so no leg here
      *> writes a literal longer than its group.
      *> GR7 sends the literal through 14.6.8, and the arm is the RECEIVING category's: 14.6.8.6 for category
      *> boolean - "transferred ... into the corresponding boolean positions of the receiving data item, with
      *> ZERO FILL or truncation to the right".  ZERO, not the space 14.6.8.5 gives an alphanumeric area; leg 3
      *> is the leg that tells the two arms apart.
      *> BEFORE: every program below was REFUSED by name (COBOLNET0899).  Before that refusal the multi-member
      *> shape CRASHED the compiler (ArgumentOutOfRangeException out of GroupValueSlicer.SliceInit, slicing a
      *> 1-character area for two members that each occupy ceil(2/8) = 1 character) and the single-member shape
      *> stored ONE boolean position where the literal has four.
      *> EVERY VALUE-BEARING LEG IS PAIRED WITH ITS MEMBER-WISE CONTROL (the byte-identical group whose members
      *> carry their own VALUEs instead).  The claim is not "some bits came out" but "the group-level VALUE and
      *> the member-wise spelling of the same storage give the SAME answer" - a control that drifts is the
      *> failure this file exists to catch.
      *> EXPECTED VALUES, COMPUTED FROM THE SPEC BEFORE THE CONFIRMING RUN.  m is 8.5.1.6.3's cursor walk: a
      *> bit item "immediately following an elementary bit data item or bit group item of the same level" goes
      *> at the next bit position (the only byte-sharing case); any other goes at "the first bit position of the
      *> first available byte"; and the NOTE under the trailing-filler rule excludes "a record that is entirely
      *> a bit group", so a bit group keeps its EXACT extent.
      *>   1  BGA m=4  area B"1010"           -> A1=10  A2=10
      *>   2  BGB m=4  area B"1010"           -> B1=1010
      *>   3  BGC m=6  area B"11" -> 110000   -> E1=110 E2=000      (14.6.8.6 zero fill)
      *>   4  BGE m=5  ALL B"10" -> 10101     -> F1=101 F2=01       (8.3.3.6.4 GR2 repeat + right truncate)
      *>   5  BGF m=6  area B"110010"         -> G1=11 G2=0010 (G2A=00 G2B=10)   nested bit group
      *>   6  BGG m=6  area B"110100"         -> H1(1)=11 H1(2)=01 H1(3)=00      (GR9, every occurrence)
      *>   7  BGH m=8  area B"11010010"       -> J1=1101 J2=0010; the PACKED byte is 0xD2 = 210, so
      *>                                         FUNCTION ORD of the REDEFINES view is 210 + 1 = 211
      *>   8  BGJ m=10 area B"1100000011"     -> L1=11 L2=11; L2 is ALIGNED, so it is NOT at the
      *>                                         next bit position and starts at the next BYTE - bits 2..7 are
      *>                                         8.5.1.6.3 implicit filler and LENGTH(BGJ) is 10, not 4.
      *>   9  NGA m=2  area N"AB"           -> M1=A M2=B        (13.18.29.4 GR2 b), as-if PICTURE N(m))
      *>  10  NGB m=2  area N"A" -> "A "     -> M3=A M4=space    (14.6.8.5 SPACE fill - the other GR7 arm)
      *>  12  TG  m=10 area B"1100000011"    -> T1=11 T2=11; TWO runs, each packed at its own byte boundary,
      *>                                         so both bytes are 11000000 = 0xC0 and FUNCTION ORD is 193.
      *> EDITIONS - MEASURED, not assumed.  GROUP-USAGE and boolean data are COBOL-2002 introductions
      *> (constructs.json group-usage-2002 / boolean-data-2002); this file's output is byte-identical at
      *> --std 2002, 2014 and 2023, and at --std 85 every leg is refused COBOLNET0900.  The area rule changes
      *> what a VALUE deposits, never the edition gate, so the corpus keeps ONE copy.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB207GV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
      *> 1 - the note's shape: two same-level members SHARING one byte (8.5.1.6.3 rule 1).
       01 BGA GROUP-USAGE BIT VALUE B"1010".
          05 A1 PIC 1(2).
          05 A2 PIC 1(2).
       01 CTA GROUP-USAGE BIT.
          05 A1C PIC 1(2) VALUE B"10".
          05 A2C PIC 1(2) VALUE B"10".
      *> 2 - ONE member, the half that used to store a single boolean position.
       01 BGB GROUP-USAGE BIT VALUE B"1010".
          05 B1 PIC 1(4).
       01 CTB GROUP-USAGE BIT.
          05 B1C PIC 1(4) VALUE B"1010".
      *> 3 - the literal SHORTER than the area: 14.6.8.6 zero fill to the right.
       01 BGC GROUP-USAGE BIT VALUE B"11".
          05 E1 PIC 1(3).
          05 E2 PIC 1(3).
       01 CTC GROUP-USAGE BIT.
          05 E1C PIC 1(3) VALUE B"110".
          05 E2C PIC 1(3) VALUE B"000".
      *> 4 - ALL literal-1: 8.3.3.6.4 GR2 repeats it to the area width and truncates from the right.
       01 BGE GROUP-USAGE BIT VALUE ALL B"10".
          05 F1 PIC 1(3).
          05 F2 PIC 1(2).
       01 CTE GROUP-USAGE BIT.
          05 F1C PIC 1(3) VALUE B"101".
          05 F2C PIC 1(2) VALUE B"01".
      *> 5 - a NESTED bit group: 13.18.29.3 SR2 makes it GROUP-USAGE BIT too, and it is a "bit group item
      *> immediately following ... an elementary bit data item of the same level", so it starts at bit 2.
       01 BGF GROUP-USAGE BIT VALUE B"110010".
          05 G1 PIC 1(2).
          05 G2.
             10 G2A PIC 1(2).
             10 G2B PIC 1(2).
       01 CTF GROUP-USAGE BIT.
          05 G1C PIC 1(2) VALUE B"11".
          05 G2C.
             10 G2AC PIC 1(2) VALUE B"00".
             10 G2BC PIC 1(2) VALUE B"10".
      *> 6 - a TABLE: 13.18.63.4 GR9 gives every occurrence its own positions, the stride being the
      *> per-occurrence BIT extent.
       01 BGG GROUP-USAGE BIT VALUE B"110100".
          05 H1 PIC 1(2) OCCURS 3.
      *> 7 - the CHARACTER-IMAGE lane: a REDEFINES view makes BGH a shared-storage class, so its backing is
      *> seeded by GroupImageCodec.ImageInitOf from the SAME area rule and read back at BYTE level.
      *> (13.18.63.3 SR12 bars a VALUE in the redefinING entry, never in the redefined one.)
       01 BGH GROUP-USAGE BIT VALUE B"11010010".
          05 J1 PIC 1(4).
          05 J2 PIC 1(4).
       01 RVH REDEFINES BGH PIC X(1).
       01 KG GROUP-USAGE BIT.
          05 J1C PIC 1(4) VALUE B"1101".
          05 J2C PIC 1(4) VALUE B"0010".
       01 RVK REDEFINES KG PIC X(1).
      *> 8 - a member that is NOT placed at the next bit position: an ALIGNED clause (13.18.1.4 GR1, "aligned on the
      *> first bit of the first available byte boundary") sends it to 8.5.1.6.3's "first bit position of the first
      *> available byte", so the group is 10 bits, not 4.  (This leg used to spell the same layout with members of
      *> ONE group at two level-numbers, 05 and 03, which 8.5.1.3.2 forbids: "All items that are immediately
      *> subordinate to a given group item shall be described using numerically equal level-numbers" - kb/Work PB1246.)
       01 BGJ GROUP-USAGE BIT VALUE B"1100000011".
          05 L1 PIC 1(2).
          05 L2 PIC 1(2) ALIGNED.
       01 CTJ GROUP-USAGE BIT.
          05 L1C PIC 1(2) VALUE B"11".
          05 L2C PIC 1(2) ALIGNED VALUE B"11".
      *> 9 / 10 - the OTHER arm of GR7's 14.6.8 dispatch, so the branch cannot drift: a NATIONAL group is
      *> 13.18.29.4 GR2 b)'s as-if PICTURE N(m), category national, and 14.6.8.5 gives it SPACE fill - not the
      *> boolean zero leg 3 pins.  One rule, two receiving categories, both measured.
       01 NGA GROUP-USAGE NATIONAL VALUE N"AB".
          05 M1 PIC N(1).
          05 M2 PIC N(1).
       01 CTN GROUP-USAGE NATIONAL.
          05 M1C PIC N(1) VALUE N"A".
          05 M2C PIC N(1) VALUE N"B".
       01 NGB GROUP-USAGE NATIONAL VALUE N"A".
          05 M3 PIC N(1).
          05 M4 PIC N(1).
      *> 12 - a TWO-RUN bit group that is ALSO a REDEFINES canonical.  Its compile-time image seed composes the
      *> WHOLE 10-bit area and packs once; PhysicalModel's runtime AsImage() packs each 8.5.1.6.3 RUN separately
      *> and concatenates.  The two agree only because rule 2 starts every run at a byte boundary, which is the
      *> assumption the seed rests on - so it is asserted, not argued.
       01 TG GROUP-USAGE BIT VALUE B"1100000011".
          05 T1 PIC 1(2).
          05 T2 PIC 1(2) ALIGNED.
       01 TV REDEFINES TG PIC X(2).
       01 TC GROUP-USAGE BIT.
          05 U1 PIC 1(2) VALUE B"11".
          05 U2 PIC 1(2) ALIGNED VALUE B"11".
       01 UV REDEFINES TC PIC X(2).
       PROCEDURE DIVISION.
       MAIN.
      *> ===== 1 - two same-level members sharing a byte =====
           DISPLAY "A1=[" BGA "] [" A1 "][" A2 "]".
           DISPLAY "A1C=[" CTA "] [" A1C "][" A2C "]".
           DISPLAY "A1L=" FUNCTION LENGTH(BGA) " " FUNCTION
             BYTE-LENGTH(BGA).
      *> ===== 2 - one member =====
           DISPLAY "A2=[" BGB "] [" B1 "]".
           DISPLAY "A2C=[" CTB "] [" B1C "]".
      *> ===== 3 - zero fill to the right (14.6.8.6), NOT space fill =====
           DISPLAY "A3=[" BGC "] [" E1 "][" E2 "]".
           DISPLAY "A3C=[" CTC "] [" E1C "][" E2C "]".
      *> ===== 4 - ALL literal-1 repeated to the width =====
           DISPLAY "A4=[" BGE "] [" F1 "][" F2 "]".
           DISPLAY "A4C=[" CTE "] [" F1C "][" F2C "]".
      *> ===== 5 - a nested bit group takes its own window of the area =====
           DISPLAY "A5=[" BGF "] [" G1 "][" G2 "] [" G2A "][" G2B "]".
           DISPLAY "A5C=[" CTF "] [" G1C "][" G2C "] [" G2AC "][" G2BC
             "]".
      *> ===== 6 - every occurrence takes its own positions =====
           DISPLAY "A6=[" BGG "] [" H1(1) "][" H1(2) "][" H1(3) "]".
      *> ===== 7 - the character-image lane, read as BYTES through the view =====
           DISPLAY "A7=[" BGH "] [" J1 "][" J2 "] " FUNCTION ORD(RVH).
           DISPLAY "A7C=[" KG "] [" J1C "][" J2C "] " FUNCTION ORD(RVK).
      *> ===== 8 - a differently-levelled member starts at the next byte =====
           DISPLAY "A8=[" BGJ "] [" L1 "][" L2 "]".
           DISPLAY "A8C=[" CTJ "] [" L1C "][" L2C "]".
           DISPLAY "A8L=" FUNCTION LENGTH(BGJ) " " FUNCTION
             BYTE-LENGTH(BGJ).
      *> ===== the area is a RECEIVING field, not a constant: a store into one member leaves the others =====
      *> (14.6.8.6 "into the corresponding boolean positions of the receiving data item").
           MOVE B"01" TO A1.
           DISPLAY "A9=[" BGA "] [" A1 "][" A2 "]".
      *> ===== 9 / 10 - the national arm keeps 14.6.8.5's SPACE fill =====
           DISPLAY "A10=[" NGA "] [" M1 "][" M2 "]".
           DISPLAY "A10C=[" CTN "] [" M1C "][" M2C "]".
           DISPLAY "A11=[" NGB "] [" M3 "][" M4 "]".
      *> ===== 12 - the seed's one Pack over the whole extent = the runtime's Pack per run =====
           DISPLAY "A12=[" TG "] [" T1 "][" T2 "] "
             FUNCTION ORD(TV(1:1)) " " FUNCTION ORD(TV(2:1)).
           DISPLAY "A12C=[" TC "] [" U1 "][" U2 "] "
             FUNCTION ORD(UV(1:1)) " " FUNCTION ORD(UV(2:1)).
           STOP RUN.
