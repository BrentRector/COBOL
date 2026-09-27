      >>TURN EC-BOUND-OVERFLOW CHECKING ON
      >>TURN EC-BOUND-SET CHECKING ON
      *> kb/Work PB1144 - a variable-length group MOVE RECREATES a dynamic-capacity receiving table (ISO
      *> 14.6.9.2): "The operation recreates or overwrites the receiving table with a copy of the sending
      *> table, after freeing, if applicable, all the resources previously occupied by the receiving table",
      *> and "If the receiving table is a dynamic-capacity table specifying a minimum capacity that is higher
      *> than its current capacity, further elements are created and filled with spaces until the current
      *> capacity of the table is equal to its minimum capacity" (14.6.9.4: each element space-filled).
      *> A: a 1-element sender into a FROM 3 receiver that held OL1..OL4 -> capacity 3, [AAA][   ][   ]
      *>    (the stale OL2/OL3 are freed with the old table, not kept).
      *> B: a 5-element sender -> capacity 5, the fifth element B05; then a 2-element sender -> capacity 3,
      *>    [AAA][B02][   ].
      *> C: the recreation is an IMPLICIT capacity change (8.5.1.9.4 makes SET the only explicit one), so
      *>    8.5.1.9.6 1) applies: taking R (FROM 1 TO 4) to 6 raises EC-BOUND-OVERFLOW - never the SET's
      *>    EC-BOUND-SET - and the table is exceeded (RC=6, RE(6)=007); a second such MOVE while R is
      *>    already past 4 raises nothing ("If the change in capacity was implicit and the expected
      *>    capacity had already been exceeded before the operation, no exception shall exist").
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1144-DYN-MOVE-RECREATE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 GS.
          05 SH PIC X(2) VALUE "HH".
          05 ST PIC X(3) OCCURS DYNAMIC CAPACITY IN CA FROM 1.
       01 GM.
          05 MH PIC X(2).
          05 MT PIC X(3) OCCURS DYNAMIC CAPACITY IN CM FROM 3.
       01 S.
          05 SE PIC 9(3) OCCURS DYNAMIC CAPACITY IN SC.
       01 R.
          05 RE PIC 9(3) OCCURS DYNAMIC CAPACITY IN RC FROM 1 TO 4.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "AAA" TO ST (1).
           MOVE "OL1" TO MT (1).
           MOVE "OL2" TO MT (2).
           MOVE "OL3" TO MT (3).
           MOVE "OL4" TO MT (4).
           SET CA TO 1.
           MOVE GS TO GM.
           DISPLAY "A=" CM "[" MT (1) "][" MT (2) "][" MT (3) "]".
           MOVE "B02" TO ST (2).
           MOVE "B03" TO ST (3).
           MOVE "B04" TO ST (4).
           MOVE "B05" TO ST (5).
           MOVE GS TO GM.
           DISPLAY "B1=" CM "[" MT (5) "]".
           SET CA TO 2.
           MOVE GS TO GM.
           DISPLAY "B2=" CM "[" MT (1) "][" MT (2) "][" MT (3) "]".
           MOVE 7 TO SE (6).
           SET LAST EXCEPTION TO OFF.
           MOVE S TO R.
           DISPLAY "C1=[" FUNCTION EXCEPTION-STATUS "]".
           DISPLAY "C2=" RC " " RE (6).
           SET LAST EXCEPTION TO OFF.
           MOVE S TO R.
           DISPLAY "C3=[" FUNCTION EXCEPTION-STATUS "]".
           STOP RUN.
