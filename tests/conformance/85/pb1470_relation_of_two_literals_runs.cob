       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1470G1.
      *> ISO 8.8.4.2.1 - "A relation condition shall contain at least
      *> one reference to an operand that is not a literal."  The
      *> sentence is neither a general format nor a syntax rule, so
      *> ISO 4.2.2 asks the warning mechanism to indicate it: "This
      *> warning mechanism shall indicate violations of such rules."
      *> Each relation below compares two literals, draws warning
      *> COBOLNET3146, and still COMPILES and RUNS, its value the one
      *> the 8.8.4.2 comparison rules give (docs/CONFORMANCE.md
      *> D-RELLITERAL; kb/Work PB1470).  A figurative constant is a
      *> literal (8.3.3.6 is a clause of 8.3.3 Literals), and so is a
      *> symbolic-character, "a user-defined figurative constant"
      *> (8.3.2.2.29).
      *>
      *> T01  1 = 1                     true
      *> T02  1 < 2                     true
      *> T03  "A" = SPACE               false: SPACE is one space here
      *> T04  ZERO = ZERO               true
      *> T05  "AB" = "AB "              true: the shorter operand is
      *>                                padded with spaces (8.8.4.2.7
      *>                                rule 2)
      *> T06  SB = "B"                  true: SB IS 67, the 67th
      *>                                character of the native set
      *> T07  1 = N OR 2  (N = 1)       true: 1 = N holds
      *> T08  1 = N OR 2  (N = 3)       false: neither 1 = 3 nor the
      *>                                abbreviated 1 = 2 (8.8.4.12)
      *> T09  PERFORM UNTIL 1 = 1       14.9.28.4 GR10: "If the
      *>                                condition is true when the
      *>                                PERFORM statement is entered,
      *>                                and the TEST BEFORE phrase is
      *>                                specified or implied, no
      *>                                transfer to the specified set
      *>                                of statements takes place":
      *>                                C stays 0
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SB IS 67.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 1.
       01 C PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       P0.
           IF 1 = 1 DISPLAY "T01 Y" ELSE DISPLAY "T01 N" END-IF.
           IF 1 < 2 DISPLAY "T02 Y" ELSE DISPLAY "T02 N" END-IF.
           IF "A" = SPACE DISPLAY "T03 Y" ELSE DISPLAY "T03 N" END-IF.
           IF ZERO = ZERO DISPLAY "T04 Y" ELSE DISPLAY "T04 N" END-IF.
           IF "AB" = "AB " DISPLAY "T05 Y" ELSE DISPLAY "T05 N"
           END-IF.
           IF SB = "B" DISPLAY "T06 Y" ELSE DISPLAY "T06 N" END-IF.
           IF 1 = N OR 2 DISPLAY "T07 Y" ELSE DISPLAY "T07 N" END-IF.
           MOVE 3 TO N.
           IF 1 = N OR 2 DISPLAY "T08 Y" ELSE DISPLAY "T08 N" END-IF.
           PERFORM UNTIL 1 = 1
               ADD 1 TO C
           END-PERFORM.
           DISPLAY "T09 " C.
           STOP RUN.
