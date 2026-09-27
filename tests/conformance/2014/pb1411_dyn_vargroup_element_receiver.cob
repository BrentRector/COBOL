      *> kb/Work PB1411 - "its occurrences are created dynamically" (ISO 8.5.1.9.1 1)) holds when the
      *> receiving operand is an element of a dynamic-capacity table that is itself a VARIABLE-LENGTH GROUP
      *> (it holds a nested dynamic-capacity table). 8.5.1.9.3: "When a data item in a dynamic-capacity table
      *> is referenced as a receiving item and the value of the subscript exceeds the current capacity of the
      *> table, a new element is automatically created and the capacity of the table is increased to the
      *> value given by the subscript." MOVE SRC TO EL (3) therefore takes EL from capacity 1 to 3, and the
      *> variable-length group MOVE (14.9.25.4 GR9 / 14.6.9.2) lands in the new element: EA(3) = "ZZ", its
      *> nested table recreated as a copy of SB (B01, B02). The same holds for an element that is a BIT or
      *> NATIONAL group receiving as an elementary item (13.18.29.4 GR1b/GR2b): BG (2) and NG (3) are created
      *> and distributed (B1 = 10, B2 = 11; N1 = XY).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1411-DYN-VARGROUP-ELEMENT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SRC.
          05 SA PIC X(2) VALUE "ZZ".
          05 SB PIC X(3) OCCURS DYNAMIC CAPACITY IN SBC FROM 2.
       01 T.
          05 EL OCCURS DYNAMIC CAPACITY IN ELC FROM 1.
             10 EA PIC X(2).
             10 EB PIC X(3) OCCURS DYNAMIC CAPACITY IN EBC FROM 1.
       01 TB.
          05 BG OCCURS DYNAMIC CAPACITY IN BC GROUP-USAGE BIT.
             10 B1 PIC 1(2).
             10 B2 PIC 1(2).
       01 TN.
          05 NG OCCURS DYNAMIC CAPACITY IN NC GROUP-USAGE NATIONAL.
             10 N1 PIC N(2).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "B01" TO SB (1).
           MOVE "B02" TO SB (2).
           MOVE SRC TO EL (3).
           DISPLAY "ELC=" ELC.
           DISPLAY "EA3=[" EA (3) "] EB31=[" EB (3, 1) "] EB32=[" EB (3, 2) "]".
           MOVE B"1011" TO BG (2).
           DISPLAY "BC=" BC " B1=" B1 (2) " B2=" B2 (2).
           MOVE N"XY" TO NG (3).
           DISPLAY "NC=" NC " N1=" N1 (3).
           STOP RUN.
