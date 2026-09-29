       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1716EC.
      *> kb/Work PB1716 - A FORMAT 2 (TABLE) VALUE FILLS EXACTLY THE ELEMENTS ITS PHRASE NAMES.
      *> The fill used to be bounded only by a 64,000,000-element cap, and a phrase that violated
      *> 13.18.63.3 SR23 ran all the way to it before being rejected.  The fill is now counted BEFORE
      *> it starts, as the mixed-radix distance from subscript-1 to subscript-2; every line below
      *> overlays a counted phrase on a whole-table "-" phrase, so one element too many or too few
      *> shows up as a wrong character next to a "-".
      *>
      *> EXPECTED VALUES ARE DERIVED FROM THE SPEC, and were written down before the confirming run:
      *>   13.18.63.4 GR12 - "Consecutive table elements are referenced by incrementing by 1 the
      *>     subscript that represents the least inclusive dimension of the table.  When any reference
      *>     to a subscript, prior to incrementing it, is equal to the maximum number of occurrences ...
      *>     that subscript is set to 1 and the subscript for the next most inclusive dimension of the
      *>     table is incremented by 1."
      *>   13.18.63.4 GR13 - under TO, "all occurrences of literal-1 are reused, in the order
      *>     specified, as a source during the initialization described in General rule 12".
      *>   13.18.63.4 GR15 - "If multiple specifications of the FROM phrase reference the same table
      *>     element, the value defined by the last specified FROM phrase in the VALUE clause is
      *>     assigned to the table element."
      *>   13.18.63.3 SR23 - with a TO phrase over an OCCURS DYNAMIC clause that has no TO phrase,
      *>     "the values of subscript-1 and subscript-2 corresponding to all levels higher than that
      *>     of the OCCURS clause, if applicable, shall be equal" - so a dimension with no ceiling may
      *>     be spanned only at the MOST inclusive level that differs.
      *>
      *> LINE 1  A CARRY BETWEEN THE TUPLES, 2x3 fixed.  "A" "B" "C" FROM (1 3) TO (2 1) names two
      *>         elements, (1 3) and (2 1): A, B.  C is never used.                     -> 1[--A|B--]
      *> LINE 2  AN UNBOUNDED OUTER DIMENSION.  U-TAB is OCCURS DYNAMIC with no TO; SR23 constrains
      *>         no level above it, so FROM (1 2) TO (2 2) spans it: (1 2)=1 (1 3)=2, carry out of
      *>         the bounded inner dimension, (2 1)=3 (2 2)=4.  "5" is never used.     -> 2[-12|34-]
      *> LINE 3  TWO NESTED UNBOUNDED DIMENSIONS.  SR23 holds for the inner one because level 1 is
      *>         equal (2 = 2); FROM (2 1) TO (2 3) names three elements: P, Q, P.     -> 3[PQP]
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B-REC.
           03 B-TAB OCCURS 2.
               05 B-X PIC X OCCURS 3
                   VALUES ARE "-" FROM (1 1) TO (2 3)
                              "A" "B" "C" FROM (1 3) TO (2 1).
       01 U-REC.
           03 U-TAB OCCURS DYNAMIC CAPACITY IN U-CAP.
               05 U-X PIC X OCCURS 3
                   VALUES ARE "-" FROM (1 1) TO (2 3)
                              "1" "2" "3" "4" "5" FROM (1 2) TO (2 2).
       01 N-REC.
           03 N-TAB OCCURS DYNAMIC CAPACITY IN N-CAP.
               05 N-X PIC X OCCURS DYNAMIC CAPACITY IN N-XCAP
                   VALUES ARE "P" "Q" FROM (2 1) TO (2 3).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "1[" B-X(1 1) B-X(1 2) B-X(1 3) "|"
                        B-X(2 1) B-X(2 2) B-X(2 3) "]"
           DISPLAY "2[" U-X(1 1) U-X(1 2) U-X(1 3) "|"
                        U-X(2 1) U-X(2 2) U-X(2 3) "]"
           DISPLAY "3[" N-X(2 1) N-X(2 2) N-X(2 3) "]"
           STOP RUN.
