*>--- conformance-case
*> id: pb418_initialize_table_value_arms
*> rules: [GR-14.9.20.4-5, GR-14.9.20.4-6]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB418TV.
      *> kb/Work PB418 — ISO 14.9.20.4 GR5c1c and GR6a3, the PER-OCCURRENCE arm of the VALUE phrase, at the
      *> introduction edition of the Format 2 (table) VALUE clause and of the TO VALUE / THEN TO DEFAULT phrases
      *> (COBOL-2002).
      *>
      *> GR5c1c: "A table format VALUE clause is specified in the data description entry of the elementary item
      *> and that VALUE clause specifies a value for the particular occurrence of the elementary data item."
      *> GR6a3: "Otherwise, the sending-operand is determined by the literal in the VALUE clause specified in the
      *> data description entry of the data item. If the data item is a table element, the literal in the VALUE
      *> clause that corresponds to the occurrence being initialized determines the sending-operand."
      *> GR6b: "If the data item does not qualify as a receiving-operand because of the VALUE phrase, but does
      *> qualify because of the REPLACING phrase, the sending-operand is the literal-1 or identifier-2 associated
      *> with the category specified in the REPLACING phrase."
      *> GR6c: "If the data item does not qualify in accordance with General rules 6a and 6b" - the category fill
      *> table, Alphanumeric -> "Figurative constant alphanumeric SPACES", Numeric -> "Figurative constant ZEROES".
      *> GR5c3 "The DEFAULT phrase is specified"; GR5c4 "Neither the REPLACING phrase nor the VALUE phrase is
      *> specified".
      *>
      *> T1 gives occurrences 1 and 2 a value and says NOTHING about 3 and 4, so GR5c1c is true for exactly two
      *> of the four occurrences - which is what makes this table the witness for the whole of GR5c/GR6: one
      *> statement has to take two different arms for two occurrences of ONE elementary item.
      *>
      *> EXPECTED, DERIVED FROM THE RULES ABOVE AND WRITTEN DOWN BEFORE THE RUN:
      *>   0[AA|BB|  |  |42]    initial state - 13.18.63.4 does not define the content of an occurrence outside
      *>                        every FROM..TO range, so 3 and 4 take the VALUE-less alphanumeric default.
      *>   1[AA|BB|ZZ|ZZ|42]    ALL TO VALUE: occurrences 1-2 qualify by GR5c1c and GR6a3 restores AA / BB;
      *>                        occurrences 3-4 fail GR5c1 (no value for THAT occurrence), fail GR5c2 (no
      *>                        REPLACING) and GR5c3 (no DEFAULT), and fail GR5c4 (the VALUE phrase IS specified)
      *>                        - they are not receiving-operands and keep ZZ. N1 qualifies by GR5c1b -> 42.
      *>   2[AA|BB|  |  |42]    ALL TO VALUE THEN TO DEFAULT: 1-2 still qualify through the VALUE phrase, so GR6a
      *>                        - the FIRST arm of GR6 - still fixes their sender to AA / BB; 3-4 now qualify by
      *>                        GR5c3 and fall to GR6c's alphanumeric SPACES. N1 keeps its GR6a3 42.
      *>   3[XY|XY|XY|XY|07]    REPLACING ALPHANUMERIC alone: every occurrence qualifies by GR5c2 and takes the
      *>                        GR6b sender. N1 is category numeric, is not named, and GR5c4 is false because a
      *>                        REPLACING phrase IS specified - it keeps the 07 the program moved in.
      *>   4[AA|BB|XY|XY|42]    ALL TO VALUE REPLACING ALPHANUMERIC: GR6a beats GR6b where both qualify (1-2),
      *>                        and GR6b covers the occurrences GR5c1c does not key (3-4).
      *>   5[AA|BB|XY|XY|42]    ... THEN TO DEFAULT added: GR6b still beats GR6c, so 3-4 stay XY, not spaces.
      *>   6[123|456]           a TWO-dimensional table VALUE (13.18.63.3 SR20's subscript tuple, most inclusive
      *>                        first): GR5c1c and GR6a3 apply per OCCURRENCE TUPLE, not per element entry.
      *>   7[000|000]           CONTROL - the bare COBOL-85 form reaches the same elements through GR5c4 and
      *>                        GR6c's "Numeric | Figurative constant ZEROES" row.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G1.
           05 T1 PIC X(2) OCCURS 4 VALUES ARE "AA" "BB" FROM (1) TO (2).
           05 N1 PIC 9(2) VALUE 42.
       01 G2.
           05 R2 OCCURS 2.
               10 M2 PIC 9 OCCURS 3 VALUES ARE 1 2 3 4 5 6 FROM (1 1) TO (2 3).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "0[" T1(1) "|" T1(2) "|" T1(3) "|" T1(4) "|" N1 "]"

           PERFORM SCRAMBLE-1
           INITIALIZE G1 ALL TO VALUE
           DISPLAY "1[" T1(1) "|" T1(2) "|" T1(3) "|" T1(4) "|" N1 "]"

           PERFORM SCRAMBLE-1
           INITIALIZE G1 ALL TO VALUE THEN TO DEFAULT
           DISPLAY "2[" T1(1) "|" T1(2) "|" T1(3) "|" T1(4) "|" N1 "]"

           PERFORM SCRAMBLE-1
           INITIALIZE G1 REPLACING ALPHANUMERIC DATA BY "XY"
           DISPLAY "3[" T1(1) "|" T1(2) "|" T1(3) "|" T1(4) "|" N1 "]"

           PERFORM SCRAMBLE-1
           INITIALIZE G1 ALL TO VALUE REPLACING ALPHANUMERIC DATA BY "XY"
           DISPLAY "4[" T1(1) "|" T1(2) "|" T1(3) "|" T1(4) "|" N1 "]"

           PERFORM SCRAMBLE-1
           INITIALIZE G1 ALL TO VALUE REPLACING ALPHANUMERIC DATA BY "XY"
               THEN TO DEFAULT
           DISPLAY "5[" T1(1) "|" T1(2) "|" T1(3) "|" T1(4) "|" N1 "]"

           PERFORM SCRAMBLE-2
           INITIALIZE G2 ALL TO VALUE
           DISPLAY "6[" M2(1 1) M2(1 2) M2(1 3) "|"
                        M2(2 1) M2(2 2) M2(2 3) "]"

           PERFORM SCRAMBLE-2
           INITIALIZE G2
           DISPLAY "7[" M2(1 1) M2(1 2) M2(1 3) "|"
                        M2(2 1) M2(2 2) M2(2 3) "]"
           STOP RUN.
       SCRAMBLE-1.
           MOVE "ZZ" TO T1(1) T1(2) T1(3) T1(4)
           MOVE 7 TO N1.
       SCRAMBLE-2.
           MOVE 9 TO M2(1 1) M2(1 2) M2(1 3)
           MOVE 9 TO M2(2 1) M2(2 2) M2(2 3).
