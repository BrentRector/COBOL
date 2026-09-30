*> reject-at: 2002 2014 2023
*> ISO/IEC 1989:2023 7.3.13.3 SR3 (cite.py --check 7.3.13.3 ">>WHEN operand-2 [THROUGH operand-3] shall begin on a new line and shall
*> be specified entirely on that line." -> OK 3)): the operand `2 +` is continued on the next line, which is malformed, although an
*> earlier WHEN matched. kb/Work PB1364. Fixed form.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1364N06.
       PROCEDURE DIVISION.
       >>EVALUATE 1
       >>WHEN 1
           DISPLAY "W1".
       >>WHEN 2 +
           3
           DISPLAY "W2".
       >>END-EVALUATE
           STOP RUN.
