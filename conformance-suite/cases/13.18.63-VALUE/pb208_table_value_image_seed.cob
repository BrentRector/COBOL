*>--- conformance-case
*> id: pb208_table_value_image_seed
*> rules: [GR-13.18.63.4-12, GR-13.18.63.4-13, GR-13.18.63.4-14,
*>    GR-13.18.63.4-15]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB208TVI.
      *> kb/Work PB208 half 2 - A FORMAT 2 (TABLE) VALUE ON THE CHARACTER-IMAGE STORAGE LANE.
      *> GroupImageCodec.ImageInitOf is THE seeder for every image-stored backing since the PB164
      *> consolidation - Tier-B REDEFINES backings, EXTERNAL run-unit cells, BASED/ADDRESS-OF cells, the OO
      *> backings - and it read only item.RawValue, which is NULL for a table VALUE (the two carriers are
      *> mutually exclusive on DataItem).  Its `Occurs is n and > 1 ? StrRepeat(one, n)` then seeded every
      *> occurrence with the same VALUE-LESS default, so the table VALUE was DISCARDED, silently, for any leaf
      *> the aliasing put on that lane.  MEASURED on 3324d794: the GD group below came back
      *> `3C 3C 00 00 00 00 3E 3E` - the seed simply absent.  Each 01 here is aliased by a level-01 REDEFINES
      *> (13.18.63.3 SR12 bars a VALUE in the redefinING entry, never in the redefined one, so every program
      *> here is conforming), which is what puts its members on the image lane.
      *>
      *> EXPECTED VALUES ARE DERIVED FROM THE SPEC, and were written down before the confirming run:
      *>   13.18.63.4 GR12 - "A format 2 VALUE clause initializes a table element to the value of literal-1.
      *>     The table element initialized is identified by subscript-1.  Consecutive table elements are
      *>     initialized, in turn, to the value of successive occurrences of the literal-1."
      *>   13.18.63.4 GR13 - under TO, "all occurrences of literal-1 are reused, in the order specified"
      *>     until the element identified by subscript-2 is initialized (cyclic reuse).
      *>   13.18.63.4 GR14 - with no TO phrase it is as if TO named the maximum number of occurrences.
      *>   13.18.63.4 GR15 - "If multiple specifications of the FROM phrase reference the same table element,
      *>     the value defined by the last specified FROM phrase in the VALUE clause is assigned".
      *>   13.18.63.4 GR11 carries GR5 and GR7 into format 2, so each occurrence is aligned in its own
      *>     element and the group area is the members' positions in order.
      *>   15.70.1 - ORD "returns ... the ordinal position of argument-1 in the program collating sequence.
      *>     The lowest ordinal position is 1", so ORD is the byte value PLUS ONE.
      *>   CONFORMANCE.md DOC-A.1-205 (13.18.60.4 GR4) - a BINARY item is a two's-complement integer of its
      *>     unscaled value, most significant byte first, 2 bytes for a 3-4 digit picture, and that is "the
      *>     width the item occupies in a group image".  So PIC 9(4) COMP holding 12 is the bytes 00 0C.
      *>
      *> DERIVED, PER GROUP:
      *>   GD  D2(1) = D2(2) = 12 (GR12 seeds occurrence 1, GR13 reuses the single literal for occurrence 2).
      *>       VD = "<<" + 00 0C + 00 0C + ">>";  ORD of bytes 3..6 = 1 13 1 13.  VD(7:2) is ">>" - the
      *>       FOLLOWING member's offset, pinned so a displacement can never come back unnoticed.
      *>   GA  four occurrences, two literals, TO (4): ab cd ab cd  =>  "<<abcdabcd>>"   (GR12 + GR13)
      *>   GB  three occurrences, one literal, NO TO: xy xy xy      =>  "<<xyxyxy>>"     (GR14)
      *>   GC  "A" over 1..4 then "Z" over 2..3: A Z Z A            =>  "<<AZZA>>"       (GR15)
       DATA DIVISION.
       WORKING-STORAGE SECTION.
      *> GR12 + GR13 on a BYTE-FORM element, byte level, plus the following member's offset.
       01 GD.
          05 D1 PIC X(2) VALUE "<<".
          05 D2 PIC 9(4) COMP OCCURS 2 VALUE 12 FROM (1) TO (2).
          05 D3 PIC X(2) VALUE ">>".
       01 VD REDEFINES GD PIC X(8).
      *> GR13 - cyclic reuse of a two-literal list across four occurrences.
       01 GA.
          05 A1 PIC X(2) VALUE "<<".
          05 A2 PIC X(2) OCCURS 4 VALUES ARE "ab" "cd" FROM (1) TO (4).
          05 A3 PIC X(2) VALUE ">>".
       01 VA REDEFINES GA PIC X(12).
      *> GR14 - no TO phrase, so the fill runs to the maximum number of occurrences.
       01 GB.
          05 B1 PIC X(2) VALUE "<<".
          05 B2 PIC X(2) OCCURS 3 VALUE "xy" FROM (1).
          05 B3 PIC X(2) VALUE ">>".
       01 VB REDEFINES GB PIC X(10).
      *> GR15 - a later FROM phrase wins on the elements the two ranges share.
       01 GC.
          05 C1 PIC X(2) VALUE "<<".
          05 C2 PIC X OCCURS 4 VALUES ARE "A" FROM (1) TO (4) "Z" FROM (2) TO (3).
          05 C3 PIC X(2) VALUE ">>".
       01 VC REDEFINES GC PIC X(8).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "D=[" D2(1) "][" D2(2) "]"
           DISPLAY "DB=[" FUNCTION ORD(VD(3:1)) "][" FUNCTION ORD(VD(4:1))
                   "][" FUNCTION ORD(VD(5:1)) "][" FUNCTION ORD(VD(6:1)) "]"
           DISPLAY "DE=[" VD(1:2) "][" VD(7:2) "]"
           DISPLAY "A=[" VA "]"
           DISPLAY "B=[" VB "]"
           DISPLAY "C=[" VC "]"
           STOP RUN.
