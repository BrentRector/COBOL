      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1077 - ISO 12.4.5.2 SR2: "A given file-name may be
      *> specified in only one SELECT clause within a factory, function,
      *> object, or program." F1 is selected twice. Before the fix the
      *> second entry silently replaced the first and the program wrote
      *> b1.dat only.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W73BSR2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "w73b_a1.dat".
           SELECT F1 ASSIGN TO "w73b_b1.dat".
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1 PIC X(10).
       PROCEDURE DIVISION.
           OPEN OUTPUT F1.
           MOVE "X" TO R1.
           WRITE R1.
           CLOSE F1.
           STOP RUN.
