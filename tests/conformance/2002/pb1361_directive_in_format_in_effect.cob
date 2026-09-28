      *> options: source-format=fixed
      * ISO/IEC 1989:2023 7.3.3 SR2/SR3, 6.3.2, 7.3.24.3 1) (kb/Work
      * PB1361): a directive line is recognized in the program-text
      * area of the reference format in effect - positions 8-72 in
      * fixed form whatever the sequence area holds, the whole line
      * in free form.
SEQ001 IDENTIFICATION DIVISION.
SEQ002 PROGRAM-ID. PB1361-DIRECTIVE-FORMAT.
SEQ003 PROCEDURE DIVISION.
SEQ004     DISPLAY "FIXED-PART-1".
SEQ005 >>SOURCE FORMAT FREE
DISPLAY "FREE-PART".
                                                                          >>SOURCE FORMAT FIXED
SEQ008     DISPLAY "FIXED-PART-2".
SEQ009     STOP RUN.
