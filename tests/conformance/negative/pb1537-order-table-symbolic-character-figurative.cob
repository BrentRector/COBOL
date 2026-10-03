      *> reject-at: 2002 2014 2023
      *> kb/Work PB1537 - ISO 12.3.7.3 SR11: literal-9 "shall specify neither a symbolic-character figurative constant
      *> nor a zero-length literal" (cite.py --check 12.3.7.3 "Literal-1, literal-2, literal-3, literal-4, literal-5,
      *> literal-6, and literal-9 shall specify neither a symbolic-character figurative constant nor a zero-length
      *> literal"). ALL SB is the symbolic-character figurative constant (8.3.3.6.2 Format 7) of the SYMBOLIC CHARACTERS
      *> name SB: the one kind of figurative constant SR11 bars, so it is refused - UNDER SR11, with the symbolic
      *> character named. (SPACE and QUOTE are legal literal-9 operands: pb1537_figurative_literal_clauses.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGPB1537A.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SB IS 66
           ORDER TABLE T1 IS ALL SB.
       DATA DIVISION.
       PROCEDURE DIVISION.
           STOP RUN.
