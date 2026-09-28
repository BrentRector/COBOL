      *> reject-at: 2023
      *> kb/Work PB1413 — ISO §8.8.2 rule 5: "The second operand shall be an integer operand." §5.5 2) a): "if that
      *> operand is a literal, it shall be an integer literal", and §8.3.3.3.2: "An integer literal is a fixed-point
      *> numeric literal that contains no decimal point." 1.5 is a single literal, so Table 4's shape is satisfied
      *> and the INTEGER half of rule 5 is what refuses it (COBOLNET2513; rule 5's first-operand half is
      *> COBOLNET1511). It compiled clean and shifted by 1 — the count truncated.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W66NSI.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A   PIC 1(4) VALUE B"1100".
       01 R   PIC 1(4).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = A B-SHIFT-L 1.5.
           DISPLAY R.
           STOP RUN.
