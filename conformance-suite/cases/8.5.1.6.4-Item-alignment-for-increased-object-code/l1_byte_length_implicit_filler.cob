*>--- conformance-case
*> id: l1_byte_length_implicit_filler
*> rules: [GR-8.5.1.6.4-1, GR-13.18.55.4-2, GR-13.18.55.4-3, GR-13.18.55.4-9, RV-15.14.4-3, FMT-13.18.55.2, DOC-A.1-12, DOC-A.1-195, DOC-A.1-197]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.14.4 r3 FUNCTION BYTE-LENGTH - "The returned value shall include the number of implicit filler
      *> positions, if any, in argument-1" (plus the r4 LEVEL-77 legs, where the standard says filler is NOT
      *> generated and the rounding is therefore the whole answer).
      *>
      *> WHERE IMPLICIT FILLER COMES FROM. §8.5.1.6.3 "Alignment of data items of usage bit" names three sources:
      *>  (a) "Following a bit data item within an alphanumeric group item, within a strongly-typed group item,
      *>      or within a bit group item, as needed to advance alignment to a required natural boundary for the
      *>      next item within that group";
      *>  (b) "Following a bit data item that is the last data item in a record that is an alphanumeric group or
      *>      strongly-typed group item, as needed to increase the number of bits to fill an integral number of
      *>      characters" - §13.18.29.4 GR3: "If a GROUP-USAGE clause is not specified or implied for a group
      *>      item that is not strongly typed and is not a variable-length group, that group item is an
      *>      alphanumeric group item", so this reaches every group below;
      *>  (c) "As defined by the implementor for a bit data item described with the SYNCHRONIZED clause."
      *>
      *> WHICH OF THE THREE A BYTE COUNT CAN SEE - and this golden claims only what it measures.
      *> SOURCE (a) IS DISCRIMINATING and is what the RUN2 / TRAIL / NEST legs are built on: each is sized so that
      *> dropping the interior run CHANGES the answer (RUN2 4 vs 3, TRAIL 3 vs 2, NEST 3/4 vs 2/3). One short run
      *> would be absorbed by r4's ceiling, which is why no leg here carries only one.
      *> SOURCE (b) IS UNOBSERVABLE THROUGH EITHER LENGTH FUNCTION, BY CONSTRUCTION - and this golden says so
      *> instead of claiming a leg for it. All source (b) ever does is complete a PARTIAL TRAILING byte ("as
      *> needed to increase the number of bits to fill an integral number of characters"), and BOTH length rules
      *> round a partial position up regardless: §15.14.4 r4 - "When argument-1 does not occupy an integral number
      *> of bytes, the returned value is rounded to the next larger integer value" - and §15.50.4 r9 - "When the
      *> returned value is expressed as a number of alphanumeric character positions and argument-1 does not
      *> occupy an integral number of positions, the returned value is rounded to the next larger integer value".
      *> Every shape source (b) can reach returns ALPHANUMERIC character positions (§15.50.4 r3; r1's boolean
      *> positions and r2's national positions belong to the shapes §8.5.1.6.3's NOTE excludes from trailing
      *> filler), so the ceiling absorbs it in both functions. Here: TRAIL is 3 with the trailing run and 3
      *> without it (19 bits), ADJ3 is 2 either way (9 bits), NEST is 3 and 4 either way (17 and 25 bits).
      *> ⛔ AND THE SAME HOLDS FOR THE FUNCTION LENGTH GOLDEN OFTEN CREDITED WITH IT. The MIX=4 leg of
      *> conformance:2023/pb43_usage_bit_occupies_bits does NOT discriminate source (b) either - W-MIX is 3 bytes
      *> plus 5 bits, which is 4 character positions with the trailing filler and 4 without it, because r9 rounds.
      *> §15.50.4 r5 ("The returned length shall include the number of implicit FILLER positions, if any, in
      *> argument-1") is SATISFIED by that leg, not WITNESSED by it - a distinction worth writing down, because
      *> the opposite claim is what this golden inherited before the derivation was re-run against r9.
      *> SOURCE (c) IS EXERCISED, AT ZERO, AND THE LEG DISCRIMINATES (SBIT). It reaches "a bit data item
      *> described with the SYNCHRONIZED clause", and there §8.5.1.6.3 hands the implementor BOTH the amount and
      *> the placement - "The implementor defines the positioning rules associated with any filler bit positions"
      *> - because the clause's own ordinary placement rules exclude that shape by their words: "Alignment of
      *> elementary bit data items and bit group items within a record, when neither a SYNCHRONIZED clause nor an
      *> ALIGNED clause is specified, is at the next bit position in storage", and the paragraph below it reaches
      *> only "all other bit data items within a record, when a SYNCHRONIZED clause is not specified".
      *> §13.18.55.4 GR10 withholds even the AUTOMATIC form from the shape - "An implementor may optionally
      *> specify automatic alignment for any internal data representations except for bit data items and, within
      *> a record, data items described with usage display or national" - so nothing but the implementor's own
      *> determination decides it. COBOL.NET's is ZERO filler bit positions, the bit item sitting exactly where
      *> the same item without the clause would (Annex A.1 item 195; CONFORMANCE.md §7 item 195).
      *> SO THIS GOLDEN CLOSES §15.14.4 r3 ON SOURCES (a) AND (c), and states why (b) is not closable here.
      *> BITS PER BYTE. §8.1.2: "The implementor shall specify the number of bits in a byte for each supported
      *> computer." COBOL.NET pins 8 (CONFORMANCE.md §4.2.16; Annex A.1 item 12, "Byte (number of bits in) …
      *> (8.1.2, Computer's coded character set)" — item 209 is USAGE DISPLAY, and this comment inherited that
      *> number from the misfiled §7 row repaired 2026-09-03), which is what turns every bit
      *> count below into its byte answer.
      *>
      *> RUN2  - two INTERIOR filler runs, source (a). Placement is §8.5.1.6.3's second paragraph: a bit item
      *>         whose predecessor is NOT a bit item of the same level falls under "Alignment of all other bit
      *>         data items within a record, when a SYNCHRONIZED clause is not specified, is at the first bit
      *>         position of the first available byte", and the bits skipped to reach the next item's natural
      *>         boundary ARE the filler.
      *>         R2B1 bit 1 | filler 7 | R2A1 bits 9-16 | R2B2 bit 17 | filler 7 | R2A2 bits 25-32.
      *>         data 1+8+1+8 = 18 bits, filler 7+7 = 14, r3 total 32 bits = 4 bytes. Drop source (a) and the
      *>         answer is ceil(18/8) = 3, so 4 is the value only r3 produces.
      *> TRAIL - one interior source-(a) run, across a character item.
      *>         TRB1 bits 1-3 | filler 5 | TRA1 bits 9-16 | TRB2 bits 17-19 | trailing filler 5 = 24 bits = 3.
      *>         Without the source-(a) run the data is 3+8+3 = 14 bits -> ceil(14/8) = 2.
      *> NEST  - the same source ONE LEVEL DEEPER (05/10), and the enclosing group over the result.
      *>         NSI: NSB1 bit 1 | filler 7 | NSA2 bits 9-16 | NSB2 bit 17 | trailing filler 7 = 24 bits = 3.
      *>         NSG: NSA bits 1-8, then NSI (an alphanumeric group, not a bit item, so it starts at the next
      *>         available byte) 24 bits = 32 bits = 4. Without the source-(a) run: 10 and 18 bits -> 2 and 3.
      *> ADJ3  - THREE ADJACENT same-level bit siblings summing to 9 bits, i.e. one run crossing a byte boundary.
      *>         §8.5.1.6.3's first bullet puts "an elementary bit data item immediately following an elementary
      *>         bit data item or bit group item of the same level" at the NEXT BIT POSITION, so the three SHARE
      *>         bits 1-9 and the answer is 2; an implementation giving each sibling its own byte answers 3. This
      *>         leg pins the SHARING rule - the placement r3's count is taken over - not a filler source.
      *> SYNC  - the OTHER implicit-filler law, and it is NOT §8.5.1.6.3's. SYN is a binary NUMERIC item, not a
      *>         bit data item, so §8.5.1.6.3 source (c) does not reach it; the clause that governs it is
      *>         §13.18.55.4 GR9 b) - "Any necessary generation of implicit FILLER, if the elementary item
      *>         immediately preceding an item containing the SYNCHRONIZED clause does not terminate at an
      *>         appropriate natural boundary" - under GR9's "The effect of this clause is defined by the
      *>         implementor". That obligation is Annex A.1 item 195, and COBOL.NET's determination is NO
      *>         physical alignment and NO generated FILLER (CONFORMANCE.md §7 item 195). A COMP 9(4) item is
      *>         2 bytes (item 205: 3-4 digits -> 2), so the group is 1 + 2 = 3. An implementation aligning SYN
      *>         to a 2-byte boundary would answer 4, so the leg measures the determination rather than assuming it.
      *> SBIT  - §8.5.1.6.3 SOURCE (c), the one shape that clause hands to the implementor: a BIT data item
      *>         carrying the SYNCHRONIZED clause. §13.18.55.3 SR1 makes the source legal - "The SYNCHRONIZED
      *>         clause may be specified for group and elementary items" - and no syntax rule excludes a bit item.
      *>         SBB1 and SBB2 are two 3-bit siblings of the SAME level and only SBB2 carries the clause. Under
      *>         the item-195 determination SBB2 sits where an unSYNCHRONIZED sibling would - the shared next bit
      *>         position - so the group is 3+3 = 6 bits, trailing filler 2, = 8 bits = 1 byte. An implementation
      *>         that generated source-(c) filler to byte-align SBB2 answers 8+3 = 11 bits = 2. The leg therefore
      *>         MEASURES the determination instead of restating it, and ADJ3 above is its clause-free control
      *>         (the same sharing, 9 bits, no SYNCHRONIZED anywhere).
      *> L77   - §15.14.4 r4, on the ONE shape §8.5.1.6.3's NOTE excludes from filler entirely: "No filler is
      *>         generated at the end of a record that is entirely a bit group, at the end of a level 77 item,
      *>         or at the end of a level 1 elementary item." A level-77 PIC 1(12) USAGE BIT therefore occupies
      *>         exactly 12 bits - one and a half bytes - and r4 applies: "When argument-1 does not occupy an
      *>         integral number of bytes, the returned value is rounded to the next larger integer value" -> 2.
      *>         L77I is the CONTROL where r4 must NOT fire: 8 bits is an integral byte, so 1, not 2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1BLFILL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77 L77B PIC 1(12) USAGE BIT.
       77 L77I PIC 1(8) USAGE BIT.
       01 R2G.
          05 R2B1 PIC 1 USAGE BIT.
          05 R2A1 PIC X(1).
          05 R2B2 PIC 1 USAGE BIT.
          05 R2A2 PIC X(1).
       01 TRG.
          05 TRB1 PIC 1(3) USAGE BIT.
          05 TRA1 PIC X(1).
          05 TRB2 PIC 1(3) USAGE BIT.
       01 NSG.
          05 NSA PIC X(1).
          05 NSI.
             10 NSB1 PIC 1 USAGE BIT.
             10 NSA2 PIC X(1).
             10 NSB2 PIC 1 USAGE BIT.
       01 A3G.
          05 A3B1 PIC 1(3) USAGE BIT.
          05 A3B2 PIC 1(3) USAGE BIT.
          05 A3B3 PIC 1(3) USAGE BIT.
       01 SYG.
          05 SYA PIC X(1).
          05 SYN PIC 9(4) COMP SYNCHRONIZED.
       01 SBG.
          05 SBB1 PIC 1(3) USAGE BIT.
          05 SBB2 PIC 1(3) USAGE BIT SYNCHRONIZED.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "RUN2=" FUNCTION BYTE-LENGTH(R2G).
           DISPLAY "TRAIL=" FUNCTION BYTE-LENGTH(TRG).
           DISPLAY "NEST=" FUNCTION BYTE-LENGTH(NSI) " " FUNCTION BYTE-LENGTH(NSG).
           DISPLAY "ADJ3=" FUNCTION BYTE-LENGTH(A3G).
           DISPLAY "SYNC=" FUNCTION BYTE-LENGTH(SYG).
           DISPLAY "SBIT=" FUNCTION BYTE-LENGTH(SBG).
           DISPLAY "L77=" FUNCTION BYTE-LENGTH(L77B) " " FUNCTION BYTE-LENGTH(L77I).
           STOP RUN.
