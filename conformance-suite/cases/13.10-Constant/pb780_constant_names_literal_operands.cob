*>--- conformance-case
*> id: pb780_constant_names_literal_operands
*> rules: [SR-13.10.3-2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB780 - a constant-name stands where the GRAMMAR used to spell a literal-only operand: the Format-2
      *> VALUE clause's subscript-1 / subscript-2 (13.18.63.2) and an inline method invocation's literal-1 (8.4.3.4.2).
      *> Both were parse errors (COBOL0309 at FROM (K2); COBOL0001 at :: KM).
      *>
      *> RULES (cite.py --check, all OK):
      *> 13.10.3 SR2: "Except in a compiler directive, constant-name-1 may be used anywhere that a format specifies a
      *>   literal of the class and category of constant-name-1."
      *> 13.10.4 GR1: the effect is "as if literal-1 ... were written where constant-name-1 is written".
      *> 13.18.63.3 SR19: "Subscript-1 and subscript-2 shall be integer numeric literals." - so K2 and K3, integer
      *>   constants, stand there.
      *>
      *> DERIVATION. T: 'A' 'B' FROM (K2) has no TO phrase, so it is as if TO (4) were written (13.18.63.4 GR14); the
      *> literals are reused in order until element 4 (GR13): elements 2, 3, 4 = A, B, A. 'Z' FROM (1) TO (1) sets
      *> element 1. T = "ZABA". U: 'P' sets element 1, 'Q' FROM (K2) TO (K3) elements 2 and 3, 'S' element 4: "PQQS".
      *> PB780IC :: KM (Y X) invokes the factory method CAT (KM's value) with Y = "BBB" and X = "AAA", which returns
      *> its two arguments joined: "BBBAAA".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB780LO.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB780IC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  K2 CONSTANT AS 2.
       01  K3 CONSTANT AS 3.
       01  KM CONSTANT AS "CAT".
       01  T.
           05  E PIC X OCCURS 4 VALUE "A" "B" FROM (K2)
               "Z" FROM (1) TO (1).
       01  U.
           05  F PIC X OCCURS 4 VALUE "P" FROM (1) TO (1)
               "Q" FROM (K2) TO (K3) "S" FROM (4).
       01  X   PIC X(3) VALUE "AAA".
       01  Y   PIC X(3) VALUE "BBB".
       01  R   PIC X(6) VALUE SPACES.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "T=" T.
           DISPLAY "U=" U.
           MOVE PB780IC :: KM (Y X) TO R.
           DISPLAY "R=" R.
           STOP RUN.
       END PROGRAM PB780LO.
       IDENTIFICATION DIVISION.
       CLASS-ID. PB780IC.
       IDENTIFICATION DIVISION.
       FACTORY.
       PROCEDURE DIVISION.
       METHOD-ID. CAT.
       DATA DIVISION.
       LINKAGE SECTION.
       01  L1 PIC X(3).
       01  L2 PIC X(3).
       01  LR PIC X(6).
       PROCEDURE DIVISION USING L1 L2 RETURNING LR.
           STRING L1 L2 DELIMITED BY SIZE INTO LR.
       END METHOD CAT.
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT.
       END OBJECT.
       END CLASS PB780IC.
