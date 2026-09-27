      *> reject-at: 2002 2014 2023
      *> kb/Work PB1406 - ISO 12.3.7.3 SR11: "Literal-1, literal-2,
      *> literal-3, literal-4, literal-5, literal-6, and literal-9 shall
      *> specify neither a symbolic-character figurative constant nor a
      *> zero-length literal." The CLASS clause's literal-5 here is a
      *> concatenation expression with a symbolic-character operand:
      *> COBOLNET2474.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEG1406SSN.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-A IS 66
           CLASS CC IS "B" & SYM-A.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
