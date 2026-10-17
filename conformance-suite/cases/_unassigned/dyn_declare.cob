*>--- conformance-case
*> id: dyn_declare
*> rules: []
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> Increment 1 of OCCURS DYNAMIC (ISO 13.18.38 Format 4 / 8.5.1.9,
      *> data-model design D9): the declaration + storage substrate. A
      *> dynamic-capacity table with a GROUP element (exercises the
      *> composed per-occurrence initializer) opens at its FROM capacity,
      *> every occurrence seeded (INITIALIZED, 8.5.1.9.5). The CAPACITY
      *> register read, SET Format 14, subscripted access and SEARCH are
      *> later increments; here we lock that the table declares, emits and
      *> runs (the CobolDynTable<T> construction).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DYN-DECLARE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-TABLE.
          05 WS-ROW OCCURS DYNAMIC FROM 2 TO 8 INITIALIZED.
             10 WS-NAME PIC X(5).
             10 WS-QTY  PIC 9(3).
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "DECLARED OK".
           STOP RUN.
