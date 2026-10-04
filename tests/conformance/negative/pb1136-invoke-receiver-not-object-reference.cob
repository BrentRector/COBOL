      *> reject-at: 2002 2014 2023
      *> ISO §14.9.23.3 SR1: "Identifier-1 shall be an object reference."
      *> X5 is PIC X(5), so the INVOKE is refused under SR1 - the diagnostic
      *> used to print SR3 (the object-class-name rule).  kb/Work PB1136.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1136N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X5 PIC X(5).
       PROCEDURE DIVISION.
       MAIN.
           INVOKE X5 "GREET".
           STOP RUN.
       END PROGRAM PB1136N1.
