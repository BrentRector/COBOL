*>--- conformance-case
*> id: pb886_refmod_intermediate_positions
*> rules: [GR-14.9.25.4-1]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB886 - THE INTERMEDIATE THAT EXISTS TO PRESERVE THE SENDER MUST BE ABLE TO HOLD IT.
      *> Every expected line below is derived from the rule text, never from a run.
      *>
      *> ISO 14.9.25.4 GR1 - "If identifier-1 is reference-modified, subscripted, or is a function-identifier,
      *>   the reference modifier, subscript, or function-identifier is evaluated only once, immediately before
      *>   data is moved to the first of the receiving operands", and the rule writes the required shape out as
      *>   an equivalence: "MOVE a (b) TO b, c (b) / is equivalent to: / MOVE a (b) TO temp / MOVE temp TO b /
      *>   MOVE temp to c (b) / where 'temp' is an intermediate result item provided by the implementor."
      *>   BECAUSE IT IS A RESULT EQUIVALENCE, the one-receiver form and the two-receiver form owe the same
      *>   value in every receiver: 'temp' is a step of the lowering, never an observable difference.
      *> ISO 8.4.3.3.4 GR5 a) - "If the usage of identifier-1 is bit, positions used in evaluation are bit
      *>   positions; otherwise, positions used in evaluation are character positions", and GR5 b)/c) bound
      *>   leftmost-position and length by "the number of positions in the data item referenced by
      *>   identifier-1". The intermediate's capacity is therefore counted in THOSE positions.
      *> ISO 8.4.3.3.4 GR6 - "The unique data item has the same class, category, and usage as that defined for
      *>   identifier-1", with three lettered exceptions that name neither boolean nor national: a slice of a
      *>   boolean item is boolean and a slice of a national item is national.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE SPEC:
      *>
      *> BIT-ONE / BIT-TWO      BE = B"10110011" is eight BOOLEAN positions (13.18.60.4 GR5). BE(1:4) is the
      *>                        leading four of them, "1011" (GR5 a): bit positions, because the usage is bit.
      *>                        The one-receiver and two-receiver forms owe the same answer, so
      *>                        BIT-ONE = 1011 and BIT-TWO = 1011/1011. An intermediate sized by the item's
      *>                        CHARACTER occupancy - one character, since 8.5.1.6.3 packs eight boolean
      *>                        positions into a byte - truncates the slice to "1" and pads: 1000/1000.
      *> NAT-ONE / NAT-TWO      NE = N"ABCDEF" is six NATIONAL positions; NE(1:4) is "ABCD" in four of them
      *>                        (GR5 a)'s "otherwise" arm, counted in the national alphabet by GR1's "national
      *>                        position"). Both forms owe ABCD.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB886POS02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BE             PIC 1(8) USAGE BIT VALUE B"10110011".
       01 B1             PIC 1(4) USAGE BIT.
       01 B2             PIC 1(4) USAGE BIT.
       01 NE             PIC N(6) USAGE NATIONAL VALUE N"ABCDEF".
       01 N1             PIC N(4) USAGE NATIONAL.
       01 N2             PIC N(4) USAGE NATIONAL.
       PROCEDURE DIVISION.
           MOVE BE(1:4) TO B1
           DISPLAY "BIT-ONE=" B1
           MOVE ALL B"0" TO B1
           MOVE BE(1:4) TO B1 B2
           DISPLAY "BIT-TWO=" B1 "/" B2

           MOVE NE(1:4) TO N1
           DISPLAY "NAT-ONE=" N1
           MOVE SPACES TO N1
           MOVE NE(1:4) TO N1 N2
           DISPLAY "NAT-TWO=" N1 "/" N2
           STOP RUN.
