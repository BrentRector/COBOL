      *> kb/Work PB1933 - a REDEFINES whose redefined item lies within an OCCURS
      *> DYNAMIC table. ISO 13.18.44.3 SR5: "data-name-2 may be subordinate to an
      *> item whose data description entry contains an OCCURS clause", and
      *> 13.18.44.4 GR1: "Storage association for the subject of the entry starts
      *> at the first bit of the data item referenced by data-name-2", so each
      *> occurrence of T has its own B over its own A.
      *> A store into an occurrence past the current capacity creates it (8.5.1.9.3
      *> - "the capacity of the table is increased"), whichever description of the
      *> occurrence's storage the store names.
      *> MEASURED BEFORE THE FIX: every reference to A(n), B1(n) or B2(n) drew
      *> COBOLNET0899 as a receiver and COBOLNET1756 (a run-time abort) as a
      *> sender - the class's backing was not reachable through a dynamic table.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1933DYN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 T OCCURS DYNAMIC CAPACITY IN C FROM 2 TO 5.
             10 A PIC X(4).
             10 B REDEFINES A.
                15 B1 PIC X(2).
                15 B2 PIC 99.
       PROCEDURE DIVISION.
      *> 1 - a store through A is read through B, and back.
           MOVE "ab12" TO A (2).
           DISPLAY "B=" B1 (2) "/" B2 (2).
           MOVE "zz" TO B1 (2).
           ADD 5 TO B2 (2).
           DISPLAY "A=" A (2).
      *> 2 - a store through the REDEFINES view into an occurrence past the
      *> current capacity creates that occurrence.
           MOVE "cd" TO B1 (4).
           MOVE 34 TO B2 (4).
           DISPLAY "CAPACITY=" C.
           DISPLAY "A4=" A (4).
           STOP RUN.
