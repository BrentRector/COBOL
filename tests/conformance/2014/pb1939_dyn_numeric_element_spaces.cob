      *> kb/Work PB1939 - a NUMERIC element of a dynamic-capacity table holds the SPACES a MOVE leaves in it.
      *> ISO 14.6.9.2 2): "If the receiving table is a dynamic-capacity table specifying a minimum capacity
      *>   that is higher than its current capacity, further elements are created and filled with spaces
      *>   until the current capacity of the table is equal to its minimum capacity."
      *> ISO 14.6.9.4: "... the current capacity of the dynamic table is unaffected, and each element of the
      *>   dynamic table is space-filled."  (cite.py --check 14.6.9.2 / 14.6.9.4 -> OK)
      *> A native value carrier holds a number, so the element used to read back 0000 where it holds spaces.
      *> A: a 1-element sender into a receiver with FROM 3 (14.9.25.4 GR9 a, 14.6.9.2): element 1 is moved by
      *>    the elementary MOVE rules (S9(4) 12 -> 0012); elements 2 and 3 are created and space filled.
      *> B: a SHORTER fixed group into the same receiver (14.9.25.4 GR9 b, 8.5.1.12.2 "treated as if it
      *>    corresponds to a space-filled fixed-length table"): the capacity stays 3 and every element,
      *>    which held 0007, is space filled (14.6.9.4).
      *> C: the same excess-part fill over a table of GROUP elements: each numeric member holds spaces.
      *> D: a group ELEMENT as a whole MOVE receiver (14.9.25.4 GR4, "without consideration for the
      *>    individual elementary or group items"): MOVE SPACES TO DTE (1) leaves spaces in its numeric member.
      *> E: the space-filled element is still a numeric item for a later numeric store: 9 + 1 -> 0010.
      *>
      *>   A=H2 0000000003 [0012][    ][    ]
      *>   B=G3 0000000003 [    ][    ][    ]
      *>   C=G3 0000000002 [   ][   ]
      *>   D=[   ][  ]
      *>   E=[0010]
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1939DYN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G2.
          05 H2 PIC X(2) VALUE "H2".
          05 S2 PIC S9(4) OCCURS DYNAMIC CAPACITY IN C2 FROM 1.
       01 GR2.
          05 HR2 PIC X(2).
          05 SR2 PIC 9(4) OCCURS DYNAMIC CAPACITY IN CS3 FROM 3.
       01 G3.
          05 H3 PIC X(2) VALUE "G3".
       01 GD.
          05 HD PIC X(2).
          05 DT OCCURS DYNAMIC CAPACITY IN CD FROM 2.
             10 DX PIC 9(3).
             10 DY PIC X(2).
       01 GE.
          05 DTE OCCURS DYNAMIC CAPACITY IN CE FROM 1.
             10 EX PIC 9(3).
             10 EY PIC X(2).
       PROCEDURE DIVISION.
           MOVE 12 TO S2 (1).
           MOVE G2 TO GR2.
           DISPLAY "A=" HR2 " " CS3 " [" SR2 (1) "][" SR2 (2) "]["
               SR2 (3) "]".
           MOVE 7 TO SR2 (1) SR2 (2) SR2 (3).
           MOVE G3 TO GR2.
           DISPLAY "B=" HR2 " " CS3 " [" SR2 (1) "][" SR2 (2) "]["
               SR2 (3) "]".
           MOVE 5 TO DX (1) DX (2).
           MOVE G3 TO GD.
           DISPLAY "C=" HD " " CD " [" DX (1) "][" DX (2) "]".
           MOVE 5 TO EX (1).
           MOVE "AB" TO EY (1).
           MOVE SPACES TO DTE (1).
           DISPLAY "D=[" EX (1) "][" EY (1) "]".
           MOVE 9 TO SR2 (2).
           ADD 1 TO SR2 (2).
           DISPLAY "E=[" SR2 (2) "]".
           STOP RUN.
       END PROGRAM PB1939DYN.
