      *> kb/Work PB1162 - ISO 8.8.4.11.2 admits condition-2 after XOR /
      *>  EXCLUSIVE-OR, and 8.8.4.11.3 Table 5 lets '(' immediately
      *>  follow EXCLUSIVE-OR or XOR. Both words are reserved at 2023
      *>  (8.9), so the '(' can open no subscript. A = 1, B = 2, C = 3.
      *>  Derived: X1 (A = 1) XOR (B = 9) = T XOR F = T; X2 (A = 1)
      *>  EXCLUSIVE-OR (B = 9 OR C = 3) = T XOR T = F; X3 (A = 9) XOR (B
      *>  = 2) = F XOR T = T.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W73AXOR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9 VALUE 1.
       01 B PIC 9 VALUE 2.
       01 C PIC 9 VALUE 3.
       PROCEDURE DIVISION.
           IF A = 1 XOR (B = 9)
              DISPLAY "X1 T" ELSE DISPLAY "X1 F" END-IF.
           IF A = 1 EXCLUSIVE-OR (B = 9 OR C = 3)
              DISPLAY "X2 T" ELSE DISPLAY "X2 F" END-IF.
           IF (A = 9) XOR (B = 2)
              DISPLAY "X3 T" ELSE DISPLAY "X3 F" END-IF.
           STOP RUN.
