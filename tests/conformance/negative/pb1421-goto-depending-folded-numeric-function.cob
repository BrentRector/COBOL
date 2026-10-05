      *> reject-at: 2002 2014 2023
      *> Train 1021 review finding C-3 (kb/Work PB1421) - HIGHEST-ALGEBRAIC of a numeric-edited item is a
      *> NUMERIC function: argument-1 PIC ZZ9 is class alphanumeric, an "Alphanumeric" argument (15.3), and
      *> the 15.43.1 table maps "Alphanumeric | Numeric". 8.4.3.2.3 SR11: "A numeric function shall not be
      *> specified where an integer operand is required, even though a particular reference of the numeric
      *> function might yield an integer value", and 14.9.17.3 SR1 requires an integer. The compiler folds
      *> the call to 999 at compile time; the fold must keep the function's type, not answer by the
      *> literal's form. It used to compile and fall through.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1421FN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 E PIC ZZ9.
       PROCEDURE DIVISION.
       MAIN-P.
           GO TO P1 P2 DEPENDING ON
               FUNCTION HIGHEST-ALGEBRAIC(E).
           STOP RUN.
       P1.
           STOP RUN.
       P2.
           STOP RUN.
