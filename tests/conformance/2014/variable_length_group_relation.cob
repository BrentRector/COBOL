      *> ISO 8.8.4.2.17 (kb/Work PB1467): "A comparison of two compatible groups, one or both of which is a
      *> variable-length group, proceeds from left to right as described under 8.8.4.2.7, Comparison of
      *> alphanumeric operands" except that corresponding dynamic-length items compare at their CURRENT length
      *> (8.5.1.10.4) and corresponding tables compare element by element, the larger table's remaining elements
      *> against spaces (14.6.9.3). Each expected line is that rule applied by hand. Before PB1467 every one of
      *> these relations aborted the run unit ("whole-group image of G1 with a dynamic-length / dynamic-capacity
      *> member") - a variable-length group has no single image.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. VARIABLE-LENGTH-GROUP-RELATION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G1.
          05 G1A PIC X(2) VALUE "AB".
          05 G1D PIC X DYNAMIC LENGTH.
          05 G1C PIC X(2) VALUE "ZZ".
       01 G2.
          05 G2A PIC X(2) VALUE "AB".
          05 G2D PIC X DYNAMIC LENGTH.
          05 G2C PIC X(2) VALUE "ZZ".
       01 V1.
          05 VA PIC X(2) VALUE "AB".
          05 VT PIC X(2) OCCURS DYNAMIC CAPACITY IN VC FROM 0 TO 5.
          05 VZ PIC X VALUE "Z".
       01 F1.
          05 FA PIC X(2) VALUE "AB".
          05 FT PIC X(2) OCCURS 3.
          05 FZ PIC X VALUE "Z".
       PROCEDURE DIVISION.
       MAIN-PARA.
      *>   Two dynamic-length items at their current lengths: "HELLO" = "HELLO".
           MOVE "HELLO" TO G1D G2D
           IF G1 = G2 DISPLAY "D1 EQ" ELSE DISPLAY "D1 NE" END-IF
      *>   "HELLO" < "HELLP" at the fifth character.
           MOVE "HELLP" TO G2D
           IF G1 < G2 DISPLAY "D2 LT" ELSE DISPLAY "D2 GE" END-IF
      *>   "HELLO" against "HELL" extended with a space: "O" > " ", so G1 > G2 and not equal - the
      *>   contiguous images ("ABHELLOZZ" / "ABHELLZZ") would have said the opposite at the fifth position.
           MOVE "HELL" TO G2D
           IF G1 > G2 DISPLAY "D3 GT" ELSE DISPLAY "D3 LE" END-IF
           IF G1 NOT = G2 DISPLAY "D4 NE" ELSE DISPLAY "D4 EQ" END-IF
      *>   Equal components, so the fixed material after them decides: "ZY" < "ZZ".
           MOVE "HELL" TO G1D
           MOVE "ZY" TO G1C
           IF G1 < G2 DISPLAY "D5 LT" ELSE DISPLAY "D5 GE" END-IF
      *>   A dynamic-capacity table (capacity 2) against the corresponding fixed table (3 occurrences):
      *>   "CDEF" against "CDEF" + a space-filled third element - equal (14.6.9.3 compares it with spaces).
           MOVE "CD" TO VT(1) FT(1)
           MOVE "EF" TO VT(2) FT(2)
           MOVE SPACES TO FT(3)
           IF V1 = F1 DISPLAY "T1 EQ" ELSE DISPLAY "T1 NE" END-IF
      *>   The fixed table's third element "AA" against spaces: V1 < F1, in either operand order.
           MOVE "AA" TO FT(3)
           IF V1 < F1 DISPLAY "T2 LT" ELSE DISPLAY "T2 GE" END-IF
           IF F1 > V1 DISPLAY "T3 GT" ELSE DISPLAY "T3 LE" END-IF
      *>   Tables equal again; the trailing fixed item decides: "Y" < "Z".
           MOVE SPACES TO FT(3)
           MOVE "Y" TO VZ
           IF V1 < F1 DISPLAY "T4 LT" ELSE DISPLAY "T4 GE" END-IF
           STOP RUN.
