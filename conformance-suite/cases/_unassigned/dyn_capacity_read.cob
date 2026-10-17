*>--- conformance-case
*> id: dyn_capacity_read
*> rules: []
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> OCCURS DYNAMIC CAPACITY register READ (increment 2, data-model D9; ISO 13.18.38 GR15 / 8.5.1.9.1). The
      *> register data-name-3 is an implicitly-defined VIEW over the table's current capacity, usable anywhere an
      *> unsigned integer is: MOVE source (to a numeric-edited item), a relational operand, an arithmetic operand.
      *> The table opens at its FROM capacity (=3), so the register reads 3.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DYN-CAP-READ.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ED       PIC ZZ9.
       01 WS-DBL   PIC 9(4).
       01 WS-TABLE.
          05 WS-E PIC 9(3) OCCURS DYNAMIC CAPACITY IN WS-CAP FROM 3 TO 10.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE WS-CAP TO ED.
           DISPLAY "CAP=[" ED "]".
           IF WS-CAP = 3
               DISPLAY "EQ3 OK".
           COMPUTE WS-DBL = WS-CAP * 2.
           DISPLAY "DBL=" WS-DBL.
           STOP RUN.
