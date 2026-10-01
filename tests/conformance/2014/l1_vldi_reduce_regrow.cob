      *> ISO §8.5.1.11.3 6) — freeing on reduction never alters results
      *> A dynamic-capacity table reduced explicitly (SET) and then
      *>     grown
      *> again, explicitly (SET UP) and implicitly (a receiving
      *>     subscript
      *> past the capacity), never resurrects a deleted occurrence; a
      *> dynamic-length item overwritten by a shorter value keeps none
      *>     of
      *> the old content; a table implicitly reduced AND overwritten
      *> by a variable-length group MOVE never resurrects a freed
      *> element either (rule 6 implicit table branch, rule 7 table
      *> branch).
      *>
      *> THE RULES.
      *> §8.5.1.11.3: resources "may be freed automatically when: ...
      *>   6) the variable-length data item is explicitly or implicitly
      *>   reduced in size; 7) the variable-length data item is
      *>   overwritten with a new value.  The actual time when the
      *>   resources used by a variable-length data item are freed is
      *>   implementor-defined." with "this has no effect on the results
      *>   of the execution of the program."
      *>   OK  §8.5.1.11.3 6) / 7) / persistence paragraph
      *> §8.5.1.9.4: "If the capacity of the table is thereby reduced,
      *>   the appropriate number of higher occurrences is deleted and
      *>   any resources they were using are freed."    OK  §8.5.1.9.4
      *> §14.9.39.4 GR30: a) TO sets the new capacity; b) UP adds;
      *>   c) DOWN subtracts; "If the new capacity of the table is
      *>     greater
      *>   than the previous current capacity, new occurrences are
      *>     created
      *>   and are initialized as described in 8.5.1.9.5". OK §14.9.39.4
      *>     30)
      *> §8.5.1.9.3: a receiving subscript past the capacity creates the
      *>   element and raises the capacity; "new intermediate
      *>     occurrences
      *>   are implicitly created."                       OK  §8.5.1.9.3
      *> §8.5.1.9.5: with INITIALIZED, items "not referenced as
      *>     receiving
      *>   operands in a statement that creates new elements ... are
      *>     first
      *>   initialized as though they had been the subject of a
      *>     statement
      *>   of the form INITIALIZE ... WITH FILLER ALL TO VALUE THEN TO
      *>   DEFAULT".                                       OK
      *>     §8.5.1.9.5
      *> §14.9.20.4 GR5 c) 3. (DEFAULT is specified) / GR6 c): TE has
      *>   no VALUE clause, so it does not qualify through the VALUE
      *>   phrase, and its sender is the numeric default, figurative
      *>   ZEROES -> 000.
      *> §8.5.1.9.1: FROM 1 makes the initial capacity 1 and the minimum
      *>   1 (§13.18.38.4 GR16); no TO phrase, so no EC-BOUND-SET.
      *> §8.5.1.10.4: "The new length of the dynamic-length elementary
      *>   item is determined by the length of new content"; §15.50.4
      *>   GR6: FUNCTION LENGTH returns that current length.
      *>
      *> DERIVATION (N is PIC 99).
      *> A=04 111222333444  four receiving MOVEs grow the table 1->4.
      *> B=02 111222        SET TCAP TO 2 deletes occurrences 3 and 4.
      *> C=04 111222000000  SET TCAP UP BY 2 creates occurrences 3 and 4
      *>                    afresh, initialized to ZEROES - never the
      *>                    deleted 333 / 444.
      *> D=01 111           SET TCAP DOWN BY 3: 4 - 3 = 1 (the minimum).
      *> E=03 111000999     MOVE 999 TO TE (3) creates element 3 and the
      *>                    intermediate element 2; element 2 is not a
      *>                    receiving operand, so it is initialized to
      *>                    000 - never the deleted 222.
      *> F=[XY] 02          "XY" overwrites "ABCDEFGHIJ": length 2, and
      *>                    no trace of the longer old value.
      *> G=01 888           MOVE GS TO GR implicitly reduces RE from
      *>                    capacity 3 to 1 and overwrites it: 14.6.9.2
      *>                    "recreates or overwrites the receiving
      *>                    table with a copy of the sending table,
      *>                    after freeing, if applicable, all the
      *>                    resources previously occupied by the
      *>                    receiving table".  The groups are
      *>                    compatible: the tables correspond at byte
      *>                    0 and match (3-byte numeric elements),
      *>                    8.5.1.12.1-.3.  RE has no FROM phrase, so
      *>                    no minimum capacity and no space fill.
      *>                    Element 1 gets 888 by the MOVE rules.
      *>                                     OK  14.6.9.2 / 8.5.1.12.2-.3
      *> H=03 888000999     MOVE 999 TO RE (3) creates element 3 and
      *>                    the intermediate element 2 (8.5.1.9.3);
      *>                    INITIALIZED makes element 2 ZEROES
      *>                    (8.5.1.9.5) - never the freed 666.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1VL6A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 TE PIC 9(3) OCCURS DYNAMIC CAPACITY IN TCAP FROM 1
                INITIALIZED.
       01 D PIC X DYNAMIC LENGTH LIMIT IS 20.
       01 GS.
          05 SE PIC 9(3) OCCURS DYNAMIC CAPACITY IN SCAP.
       01 GR.
          05 RE PIC 9(3) OCCURS DYNAMIC CAPACITY IN RCAP
                INITIALIZED.
       01 N PIC 99.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 111 TO TE (1).
           MOVE 222 TO TE (2).
           MOVE 333 TO TE (3).
           MOVE 444 TO TE (4).
           MOVE TCAP TO N.
           DISPLAY "A=" N " " TE (1) TE (2) TE (3) TE (4).
           SET TCAP TO 2.
           MOVE TCAP TO N.
           DISPLAY "B=" N " " TE (1) TE (2).
           SET TCAP UP BY 2.
           MOVE TCAP TO N.
           DISPLAY "C=" N " " TE (1) TE (2) TE (3) TE (4).
           SET TCAP DOWN BY 3.
           MOVE TCAP TO N.
           DISPLAY "D=" N " " TE (1).
           MOVE 999 TO TE (3).
           MOVE TCAP TO N.
           DISPLAY "E=" N " " TE (1) TE (2) TE (3).
           MOVE "ABCDEFGHIJ" TO D.
           MOVE "XY" TO D.
           MOVE FUNCTION LENGTH (D) TO N.
           DISPLAY "F=[" D "] " N.
           MOVE 555 TO RE (1).
           MOVE 666 TO RE (2).
           MOVE 777 TO RE (3).
           MOVE 888 TO SE (1).
           MOVE GS TO GR.
           MOVE RCAP TO N.
           DISPLAY "G=" N " " RE (1).
           MOVE 999 TO RE (3).
           MOVE RCAP TO N.
           DISPLAY "H=" N " " RE (1) RE (2) RE (3).
           STOP RUN.
