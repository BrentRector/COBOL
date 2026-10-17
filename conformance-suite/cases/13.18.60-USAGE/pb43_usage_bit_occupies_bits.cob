*>--- conformance-case
*> id: pb43_usage_bit_occupies_bits
*> rules: [GR-13.18.60.4-5, RV-15.50.4-5]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.60.4 GR5 - "The USAGE BIT clause specifies that BITS SHALL BE
      *> USED to represent a boolean data item. A data item described with USAGE BIT
      *> is a bit data item. The alignment of a data item described with USAGE BIT is
      *> specified in 8.5.1.6.3." (fix-queue PB43; design D19.)
      *>
      *> COBOL.NET stored USAGE BIT one '0'/'1' CHARACTER per bit and justified it
      *> with 13.18.40.4 GR14, which really does say "Each boolean character can be
      *> represented in storage as a bit, an alphanumeric character, or a national
      *> character". But GR14 lists the AVAILABLE representations; the USAGE clause
      *> SELECTS one, and both selections are mandatory:
      *>   - no USAGE clause -> 13.18.60.3 SR13(b) implies USAGE DISPLAY, and GR7
      *>     makes DISPLAY "an alphanumeric coded character set" -> one character per
      *>     boolean position is REQUIRED (the PLAIN- lines below, unchanged);
      *>   - USAGE BIT -> bits, aligned per 8.5.1.6.3 (everything else below).
      *>
      *> 8.5.1.6.3 IS A LAYOUT RULE, NOT A SUM, and SPLIT is the case that proves it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB43BITS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
      *> A bit item after a CHARACTER item: 8.5.1.6.3 - "Alignment of all other bit
      *> data items within a record, when a SYNCHRONIZED clause is not specified, is
      *> at the first bit position of the first available byte." -> byte 3. It is the
      *> last item of an alphanumeric-group
      *> record, so 8.5.1.6.3 adds trailing filler "to fill an integral number of
      *> characters" -> 1 char. 15.50.4 r5 requires that filler be COUNTED. 3+1 = 4.
       01 W-MIX.
          05 M1 PIC X(3) VALUE "ABC".
          05 M2 PIC 1(5) USAGE BIT VALUE B"10101".
      *> Two ADJACENT same-level bit items: the second is "immediately following an
      *> elementary bit data item of the same level", so it takes the NEXT BIT
      *> POSITION and they SHARE a byte. 5+3 bits = 1 character position.
      *> NOTE it is NOT a bit group item: 13.18.29.4 GR3 makes a group with no
      *> GROUP-USAGE clause an ALPHANUMERIC group item, so 15.50.4 r3 applies (
      *> character positions), not r1 (boolean positions).
       01 W-ADJ.
          05 J1 PIC 1(5) USAGE BIT.
          05 J2 PIC 1(3) USAGE BIT.
      *> THE DISCRIMINATOR. Two 3-bit items SEPARATED by a character item are two
      *> separate runs - the character item breaks the same-level adjacency - so each
      *> takes its own byte: 1+1+1 = 3. "Sum the bits then round once" gives
      *> 3+8+3 = 14 bits -> 2, which is what a naive fix would have returned.
       01 W-SPLIT.
          05 S1 PIC 1(3) USAGE BIT.
          05 S2 PIC X(1).
          05 S3 PIC 1(3) USAGE BIT.
      *> An ELEMENTARY bit item. 15.50.4 r1 gives its LENGTH in BOOLEAN positions
      *> (8), while it OCCUPIES 1 byte - the two are different questions and used to
      *> be the same number, which is exactly why the defect was invisible.
       01 W-ELEM PIC 1(8) USAGE BIT VALUE B"11000001".
      *> NO USAGE CLAUSE - the GR7 half. Unchanged, and required to be.
       01 W-PLAIN.
          05 P1 PIC X(3).
          05 P2 PIC 1(5).
       01 W-IMG PIC X(4).
       01 W-N PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "MIX=" FUNCTION LENGTH(W-MIX).
           DISPLAY "MIX-BYTES=" FUNCTION BYTE-LENGTH(W-MIX).
           DISPLAY "ADJ=" FUNCTION LENGTH(W-ADJ).
           DISPLAY "SPLIT=" FUNCTION LENGTH(W-SPLIT).
           DISPLAY "ELEM-LEN=" FUNCTION LENGTH(W-ELEM).
           DISPLAY "ELEM-BYTES=" FUNCTION BYTE-LENGTH(W-ELEM).
           DISPLAY "PLAIN=" FUNCTION LENGTH(W-PLAIN).
      *> THE IMAGE MUST AGREE WITH THE LENGTH, which is the half that makes this a
      *> representation change rather than an arithmetic one. A group move transfers
      *> the group's 4 character positions: "ABC" plus ONE packed byte. 8.5.1.6.3
      *> numbers bit positions from "the first bit position", so boolean position 1
      *> is the HIGH-ORDER bit: B"10101" packs to 10101000 = 168, filler zero.
      *> ORD is 1-based (15.70), hence the -1.
           MOVE W-MIX TO W-IMG.
           COMPUTE W-N = FUNCTION ORD(W-IMG(4:1)) - 1.
           DISPLAY "PACKED=" W-N.
      *> And it ROUND-TRIPS: the packed image back into the group restores both
      *> fields, so the codec is reversible rather than merely narrower.
           MOVE SPACES TO M1
           MOVE "00000" TO M2
           MOVE W-IMG TO W-MIX.
           DISPLAY "RT=[" M1 "][" M2 "]".
           STOP RUN.
