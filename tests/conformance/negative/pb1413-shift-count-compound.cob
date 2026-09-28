      *> reject-at: 2023
      *> kb/Work PB1413 — ISO §8.8.2 Table 4 permits exactly one symbol after a boolean shift operator, "Identifier
      *> or literal", and rule 5 makes it "an integer operand"; rule 2 then requires the expression to end there
      *> (or go on with a boolean operator or ')'). `A B-SHIFT-L 1 + 1` puts an arithmetic operator after the count,
      *> so it is not a boolean expression at all — COBOLNET1719, the Table 3 / Table 4 invalid-pair diagnostic. It
      *> compiled clean and shifted by 2: the grammar parses the count as a whole arithmetic expression and nothing
      *> narrowed it back.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W66NSC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A   PIC 1(4) VALUE B"1100".
       01 R   PIC 1(4).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = A B-SHIFT-L 1 + 1.
           DISPLAY R.
           STOP RUN.
