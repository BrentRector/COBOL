*>--- conformance-case
*> id: pb393_move_varlen_table
*> rules: [GR-14.6.9.2-2, GR-14.9.25.4-9]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB393. ISO 1989:2023 14.9.25.4 GR9a's table half - "Where two tables correspond, as specified
      *> in 8.5.1.12.2, Positional correspondence, the table in the sending group is moved to the corresponding
      *> table in the receiving group, as specified in 14.6.9.2, Moving a table" - in BOTH directions, because
      *> 8.5.1.12.1 admits a FIXED group as the other operand ("only one of the operands may be a
      *> variable-length group") and 8.5.1.12.3 says how: a corresponding table that is not dynamic-capacity
      *> "is treated as though it were a dynamic-capacity table whose capacity is ... its fixed number of
      *> occurrences", the same conversion 14.6.9.1 states for the operation itself.
      *>
      *> D= dynamic-to-dynamic. 14.6.9.2: "The operation recreates or overwrites the receiving table with a
      *>    copy of the sending table" - the receiving table's capacity becomes the sender's 2, whatever its
      *>    own FROM 1 seed was, so CB=2 and both elements arrive.
      *> F= dynamic-to-FIXED. 14.6.9.2 rule 2: "If the sending table has a lower current capacity than the
      *>    receiving table, all the remaining elements of the receiving table are space filled" - the sender
      *>    holds 2 elements, the receiving fixed table has 3, so FX-T(3) is spaces.
      *> R= FIXED-to-dynamic. The fixed table is a dynamic-capacity table of capacity 3 by 8.5.1.12.3, and
      *>    "recreates or overwrites the receiving table with a copy of the sending table" makes the receiving
      *>    dynamic table's capacity 3.
      *> X= the EXCESS PART. SFIX is a FIXED group with no table at all, and 8.5.1.12.2's last sentence
      *>    admits the pair anyway: "where the relative byte position of a dynamic capacity table in the
      *>    longer group is beyond the last character of the shorter group, the dynamic capacity table is
      *>    treated as if it corresponds to a space-filled fixed-length table". 14.9.25.4 GR9b step 2 then
      *>    sends RV-T to 14.6.9.4: "the current capacity of the dynamic table is unaffected, and each
      *>    element of the dynamic table is space-filled" - so the capacity stays 2 and both elements are
      *>    spaces. It is NOT recreated at capacity zero, which is what 14.6.9.2 would do for a sender that
      *>    really carried an empty table; the two cases are the same zero-length component and are told
      *>    apart by whether one was carried at all.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB393MVDYNT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 EA PIC ZZ9.
       01 GS.
          05 SH PIC X(2) VALUE "HH".
          05 ST PIC X(3) OCCURS DYNAMIC CAPACITY IN CA FROM 2.
       01 GD.
          05 DH PIC X(2).
          05 DT PIC X(3) OCCURS DYNAMIC CAPACITY IN CB FROM 1.
       01 FXG.
          05 FX-H PIC X(2).
          05 FX-T PIC X(3) OCCURS 3 TIMES.
       01 GE.
          05 EH PIC X(2).
          05 ET PIC X(3) OCCURS DYNAMIC CAPACITY IN CE FROM 1.
       01 SFIX.
          05 SF-A PIC X(2) VALUE "PQ".
       01 RVAR.
          05 RV-A PIC X(2).
          05 RV-T PIC X(3) OCCURS DYNAMIC CAPACITY IN RV-C FROM 2.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "AAA" TO ST (1).
           MOVE "BBB" TO ST (2).
           MOVE GS TO GD.
           MOVE CB TO EA.
           DISPLAY "D=[" DH "]" EA "[" DT (1) "][" DT (2) "]".
           MOVE GS TO FXG.
           DISPLAY "F=[" FX-H "][" FX-T (1) "][" FX-T (2) "][" FX-T (3)
               "]".
           MOVE FXG TO GE.
           MOVE CE TO EA.
           DISPLAY "R=[" EH "]" EA "[" ET (1) "][" ET (3) "]".
           MOVE "111" TO RV-T (1).
           MOVE "222" TO RV-T (2).
           MOVE SFIX TO RVAR.
           MOVE RV-C TO EA.
           DISPLAY "X=[" RV-A "]" EA "[" RV-T (1) "][" RV-T (2) "]".
           STOP RUN.
