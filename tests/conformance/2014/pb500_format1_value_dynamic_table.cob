       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB500F1DYN.
      *> kb/Work PB500 - A **FORMAT 1** VALUE ON OR UNDER AN OCCURS DYNAMIC ENTRY IS CONFORMING SOURCE,
      *> and 13.18.63.4 GR16's VALUE-derived initial capacity DOES NOT REACH IT.
      *>
      *> A COBOLNET1528 refusal used to reject this file outright, citing GR16 and reading its subrule (b)
      *> ("If no TO phrase is specified in the VALUE clause, the initial capacity is set equal to the
      *> expected capacity specified in the OCCURS clause") as covering a Format 1 "VALUE IS literal-1",
      *> which trivially has no TO phrase.  IT DOES NOT.  GR16 sits under the FORMAT 2 general-rule heading
      *> (GR11-GR16), and 13.18.63 states cross-band application EXPLICITLY and in ONE direction only -
      *> GR11 ("General rules 1, 2, 3, 4, 5, 6, 7, 8, and 10 above apply"), GR17, GR21 and GR24 all import
      *> FORMAT 1 rules INTO a later band, and nothing imports GR12-GR16 back into FORMAT 1.  The syntax
      *> rules settle it independently: 13.18.63.3 SR22 ("A VALUE clause without the TO phrase shall not be
      *> specified in the same entry as an OCCURS clause with a DYNAMIC phrase but no TO phrase, or in any
      *> entry subordinate to such an OCCURS clause") is the rule that keeps GR16b from having no operand,
      *> and it too is FORMAT 2 (band SR16-SR23) with no FORMAT 1 counterpart.  13.18.38.3 forbids a VALUE
      *> on a Format 4 entry nowhere.
      *>
      *> EXPECTED VALUES ARE DERIVED FROM THE SPEC, and were written down before the confirming run:
      *>   CAPACITY - 14.6.2.3.2 item 6, "For each dynamic-capacity table, except where the table is
      *>     defined by an elementary entry with a VALUE clause, the capacity of the table is set to the
      *>     minimum capacity specified in the corresponding OCCURS clause", with 13.18.38.4 GR16,
      *>     "Integer-4 is the minimum capacity of the table.  If integer-4 is absent, a value of zero is
      *>     assumed for it."  The item-6 EXCEPTION is the carve-out that lets a GR16-derived (FORMAT 2)
      *>     capacity survive this step; for a Format 1 VALUE it changes nothing either way, because
      *>     8.5.1.9.1 gives the same number from the other side - "The current capacity of a
      *>     dynamic-capacity table may be initialized explicitly in the FROM phrase of the OCCURS clause
      *>     or implicitly in the VALUE clause.  If neither is specified, the current capacity is
      *>     initialized to zero" - and GR16 is the only "implicitly in the VALUE clause" mechanism there
      *>     is.  FROM, or zero, both ways.
      *>   CONTENT - 13.18.63.4 GR9, "A VALUE clause specified in a data description entry that contains an
      *>     OCCURS clause or in an entry that is subordinate to an OCCURS clause causes every occurrence
      *>     of the associated data item to be assigned the specified value", reinforced by 13.18.38.4 GR1
      *>     (band "FORMATS 1, 2 AND 4" - Format 4 IS the dynamic-capacity table): "Except for the OCCURS
      *>     clause itself, all data description clauses associated with an item whose description includes
      *>     an OCCURS clause apply to each occurrence of the item described."
      *>   GROWTH - 8.5.1.9.5, "If the INITIALIZED phrase is specified in the OCCURS clause of a
      *>     dynamic-capacity table, any elementary items not referenced as receiving operands in a
      *>     statement that creates new elements in that table are first initialized as though they had
      *>     been the subject of a statement of the form INITIALIZE ... WITH FILLER ALL TO VALUE THEN TO
      *>     DEFAULT."
      *>
      *> LINE 1  THE ELEMENTARY ARM.  E-TAB is an elementary dynamic entry carrying its own Format 1
      *>         VALUE.  Capacity is the OCCURS minimum 3 - NOT the expected capacity 9, which is only the
      *>         8.5.1.9.6 EC-BOUND-OVERFLOW ceiling - and 13.18.63.4 GR9 gives all three occurrences the value 7,
      *>         displayed through PIC 9(3) as 007.                     -> 1[0000000003][007|007]
      *> LINE 2  THE GROUP ARM, WITH AN OCCURS TO.  The VALUEs are on entries SUBORDINATE to the dynamic
      *>         entry, the half of GR9 that says "or in an entry that is subordinate to an OCCURS
      *>         clause".  Capacity is the minimum 2; each occurrence composes AB and 4.
      *>                                                                -> 2[0000000002][AB4|AB4]
      *> LINE 3  THE SAME CONSTRUCT WITH NO OCCURS TO - and therefore THE SAME ANSWER.  This line is the
      *>         two-arm witness: the retired refusal fired on line 2 and not on line 3, so one optional
      *>         phrase decided whether identical source compiled.      -> 3[0000000002][AB4|AB4]
      *> LINE 4  NO FROM PHRASE AT ALL.  13.18.38.4 GR16's "If integer-4 is absent, a value of zero is
      *>         assumed for it" makes the minimum 0, so the table opens EMPTY and GR9's "every occurrence"
      *>         is no occurrence.                                      -> 4[0000000000]
      *> LINE 5  GROWTH SEEDS FROM THE SAME VALUE.  A SET raises the capacity to 2; INITIALIZED is written,
      *>         so 8.5.1.9.5 initializes the two new occurrences ALL TO VALUE - both are V.
      *>                                                                -> 5[0000000002][V|V]
      *> LINE 6  BOTH FORMATS OVER ONE DYNAMIC TABLE.  X-A carries a Format 1 VALUE and X-B a Format 2 one.
      *>         GR16a applies to X-B alone and raises the initial capacity to subscript-2 = 4 ("provided
      *>         that this value does not lie outside the range defined by the minimum and expected
      *>         capacity specified in the OCCURS clause" - 4 lies inside [1, 8]).  Over those 4
      *>         occurrences GR9 gives X-A the value Z and GR12/GR13 give X-B the value Q.
      *>         The retired refusal rejected this file on X-A's Format 1 VALUE alone, taking the Format 2
      *>         capacity derivation down with it.                      -> 6[0000000004][ZQ|ZQ]
      *> LINE 7  14.6.2.3.2 item 6's SECOND sentence at INITIAL STATE, with no VALUE anywhere: "If the
      *>         INITIALIZED keyword is present in the OCCURS clause, all the occurrences, if any, of the
      *>         table are then initialized."  The minimum 2 occurrences exist, and 8.5.1.9.5 initializes
      *>         them "as though they had been the subject of a statement of the form INITIALIZE ... WITH
      *>         FILLER ALL TO VALUE THEN TO DEFAULT" - with no VALUE to take, TO DEFAULT gives the
      *>         category defaults, spaces for PIC X(2) and zero for PIC 9.       -> 7[0000000002][  /0|  /0]
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 E-GRP.
          05 E-TAB PIC 9(3) OCCURS DYNAMIC CAPACITY IN E-CAP
             FROM 3 TO 9 VALUE 7.
       01 N-GRP.
          05 N-TAB OCCURS DYNAMIC CAPACITY IN N-CAP FROM 2 TO 10.
             10 N-A PIC X(2) VALUE "AB".
             10 N-B PIC 9    VALUE 4.
       01 O-GRP.
          05 O-TAB OCCURS DYNAMIC CAPACITY IN O-CAP FROM 2.
             10 O-A PIC X(2) VALUE "AB".
             10 O-B PIC 9    VALUE 4.
       01 Z-GRP.
          05 Z-TAB PIC X OCCURS DYNAMIC CAPACITY IN Z-CAP INITIALIZED
             VALUE "V".
       01 X-GRP.
          05 X-TAB OCCURS DYNAMIC CAPACITY IN X-CAP FROM 1 TO 8.
             10 X-A PIC X VALUE "Z".
             10 X-B PIC X VALUES ARE "Q" FROM (1) TO (4).
       01 I-GRP.
          05 I-TAB OCCURS DYNAMIC CAPACITY IN I-CAP FROM 2 INITIALIZED.
             10 I-A PIC X(2).
             10 I-B PIC 9.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "1[" E-CAP "][" E-TAB(1) "|" E-TAB(3) "]"
           DISPLAY "2[" N-CAP "][" N-A(1) N-B(1) "|" N-A(2) N-B(2) "]"
           DISPLAY "3[" O-CAP "][" O-A(1) O-B(1) "|" O-A(2) O-B(2) "]"
           DISPLAY "4[" Z-CAP "]"
           SET Z-CAP TO 2
           DISPLAY "5[" Z-CAP "][" Z-TAB(1) "|" Z-TAB(2) "]"
           DISPLAY "6[" X-CAP "][" X-A(1) X-B(1) "|" X-A(4) X-B(4) "]"
           DISPLAY "7[" I-CAP "][" I-A(1) "/" I-B(1) "|"
                        I-A(2) "/" I-B(2) "]"
           STOP RUN.
