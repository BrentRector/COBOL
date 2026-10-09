      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1935 (train 1047 review): a class condition prints
      *> identifier-1 alone (ISO 8.8.4.4.2); (N) is an arithmetic
      *> expression (8.8.1.2 GR1), a value, not the data item whose
      *> characters the condition tests. It used to answer from the
      *> decoded number: N over "AB " read TRUE for IS NUMERIC.
      *> COBOLNET3318 at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEG1935C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC X(3) VALUE "AB ".
       01 N REDEFINES R PIC 9(3).
       PROCEDURE DIVISION.
           IF (N) IS NUMERIC
               DISPLAY "NUMERIC"
           END-IF
           STOP RUN.
