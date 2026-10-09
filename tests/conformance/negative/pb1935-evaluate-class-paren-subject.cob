      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1935 (train 1047 review): the EVALUATE twin of the
      *> class-condition case. WHEN IS NUMERIC over the subject (N)
      *> forms the class condition (N) IS NUMERIC (ISO 14.9.13.4 4)
      *> a)), whose subject is an arithmetic expression, not the
      *> identifier-1 8.8.4.4.2 prints. COBOLNET3318 at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEG1935F.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC X(3) VALUE "AB ".
       01 N REDEFINES R PIC 9(3).
       PROCEDURE DIVISION.
           EVALUATE (N)
               WHEN IS NUMERIC
                   DISPLAY "NUMERIC"
               WHEN OTHER
                   DISPLAY "OTHER"
           END-EVALUATE
           STOP RUN.
