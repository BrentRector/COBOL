      *> kb/Work PB1465 - a parenthesized boolean sub-expression may follow every binary boolean
      *> operator and B-NOT. ISO 1989:2023 8.8.2 GR6: "The permissible combinations of operands,
      *> operators, and parentheses in a boolean expression are specified in Table 4", whose rows
      *> "B-AND, B-OR, B-XOR" and "B-NOT" both mark "(" as a permissible second symbol.
      *> 8.8.4.3.4 GR1/GR2: the condition is true when the expression's value is 1, and NOT reverses
      *> it. Before the fix every leg below except the subscript control was COBOL0001: the lexer
      *> opened SUBSCRIPT mode at a "(" after B-OR / B-AND / B-XOR / B-NOT (user words at 85),
      *> although 8.9 reserves them from 2002 (8.3.2.1 GR1: reserved words shall not be user words).
      *> Expected values, derived (BW = 1, BZ = 0, BT = 0 1 0):
      *>  C1 BZ B-OR (BW B-AND BW)          0 OR 1          T
      *>  C2 B-NOT (BW B-XOR BZ)            NOT 1           F
      *>  C3 BW B-AND (BZ B-OR BZ)          1 AND 0         F
      *>  C4 BZ B-XOR (B-NOT BZ)            0 XOR 1         T
      *>  C5 BZ B-OR BT(2)                  0 OR 1          T  (a subscript still subscripts)
      *>  C6 BZ B-OR (BT(1) B-OR BT(2))     0 OR 1          T
      *>  K1 BZ B-OR (BW B-AND BW)          1
      *>  K2 B-NOT (BW B-XOR BZ)            0
      *>  K3 BW B-XOR (BW B-AND BZ)         1 XOR 0         1
      *>  K4 B-NOT (BZ B-OR (BZ B-AND BW))  NOT 0           1
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1465BPO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BW PIC 1 USAGE BIT VALUE B"1".
       01 BZ PIC 1 USAGE BIT VALUE B"0".
       01 BR PIC 1 USAGE BIT.
       01 BTAB.
          05 BT PIC 1 USAGE BIT OCCURS 3.
       PROCEDURE DIVISION.
           MOVE B"0" TO BT(1).
           MOVE B"1" TO BT(2).
           MOVE B"0" TO BT(3).
           IF BZ B-OR (BW B-AND BW)
               DISPLAY "C1 T" ELSE DISPLAY "C1 F" END-IF.
           IF B-NOT (BW B-XOR BZ)
               DISPLAY "C2 T" ELSE DISPLAY "C2 F" END-IF.
           IF BW B-AND (BZ B-OR BZ)
               DISPLAY "C3 T" ELSE DISPLAY "C3 F" END-IF.
           IF BZ B-XOR (B-NOT BZ)
               DISPLAY "C4 T" ELSE DISPLAY "C4 F" END-IF.
           IF BZ B-OR BT(2)
               DISPLAY "C5 T" ELSE DISPLAY "C5 F" END-IF.
           IF BZ B-OR (BT(1) B-OR BT(2))
               DISPLAY "C6 T" ELSE DISPLAY "C6 F" END-IF.
           COMPUTE BR = BZ B-OR (BW B-AND BW).
           DISPLAY "K1 " BR.
           COMPUTE BR = B-NOT (BW B-XOR BZ).
           DISPLAY "K2 " BR.
           COMPUTE BR = BW B-XOR (BW B-AND BZ).
           DISPLAY "K3 " BR.
           COMPUTE BR = B-NOT (BZ B-OR (BZ B-AND BW)).
           DISPLAY "K4 " BR.
           STOP RUN.
