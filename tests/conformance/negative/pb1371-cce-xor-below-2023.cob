      *> reject-at: 2002 2014
      *> kb/Work PB1371 — a >>IF operand may be "A complex condition as specified in 8.8.4.9, Complex conditions"
      *> (ISO §7.3.8.2 SR1 d)) of the TARGETED edition, and the EXCLUSIVE-OR / XOR connective is a COBOL-2023
      *> addition (Annex E.2 item 25). Below 2023 an XOR in a constant conditional expression is therefore the same
      *> COBOLNET0900 its runtime twin (IF A = 1 XOR B = 1) draws, asked per fragment through the one
      *> LogicalOperatorGate. The directive itself is a 2002 construct, so 2002 and 2014 are exactly the editions
      *> where only the connective is out of edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W68NCX.
       PROCEDURE DIVISION.
       MAIN.
       >>IF 1 = 2 XOR 1 = 1
           DISPLAY "TRUE".
       >>END-IF
           STOP RUN.
