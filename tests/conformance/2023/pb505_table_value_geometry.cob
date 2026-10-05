       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB505TVG.
      *> kb/Work PB505 - THE FORMAT 2 (TABLE) VALUE OVER THE WHOLE POPULATION 13.18.63.3 SR18 ADMITS.
      *> Until this landed, DataBinder.ValidateTableValues accepted exactly one shape - a single-dimension
      *> table VALUE on the entry that itself carries the OCCURS clause - and every other shape exited
      *> through ONE staged diagnostic, COBOLNET0899, "recognized but currently supported only on a
      *> single-dimension table's own OCCURS entry".  That refusal covered CONFORMING source: SR18 says
      *> "A data description entry that contains the VALUE clause shall contain an OCCURS clause or be
      *> subordinate to a data description entry that contains an OCCURS clause", and the subordinate half
      *> was rejected.  It also stood in for SR20 sentence 1, SR21 sentence 1, SR22's subordinate arm and
      *> SR23, none of which was written down anywhere.
      *>
      *> EXPECTED VALUES ARE DERIVED FROM THE SPEC, and were written down before the confirming run:
      *>   13.18.63.4 GR12 - "A format 2 VALUE clause initializes a table element to the value of
      *>     literal-1.  The table element initialized is identified by subscript-1.  Consecutive table
      *>     elements are initialized, in turn, to the value of successive occurrences of the literal-1.
      *>     Consecutive table elements are referenced by incrementing by 1 the subscript that represents
      *>     the least inclusive dimension of the table.  When any reference to a subscript, prior to
      *>     incrementing it, is equal to the maximum number of occurrences ... that subscript is set to 1
      *>     and the subscript for the next most inclusive dimension of the table is incremented by 1."
      *>   13.18.63.4 GR13 - under TO, "all occurrences of literal-1 are reused, in the order specified,
      *>     as a source during the initialization described in General rule 12" until subscript-2's
      *>     element is initialized.
      *>   13.18.63.4 GR14 - "If the TO phrase is not specified, it is as if the TO phrase were specified
      *>     with each subscript-2 as the maximum number of occurrences ... of the table associated with
      *>     each corresponding subscript-1."
      *>   13.18.63.4 GR15 - "If multiple specifications of the FROM phrase reference the same table
      *>     element, the value defined by the last specified FROM phrase in the VALUE clause is assigned
      *>     to the table element."
      *>   13.18.63.4 GR16a - with a TO phrase "the initial capacity is increased, if necessary, to the
      *>     value of the corresponding subscript-2, provided that this value does not lie outside the
      *>     range defined by the minimum and expected capacity specified in the OCCURS clause."
      *>   13.18.63.4 GR5 (carried into format 2 by GR11) - "the group area is initialized without
      *>     consideration for the individual elementary or group items contained within this group."
      *>   13.18.57.4 GR1 - a TYPE reference assumes the type declaration's data description; the VALUE
      *>     clause is not in its exclusion list, in either of its two formats.
      *>
      *> LINE 1  SR18's SUBORDINATE arm.  The VALUE is on S-X, the OCCURS on S-TAB.  No TO, so GR14 makes
      *>         it TO (3); two literals cyclically reused by GR13: AB, CD, AB.        -> 1[AB|CD|AB]
      *> LINE 2  TWO DIMENSIONS.  Six literals from (1 1) to (2 3) in GR12's odometer order:
      *>         (1 1)=1 (1 2)=2 (1 3)=3, carry, (2 1)=4 (2 2)=5 (2 3)=6.              -> 2[123|456]
      *> LINE 3  GR15 ACROSS DIMENSIONS.  "A" FROM (1 1) TO (2 2) keys all four elements of the 2x2;
      *>         "Z" FROM (1 2) TO (2 1) then keys (1 2) and (2 1) - the two elements between them in
      *>         odometer order - and being LATER it wins on both.                     -> 3[AZ|ZA]
      *> LINE 4  A GROUP entry's table VALUE.  13.18.63.3 SR16 carries SR13 onto format 2 and GR5
      *>         initializes the AREA, per occurrence: "ABCD" is 4 character positions over G-P (2) and
      *>         G-Q (2), for occurrence 1 and occurrence 2 alike.                     -> 4[AB/CD|AB/CD]
      *> LINE 5  A TYPE clone keeps the table VALUE (13.18.57.4 GR1).                  -> 5[AB|CD|AB]
      *> LINE 6  A DYNAMIC dimension OUTSIDE the VALUE-carrying entry.  D-TAB opens at its OCCURS FROM
      *>         minimum 1; D-X's TO (3) raises the initial capacity to 3 by GR16a (3 lies inside
      *>         [1, 4]); the three occurrences take AB, CD, AB.  D-CAP is the CAPACITY register.
      *>                                                                  -> 6[0000000003][AB|CD|AB]
      *> LINE 7  THE BIT CARRIER, the third storage lane.  The level-01 REDEFINES puts BG's members on it
      *>         (13.18.63.3 SR12 bars a VALUE in the redefinING entry, never in the redefined one).  Two
      *>         boolean literals over three occurrences: GR14's implied TO (3) and GR13's cyclic reuse.
      *>         3 x PIC 1(4) is 12 boolean positions, which 8.5.1.6.3 packs into ceil(12/8) = 2
      *>         character positions, so the alias is PIC X(2).             -> 7[1010|0101|1010]
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S-GRP.
           05 S-TAB OCCURS 3.
               10 S-X PIC X(2) VALUES ARE "AB" "CD" FROM (1).
       01 M-GRP.
           05 M-ROW OCCURS 2.
               10 M-C PIC 9 OCCURS 3 VALUES ARE 1 2 3 4 5 6 FROM (1 1) TO (2 3).
       01 W-GRP.
           05 W-ROW OCCURS 2.
               10 W-C PIC X OCCURS 2 VALUES ARE "A" FROM (1 1) TO (2 2)
                                                "Z" FROM (1 2) TO (2 1).
       01 G-REC.
           05 G-T OCCURS 2 VALUE "ABCD" FROM (1) TO (2).
               10 G-P PIC X(2).
               10 G-Q PIC X(2).
       01 TT IS TYPEDEF.
           05 T-X PIC X(2) OCCURS 3 VALUES ARE "AB" "CD" FROM (1).
       01 T-REC TYPE TT.
       01 D-GRP.
           05 D-TAB OCCURS DYNAMIC CAPACITY IN D-CAP FROM 1 TO 4.
               10 D-X PIC X(2) VALUES ARE "AB" "CD" FROM (1) TO (3).
       01 BG GROUP-USAGE BIT.
           05 BT PIC 1(4) OCCURS 3 VALUES ARE B"1010" B"0101" FROM (1).
       01 BV REDEFINES BG PIC X(2).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "1[" S-X(1) "|" S-X(2) "|" S-X(3) "]"
           DISPLAY "2[" M-C(1 1) M-C(1 2) M-C(1 3) "|"
                        M-C(2 1) M-C(2 2) M-C(2 3) "]"
           DISPLAY "3[" W-C(1 1) W-C(1 2) "|" W-C(2 1) W-C(2 2) "]"
           DISPLAY "4[" G-P(1) "/" G-Q(1) "|" G-P(2) "/" G-Q(2) "]"
           DISPLAY "5[" T-X OF T-REC (1) "|" T-X OF T-REC (2) "|"
                        T-X OF T-REC (3) "]"
           DISPLAY "6[" D-CAP "][" D-X(1) "|" D-X(2) "|" D-X(3) "]"
           DISPLAY "7[" BT(1) "|" BT(2) "|" BT(3) "]"
           STOP RUN.
