*>--- conformance-case
*> id: pb9_reserved_intrinsic_names
*> rules: [RV-15.81.4-1, FMT-15.81.2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB9 - the FOUR intrinsic function names that are ALSO reserved words, in the KEYWORD-OMITTED form.
      *> 8.4.3.2.3 SR2 lets the word FUNCTION be omitted for any intrinsic the REPOSITORY declares. 8.9 lists
      *> the reserved words and 8.11 the intrinsic function names; their intersection is exactly four -
      *> LENGTH, RANDOM, SIGN, SUM - and three of them were unreachable without the keyword: the parser's
      *> cobolWord funnel excluded them, so `COMPUTE N = SUM(1 2 3)` was COBOL0001. LENGTH already worked and
      *> is the precedent the other three now follow.
      *>
      *> ⛔ ADMITTING THEM IS SAFE FOR THE REASON D2's AMBIGUITY DOES NOT APPLY: a reserved word can never be a
      *> user-defined data name (8.3.2.4.1), so `SUM(x)` cannot be a subscripted data reference. The words stay
      *> funnel-gated, so each is still REJECTED in a definition slot - the negative fixture pins that, because
      *> a fix that opened them up as data names would pass this file and still be wrong.
      *>
      *> Both reference forms are exercised side by side: the keyword-omitted form and the FUNCTION-keyword
      *> form are the SAME reference and must yield the same value.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB9RESERVEDFN.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY. FUNCTION ALL INTRINSIC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A   PIC X(5) VALUE "ABCDE".
       01 NEG PIC S9(4)V9 VALUE -3.5.
       01 POS PIC S9(4)V99 VALUE 42.25.
       01 ZER PIC S9(4)V9 VALUE 0.
       01 N   PIC S9(4) SIGN IS LEADING SEPARATE.
       01 R   PIC 9V9(4).
       PROCEDURE DIVISION.
      *> LENGTH - 15.50.3: the number of character positions in argument-1. A PIC X(5) is 5.
           COMPUTE N = LENGTH(A)           DISPLAY "LEN-K=" N
           COMPUTE N = FUNCTION LENGTH(A)  DISPLAY "LEN-F=" N

      *> SIGN - 15.81.4 r1: (1) when argument-1 > 0, (0) when zero, (-1) when < 0.
           COMPUTE N = SIGN(NEG)           DISPLAY "SGN-NEG=" N
           COMPUTE N = SIGN(ZER)           DISPLAY "SGN-ZER=" N
           COMPUTE N = SIGN(POS)           DISPLAY "SGN-POS=" N
           COMPUTE N = FUNCTION SIGN(NEG)  DISPLAY "SGN-F=" N

      *> SUM - 15.88.4: the sum of the arguments. 1 + 2 + 3 = 6.
           COMPUTE N = SUM(1 2 3)          DISPLAY "SUM-K=" N
           COMPUTE N = FUNCTION SUM(1 2 3) DISPLAY "SUM-F=" N

      *> RANDOM - 15.75.4: a value in the range 0 <= value < 1, so the assertion is STRUCTURAL (the PB7
      *> convention for a value that is not reproducible), and it is the ZERO-ARGUMENT reference form.
           COMPUTE R = RANDOM
           IF R >= 0 AND R < 1 DISPLAY "RND-K-IN-RANGE"
              ELSE DISPLAY "RND-K-BAD" END-IF
           COMPUTE R = FUNCTION RANDOM
           IF R >= 0 AND R < 1 DISPLAY "RND-F-IN-RANGE"
              ELSE DISPLAY "RND-F-BAD" END-IF

      *> The same words in their RESERVED-KEYWORD roles must be untouched by the name-slot admission:
      *> SIGN IS LEADING SEPARATE is declared on N above and prints with its separate sign below.
           MOVE -7 TO N
           DISPLAY "SIGNCLAUSE=" N
           STOP RUN.
