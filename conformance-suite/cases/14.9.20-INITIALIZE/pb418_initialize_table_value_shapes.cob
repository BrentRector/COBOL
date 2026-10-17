*>--- conformance-case
*> id: pb418_initialize_table_value_shapes
*> rules: [GR-14.9.20.4-5, GR-14.9.20.4-6]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB418TVS.
      *> kb/Work PB418 — the OCCURRENCE-KEY shapes of ISO 14.9.20.4 GR5c1c / GR6a3. The 2002 golden
      *> (tests/conformance/2002/pb418_initialize_table_value_arms.cob) pins the GR5c/GR6 arm SELECTION; this one
      *> pins that the occurrence a Format-2 (table) VALUE is keyed by is identified on every lane the expansion
      *> can reach it through - a loop the walk introduced, a DYNAMIC-capacity loop, an OCCURS DEPENDING loop, and
      *> a subscript identifier-1 supplied itself, with a literal and with a run-time value.
      *>
      *> GR5c1c: "A table format VALUE clause is specified in the data description entry of the elementary item
      *>   and that VALUE clause specifies a value for the particular occurrence of the elementary data item."
      *> GR6a3: "If the data item is a table element, the literal in the VALUE clause that corresponds to the
      *>   occurrence being initialized determines the sending-operand."
      *> GR5b1/b2: a possible receiving item is one "explicitly referenced by identifier-1" or "contained within
      *>   the group data item referenced by identifier-1 ... each occurrence ... is a possible receiving-operand".
      *> GR8: "For a variable-occurrence data item, the number of occurrences initialized is determined by the
      *>   rules of the OCCURS clause for a receiving data item" -> 13.18.38.4 GR8a, the CURRENT count, when
      *>   data-name-1 is outside identifier-1's group.
      *> GR10: "When a group containing a dynamic-capacity table is initialized, all the elements of the table up
      *>   to current capacity, if any, are initialized ... and the current capacity of the table is left
      *>   unchanged."
      *> 13.18.63.3 SR18/SR20 put the VALUE on an entry SUBORDINATE to the OCCURS and key it by one subscript per
      *>   OCCURS clause "for the subject of the entry or superordinate to that entry"; 13.18.63.4 GR13 reuses a
      *>   short literal list cyclically, GR14 implies TO (max), GR16a raises a dynamic table's initial capacity.
      *>
      *> EXPECTED, DERIVED FROM THE RULES ABOVE AND WRITTEN DOWN BEFORE THE RUN:
      *>   1[AB|CD|AB]              SR18's subordinate arm: the VALUE is on S-X, the OCCURS on S-TAB, so the key
      *>                            is the loop the walk enters at the PARENT. No TO -> GR14 implies TO (3); two
      *>                            literals over three occurrences -> GR13's cyclic AB, CD, AB. Every occurrence
      *>                            is keyed, so GR5c1c qualifies all three and GR6a3 restores each.
      *>   2[0000000003][AB|CD|AB]  a DYNAMIC-capacity table. GR16a opened it at capacity 3 (D-X's TO (3), inside
      *>                            the OCCURS [1,4]); GR10 initializes the elements up to that capacity and
      *>                            leaves the capacity itself unchanged, so D-CAP still reads 3.
      *>   3[AB|CD]                 an OCCURS DEPENDING table whose data-name-1 (O-N = 2) is OUTSIDE identifier-1:
      *>                            13.18.38.4 GR8a bounds the receivers at the CURRENT count, so occurrence 3 is
      *>                            not a receiving-operand at all even though the clause keys it (GR14's implied
      *>                            TO (3) and GR13's cyclic reuse give 1/2/3 the literals AB/CD/AB).
      *>   4[ZZ|CD|ZZ]              identifier-1 IS one occurrence (GR5b1, literal subscript 2): only S-X(2) is a
      *>                            possible receiving-operand, and GR5c1c keys it to the second literal, CD.
      *>                            Occurrences 1 and 3 keep the ZZ the program moved in.
      *>   5[ZZ|CD|ZZ]              the same statement with a RUN-TIME subscript (IX = 2). The occurrence is not a
      *>                            bind-time constant, so the GR5c1c test is emitted rather than folded - and the
      *>                            answer has to be identical to line 4.
      *>   6[AB|CD|AB|  ]           GR6a beats GR6c on the ODO lane too: with O-N raised to 4 every occurrence is a
      *>                            receiving-operand; 1-3 are keyed by the clause (GR14's implied TO (3), GR13's
      *>                            cyclic AB/CD/AB) and take GR6a3's literal, while occurrence 4 is keyed by
      *>                            nothing, qualifies only through the DEFAULT phrase (GR5c3) and takes GR6c's
      *>                            "Figurative constant alphanumeric SPACES".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S-G.
           05 S-TAB OCCURS 3.
               10 S-X PIC X(2) VALUES ARE "AB" "CD" FROM (1).
       01 D-G.
           05 D-TAB OCCURS DYNAMIC CAPACITY IN D-CAP FROM 1 TO 4.
               10 D-X PIC X(2) VALUES ARE "AB" "CD" FROM (1) TO (3).
       01 O-N PIC 9 VALUE 2.
       01 O-G.
           05 O-T PIC X(2) OCCURS 1 TO 4 DEPENDING ON O-N
               VALUES ARE "AB" "CD" FROM (1) TO (3).
       01 IX PIC 9 VALUE 2.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "ZZ" TO S-X(1) S-X(2) S-X(3)
           INITIALIZE S-G ALL TO VALUE
           DISPLAY "1[" S-X(1) "|" S-X(2) "|" S-X(3) "]"

           MOVE "ZZ" TO D-X(1) D-X(2) D-X(3)
           INITIALIZE D-G ALL TO VALUE
           DISPLAY "2[" D-CAP "][" D-X(1) "|" D-X(2) "|" D-X(3) "]"

           MOVE "ZZ" TO O-T(1) O-T(2)
           INITIALIZE O-G ALL TO VALUE
           DISPLAY "3[" O-T(1) "|" O-T(2) "]"

           MOVE "ZZ" TO S-X(1) S-X(2) S-X(3)
           INITIALIZE S-X(2) ALL TO VALUE
           DISPLAY "4[" S-X(1) "|" S-X(2) "|" S-X(3) "]"

           MOVE "ZZ" TO S-X(1) S-X(2) S-X(3)
           INITIALIZE S-X(IX) ALL TO VALUE
           DISPLAY "5[" S-X(1) "|" S-X(2) "|" S-X(3) "]"

           MOVE 4 TO O-N
           MOVE "ZZ" TO O-T(1) O-T(2) O-T(3) O-T(4)
           INITIALIZE O-G ALL TO VALUE THEN TO DEFAULT
           DISPLAY "6[" O-T(1) "|" O-T(2) "|" O-T(3) "|" O-T(4) "]"
           STOP RUN.
