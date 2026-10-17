*>--- conformance-case
*> id: pb907_through_alias_vlg_move
*> rules: [GR-13.18.45.4-2, GR-14.9.25.4-9]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB907 - A LEVEL-66 THROUGH ALIAS IS A GROUP ITEM ON EITHER SIDE OF A VARIABLE-LENGTH MOVE.
      *>
      *> 13.18.45.4 GR2: "When the THROUGH phrase is specified, data-name-1 defines an alphanumeric group
      *> item that includes all elementary items starting with data-name-2 ... and concluding with
      *> data-name-3". 14.9.25.4 GR9: "If both the sending operand and the receiving data item are group
      *> items and one or both is a variable-length group, the following rules apply". 14.9.25.3 SR9 makes
      *> the pair legal when the groups are compatible as specified in 8.5.1.12. SALIAS (S1 THRU S3) and
      *> DST are compatible: 8.5.1.12.2 "Two tables correspond if at least one of them is a dynamic-capacity
      *> table and they occupy the same relative byte positions within their groups" - ST and D1 both start
      *> at relative byte position 2 - and 8.5.1.12.3 "Two corresponding tables match when the byte length
      *> of their elements is equal" (1 and 1). This program used to be refused with "the sending operand
      *> is not a group item", which is false of an alias by GR2.
      *>
      *> EXPECTED VALUES, DERIVED:
      *>   L1 - GR9 a) (equal lengths: 8.5.1.12.3 makes D1 the same length as ST, so both are 7 bytes):
      *>        D0 takes "CD", D3 takes "EF", and 14.6.9.2 "recreates or overwrites the receiving table
      *>        with a copy of the sending table", so D1's capacity becomes 3 and its elements are TTT.
      *>   L2 - the reverse move into the alias: the receiving table ST is fixed-capacity, and 14.6.9.2
      *>        rule 2 "If the sending table has a lower current capacity than the receiving table, all
      *>        the remaining elements of the receiving table are space filled" - B1 holds 2 elements, so
      *>        ST becomes "ZW ". S1 takes "XY", S3 takes "UV", and S0 (outside the alias) keeps "AB".
      *>   L3 - the move's result is the same through the structural twin TG (a plain group with the
      *>        alias's layout) - the equivalence GR2 states.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB907VLA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-C PIC 9.
       01 SRC.
          05 S0 PIC X(2) VALUE "AB".
          05 S1 PIC X(2) VALUE "CD".
          05 ST PIC X OCCURS 3 VALUE "T".
          05 S3 PIC X(2) VALUE "EF".
       66 SALIAS RENAMES S1 THROUGH S3.
       01 TWIN.
          05 T0 PIC X(2) VALUE "AB".
          05 TG.
             10 T1 PIC X(2) VALUE "CD".
             10 TT PIC X OCCURS 3 VALUE "T".
             10 T3 PIC X(2) VALUE "EF".
       01 DST.
          05 D0 PIC X(2).
          05 D1 PIC X OCCURS DYNAMIC CAPACITY IN DCAP.
          05 D3 PIC X(2).
       01 BACK.
          05 B0 PIC X(2).
          05 B1 PIC X OCCURS DYNAMIC CAPACITY IN BCAP.
          05 B3 PIC X(2).
       PROCEDURE DIVISION.
       MAIN.
           MOVE SALIAS TO DST
           MOVE DCAP TO WS-C
           DISPLAY "L1=" WS-C " [" D0 "][" D1(1) D1(2) D1(3) "][" D3 "]"
           MOVE "XY" TO B0
           MOVE "Z" TO B1(1)
           MOVE "W" TO B1(2)
           MOVE "UV" TO B3
           MOVE BACK TO SALIAS
           DISPLAY "L2=[" SRC "]"
           MOVE TG TO DST
           MOVE DCAP TO WS-C
           DISPLAY "L3=" WS-C " [" D0 "][" D1(1) D1(2) D1(3) "][" D3 "]"
           MOVE BACK TO TG
           DISPLAY "L3=[" TWIN "]"
           STOP RUN.
