*>--- conformance-case
*> id: pb65_boolean_expression_argument
*> rules: [GR-15.2-2, AR-15.3-3, AR-15.45.3-1, FMT-15.45.2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB65 (FMT-15.45.2). ISO §8.4.3.2.3 SR8: "Argument-1 shall be an
      *> identifier, a literal, a boolean expression, or an arithmetic
      *> expression"; §15.3 item 3 admits for a Boolean argument "a boolean
      *> expression or literal, or an elementary boolean data item"; §8.3.3.4.4
      *> GR4: "If boolean-character-1 is not specified, the literal is a
      *> zero-length literal" — B"" is a boolean literal, and §15.45.4 makes
      *> INTEGER-OF-BOOLEAN of it 0. Before this the grammar's functionArgument
      *> had no boolean-expression alternative (COBOLNET1639 "B-AND is not
      *> defined" + a false arity error) and B"" did not lex. Expected values
      *> are the bit arithmetic: 1100 B-AND 1010 = 1000 = 8; B-OR = 1110 = 14;
      *> B-NOT 1100 = 0011 = 3; BOOLEAN-OF-INTEGER(5, 8) = 00000101, B-AND
      *> 00000111 = 00000101 (the argument list's ')' ends the argument — the
      *> following B-AND belongs to the COMPUTE, not to the numeric argument 5).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB65BOOLARG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BIT-A PIC 1(4) USAGE BIT VALUE B"1100".
       01 BIT-B PIC 1(4) USAGE BIT VALUE B"1010".
       01 BR    PIC 1(8) USAGE BIT.
       01 BZ    PIC 1(4) VALUE B"".
       01 R     PIC 9(6).
       PROCEDURE DIVISION.
           COMPUTE R = FUNCTION INTEGER-OF-BOOLEAN(BIT-A B-AND BIT-B).
           DISPLAY "T1 IOB(A B-AND B)=" R.
           COMPUTE R = FUNCTION INTEGER-OF-BOOLEAN(BIT-A B-OR BIT-B).
           DISPLAY "T2 IOB(A B-OR B)=" R.
           COMPUTE R = FUNCTION INTEGER-OF-BOOLEAN(B-NOT BIT-A).
           DISPLAY "T3 IOB(B-NOT A)=" R.
           COMPUTE R = FUNCTION INTEGER-OF-BOOLEAN(BIT-A).
           DISPLAY "T4 IOB(A)=" R.
           COMPUTE R = FUNCTION INTEGER-OF-BOOLEAN(B"0111").
           DISPLAY "T5 IOB(B0111)=" R.
           COMPUTE BR = FUNCTION BOOLEAN-OF-INTEGER(5, 8) B-AND B"00000111".
           DISPLAY "T6 BOI(5 8) B-AND=" BR.
           COMPUTE R = FUNCTION INTEGER-OF-BOOLEAN(B"").
           DISPLAY "T7 IOB(B empty)=" R.
           DISPLAY "T8 VALUE B empty=[" BZ "]".
           STOP RUN.
