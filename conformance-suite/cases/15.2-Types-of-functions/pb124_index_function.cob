*>--- conformance-case
*> id: pb124_index_function
*> rules: [GR-15.2-6, AR-15.3-5]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB124 wave 5b (GR-15.2-6 / AR-15.3-5 / AR-15.3-10) — INDEX functions. ISO 15.2 item 6:
      *> "Index functions. These are of the class and category index." MAX/MIN over index arguments is one
      *> (15.59.1's Index result row); its result participates where class index may — a relation condition
      *> (8.8.4.2's index comparison compares occurrence numbers) — and nowhere else: the negatives
      *> pb124-move-index-function / pb124-sqrt-index-function pin the MOVE (14.9.25.3 SR1) and the nested
      *> class-numeric argument (15.84.3 r1) rejections. Both the bare and the SUBSCRIPTED index-item forms
      *> select the Index result row (the adjudication's only-bare-matches concern measured false here).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB124IF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TIX.
          05 IXE USAGE INDEX OCCURS 2 TIMES.
       01 T.
          05 E PIC X OCCURS 5 TIMES INDEXED BY XI.
       01 IX1 USAGE INDEX.
       01 IX2 USAGE INDEX.
       PROCEDURE DIVISION.
       MAIN.
           SET XI TO 2
           SET IX1 TO XI
           SET XI TO 5
           SET IX2 TO XI
           IF FUNCTION MAX(IX1 IX2) = IX2
               DISPLAY "REL OK" ELSE DISPLAY "REL BAD" END-IF
           IF FUNCTION MIN(IX1 IX2) = IX1
               DISPLAY "MIN OK" ELSE DISPLAY "MIN BAD" END-IF
           SET XI TO 3
           SET IXE(1) TO XI
           SET XI TO 4
           SET IXE(2) TO XI
           IF FUNCTION MAX(IXE(1) IXE(2)) = IXE(2)
               DISPLAY "SUB OK" ELSE DISPLAY "SUB BAD" END-IF
           STOP RUN.
