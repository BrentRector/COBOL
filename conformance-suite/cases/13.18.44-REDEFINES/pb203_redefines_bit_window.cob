*>--- conformance-case
*> id: pb203_redefines_bit_window
*> rules: [GR-13.18.44.4-2, FMT-13.18.44.2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB203 - a USAGE BIT member of a REDEFINES class holds BIT positions of the shared area, not
      *> the characters of the byte that contains them. ISO 13.18.44.4 GR1 states the association in BITS:
      *> "Storage association for the subject of the entry starts at the first bit of the data item referenced
      *> by data-name-2 and continues over an area sufficient to contain the number of bits required by the
      *> data item referenced by the subject of the entry." 13.18.29.4 GR1 b) makes a bit group "an elementary
      *> data item of usage bit ... described with PICTURE 1(m), where m is the bit length of the group" and
      *> GR1 c) sends its members through 8.5.1.6.3, whose rule 1 puts two same-level bit items at successive
      *> BIT positions - so they SHARE a byte. 14.6.8.6's own NOTE settles it: "When an item is of usage bit,
      *> the item is not necessarily aligned on a byte boundary and the item need not occupy an integral
      *> number of bytes."
      *> BEFORE: the bit-group shapes failed BACKEND compilation (four CS0103 - a dead record-struct type was
      *> emitted for a group the physical model had collapsed into the class backing, and its AsBits() named
      *> the suppressed subordinates), while the elementary and sub-byte shapes compiled and answered the raw
      *> CHARACTER of the containing byte.
      *> ⛔ EVERY BIT LEG BELOW IS PAIRED WITH ITS NO-REDEFINES CONTROL. The claim is not "some bits come out"
      *> but "the redefines lane and the ordinary lane give the SAME answer", which is what one shared storage
      *> area means; a control that drifts is the failure this file exists to catch.
      *> Expected values are computed from the character repertoire: 'A'=x41=01000001, 'B'=x42=01000010,
      *> 'H'=x48=01001000, 'I'=x49=01001001, 'a'=x61=01100001, 'b'=x62=01100010, 'c'=x63=01100011,
      *> 'X'=x58=01011000, 'Y'=x59=01011001 - high-order bit first (8.5.1.6.3's "first bit position").
      *> EDITIONS - MEASURED, not assumed. GROUP-USAGE and boolean data are both COBOL-2002 introductions
      *> (constructs.json group-usage-2002 / boolean-data-2002), and this file's output is BYTE-IDENTICAL at
      *> --std 2002, 2014 and 2023; at --std 85 every leg is refused COBOLNET0900 ("GROUP-USAGE clause requires
      *> COBOL-2002" / "boolean data (PICTURE symbol 1 / USAGE BIT) requires COBOL-2002") plus COBOLNET1502 for
      *> FUNCTION BYTE-LENGTH. The fix changes storage association, never the edition gate, so the corpus keeps
      *> ONE copy - a byte-identical 2002/2014 duplicate would test the gate a third time and nothing else.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB203BW.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
      *> a level-1 BIT GROUP redefining a character item (8.5.1.6.3: "a level 1 bit group [is aligned] at the
      *> first bit of a byte", so the association starts at A's first bit).
       01 A PIC X(2) VALUE "AB".
       01 BV REDEFINES A GROUP-USAGE BIT.
          05 BV1 PIC 1(8).
          05 BV2 PIC 1(8).
      *> an ELEMENTARY bit item redefining a character item. Same CLR carrier and same byte occupancy as P,
      *> which is exactly why this used to alias P's one CHARACTER field and print "A".
       01 P PIC X(1) VALUE "A".
       01 PB REDEFINES P PIC 1(8) USAGE BIT.
      *> two SUB-BYTE bit items that SHARE a byte (8.5.1.6.3 rule 1), followed by a character item that
      *> therefore starts at the NEXT byte - the shape a byte-granular offset walk displaces.
       01 S PIC X(2) VALUE "AB".
       01 SV REDEFINES S.
          05 F1 PIC 1(4) USAGE BIT.
          05 F2 PIC 1(4) USAGE BIT.
          05 CC PIC X(1).
       01 CTL.
          05 H1 PIC 1(4) USAGE BIT VALUE B"0100".
          05 H2 PIC 1(4) USAGE BIT VALUE B"0001".
          05 H3 PIC X(1) VALUE "B".
      *> a TABLE of sub-byte bit items: occurrences lie at successive BIT positions, so the subscript stride
      *> is 4 bits - not the 1 BYTE the item's ceil(n/8) occupancy would suggest.
       01 T PIC X(3) VALUE "abc".
       01 TV REDEFINES T.
          05 E PIC 1(4) USAGE BIT OCCURS 6.
       01 TCTL.
          05 TCE PIC 1(4) USAGE BIT OCCURS 6.
      *> the bit group as the CANONICAL, with an alphanumeric view over it - the same class, entered from the
      *> other side (13.18.44.4 GR2: "the data-name associated with any of those data description entries may
      *> be used to reference that storage area").
       01 K GROUP-USAGE BIT.
          05 K1 PIC 1(8) VALUE B"01001000".
          05 K2 PIC 1(8) VALUE B"01001001".
       01 KV REDEFINES K PIC X(2).
       PROCEDURE DIVISION.
      *> ===== the bit GROUP view: the crash shape, and its bit values =====
           DISPLAY "A1=[" A "] BV=[" BV "] BV1=[" BV1 "] BV2=[" BV2 "]".
      *> 15.50.4 r1 gives an elementary bit item its length in BOOLEAN positions, 15.14 its BYTES; a bit group
      *> keeps its as-if PICTURE 1(m) description under REDEFINES.
           DISPLAY "A2=" FUNCTION LENGTH(BV) " " FUNCTION BYTE-LENGTH(BV).
      *> ===== the ELEMENTARY bit view =====
           DISPLAY "A3=[" P "] PB=[" PB "]".
      *> ===== two sub-byte members sharing one byte, and the character member after them =====
           DISPLAY "A4=[" F1 "][" F2 "][" CC "]".
           DISPLAY "A4C=[" H1 "][" H2 "][" H3 "]".
      *> a store into ONE of the two members leaves the other, and the byte's other occupant, untouched -
      *> 14.6.8.6 transfers "into the corresponding boolean positions of the receiving data item".
           MOVE B"1111" TO F1.
           MOVE B"1111" TO H1.
           DISPLAY "A5=[" F1 "][" F2 "][" CC "]".
           DISPLAY "A5C=[" H1 "][" H2 "][" H3 "]".
      *> a SHORT boolean source zero-fills to the right (14.6.8.6).
           MOVE B"0" TO F2.
           MOVE B"0" TO H2.
           DISPLAY "A6=[" F1 "][" F2 "][" CC "]".
           DISPLAY "A6C=[" H1 "][" H2 "][" H3 "]".
      *> INITIALIZE walks the view member-by-member at the same bit positions (13.18.29.4 GR1 c).
           INITIALIZE SV.
           INITIALIZE CTL.
           DISPLAY "A7=[" F1 "][" F2 "][" CC "]".
           DISPLAY "A7C=[" H1 "][" H2 "][" H3 "]".
      *> ===== the sub-byte OCCURS stride =====
           MOVE "abc" TO T.
           MOVE B"0110" TO TCE(1).
           MOVE B"0001" TO TCE(2).
           MOVE B"0110" TO TCE(3).
           MOVE B"0010" TO TCE(4).
           MOVE B"0110" TO TCE(5).
           MOVE B"0011" TO TCE(6).
           DISPLAY "A8=[" E(1) E(2) E(3) E(4) E(5) E(6) "]".
           DISPLAY "A8C=[" TCE(1) TCE(2) TCE(3) TCE(4) TCE(5) TCE(6) "]".
           MOVE B"1111" TO E(3).
           MOVE B"1111" TO TCE(3).
           DISPLAY "A9=[" E(3) "][" E(4) "]".
           DISPLAY "A9C=[" TCE(3) "][" TCE(4) "]".
      *> ===== the whole bit-group receiver, and 8.4.3.3.4 GR5a's BIT-position reference modification =====
      *> "If the usage of identifier-1 is bit, positions used in evaluation are bit positions" - and a Tier-B
      *> window over a bit member already reads its boolean carrier, so the slice needs no second substrate.
           MOVE B"0100100001001001" TO BV.
           DISPLAY "B1=[" A "] BV1=[" BV1 "] BV2=[" BV2 "]".
           DISPLAY "B2=[" BV(1:3) "]".
           MOVE ALL B"0" TO BV(2:3).
           DISPLAY "B3=[" BV "]".
      *> a store through the SUBORDINATE shows through the group and through A - one storage area (GR2).
           MOVE B"11111111" TO BV2.
           DISPLAY "B4=[" BV "]".
      *> ===== entered from the other side: the bit group is the canonical, the character item the view =====
           DISPLAY "C1=[" K "] KV=[" KV "]".
           MOVE "XY" TO KV.
           DISPLAY "C2=[" K "] K1=[" K1 "] K2=[" K2 "]".
           STOP RUN.
