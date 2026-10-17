      *>--- conformance-case
      *> id: pb877_subscripted_subscript
      *> rules: [SR-8.4.2.3.3-2, SR-8.4.2.3.3-3, GR-8.4.2.3.4-1, FMT-8.4.2.3.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB877 - a SUBSCRIPT that is itself a SUBSCRIPTED IDENTIFIER, on every side of a statement.
      *>
      *> THE RULE. ISO/IEC 1989:2023 8.4.2.3.2 prints the `subscript` general format as exactly three
      *> alternatives - ALL, arithmetic-expression-1, and index-name-1 [ {+|-} integer-1 ] - and 8.8.1.1
      *> defines an arithmetic expression as, among other things, "an identifier referencing a numeric data
      *> item". 8.4.3.1.2 Format 2 makes that identifier a qualified-data-name-WITH-SUBSCRIPTS, and
      *> 8.4.2.3.3 SR2 says when it may carry them: the entry "shall contain an OCCURS clause OR SHALL BE
      *> SUBORDINATE TO a data description entry that contains an OCCURS clause". X below carries no OCCURS
      *> clause of its own and is subordinate to one, so X(1) is a legal identifier and therefore a legal
      *> arithmetic-expression-1 subscript. 8.4.2.3.3 SR3 fixes the COUNT - one subscript per OCCURS clause
      *> in the description of the table element - so Y (X(1)) writes exactly ONE subscript, not two.
      *>
      *> Every expected value below is computed from the rules, never from a run:
      *>   8.4.2.3.4 GR1b - the subscript is THE RESULT of evaluating arithmetic-expression-1, so with
      *>                    X(1) = 2 the reference Y (X(1)) selects occurrence 2, identically to Y (2).
      *>   14.9.25.4 GR4  - MOVE ALL "A" TO G2(1) is a group move: 12 character positions filled with "A".
      *>   14.9.20.4     - INITIALIZE with no REPLACING sets each elementary item to the value its category
      *>                    takes: a numeric item to ZERO, an alphanumeric item to SPACE.
      *>
      *> The first pair of lines is the shape of GnuCOBOL's run_initialize:298, which battery #77 caught
      *> regressing to COBOLNET0899 - INITIALIZE Y (X(1)) beside INITIALIZE Y (2) in one program.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB877SUBSCRIPT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
         02 G2          OCCURS 3.
           03 X         PIC 9.
           03 Y.
             04 Y-REC   OCCURS 5.
              05  Y1    PIC 9 VALUE 9.
              05  Y2    PIC X VALUE 'Y'.
           03 Z         PIC X VALUE 'Z'.
       01 T.
         02 T-G         OCCURS 2.
           03 T-N       PIC 9.
           03 T-E       OCCURS 4 ASCENDING KEY IS T-K INDEXED BY TX.
             04 T-K     PIC 9.
             04 T-V     PIC X.
       01 P             PIC X(8) VALUE "ABCDEFGH".
       01 W             PIC X(2).
       01 N             PIC 9(2).
       01 R             PIC X(4).
       01 I             PIC 9.
       PROCEDURE DIVISION.
      *> 1. The differential's own shape: a literal subscript and a subscripted-identifier subscript,
      *>    on the SAME receiving operand, in one program. G is 36 positions: three 12-position G2
      *>    occurrences of X(1) + Y(10) + Z(1).
           MOVE ALL "A" TO G2(1)
           MOVE ALL "B" TO G2(2)
           MOVE ALL "C" TO G2(3)
           INITIALIZE Y (2)
           DISPLAY "1 " G
           MOVE ALL "A" TO G2(1)
           MOVE ALL "B" TO G2(2)
           MOVE ALL "C" TO G2(3)
           MOVE 2       TO X(1)
           INITIALIZE Y (X(1))
           DISPLAY "2 " G
      *> 2. One receiving family per line, each selecting occurrence 3 through X(1) = 3, read back with
      *>    the literal subscript 3 so the witness is never the same reference shape it verifies.
           MOVE ALL "A" TO G2(1)
           MOVE ALL "B" TO G2(2)
           MOVE ALL "C" TO G2(3)
           MOVE 3       TO X(1)
           MOVE "R"     TO Y2 OF Y-REC (X(1), 2)
           DISPLAY "3 MOVE     " Y2 OF Y-REC (3, 2)
           MOVE 1       TO Y1 OF Y-REC (X(1), 3)
           ADD  4       TO Y1 OF Y-REC (X(1), 3)
           DISPLAY "4 ADD      " Y1 OF Y-REC (3, 3)
           COMPUTE Y1 OF Y-REC (X(1), 4) = 7
           DISPLAY "5 COMPUTE  " Y1 OF Y-REC (3, 4)
           STRING "S" DELIMITED BY SIZE INTO Y2 OF Y-REC (X(1), 5)
           DISPLAY "6 STRING   " Y2 OF Y-REC (3, 5)
           UNSTRING P DELIMITED BY "B" INTO Y2 OF Y-REC (X(1), 1)
           DISPLAY "7 UNSTRING " Y2 OF Y-REC (3, 1)
      *> 3. The SENDING side of the same shape - it compiled clean and aborted at run time before PB877.
      *>    Y2 OF Y-REC (3,2) holds "R" from line 3; occurrence 3 - 1 = 2 holds the "B" the group move put
      *>    there, which is 8.4.2.3.4 GR1b's "result of the evaluation" over a compound expression.
           DISPLAY "8 SEND     " Y2 OF Y-REC (X(1), 2)
           DISPLAY "9 RELATIVE " Y2 OF Y-REC (X(1) - 1, 2)
      *> 4. The subscripted identifier in the other two positions that read a subscript list: a
      *>    reference-modification leftmost character position (8.4.3.3) and an intrinsic-function
      *>    argument (15.3). P (3:2) is "CD"; Y OF G2 (3) is the 10-position group.
           MOVE P (X(1):2) TO W
           DISPLAY "A REFMOD   " W
           COMPUTE N = FUNCTION LENGTH (Y OF G2 (X(1)))
           DISPLAY "B FUNCARG  " N
      *> 5. SEARCH ALL reads identifier-1's subscript list as SOURCE TEXT (14.9.37.3 SR8/SR9), through the
      *>    same splitter - so its OUTER subscript may be a subscripted identifier too. T-G(1) carries the
      *>    keys 1..4 against "a".."d" and T-G(2) the same keys against "w".."z"; T-N(1) = 2 selects
      *>    T-G(2), so the key 3 is found at its occurrence 3 and T-V there is "y". Reading "c" would mean
      *>    the outer subscript had been lost.
      *>    The subject writes ONLY the enclosing occurrence: 14.9.37.3 SR2 says "identifier-1 shall not be
      *>    subscripted at the level for which the SEARCH is applicable", and 8.4.2.3.3 SR5 a) exempts "the
      *>    subject of a SEARCH statement" from the one-subscript-per-OCCURS count for that reason - the
      *>    searched occurrence is the one the statement itself varies TX over (14.9.37.4 GR1). The WHEN
      *>    operands DO carry TX, because SR8 requires them to.
           MOVE 2 TO T-N (1)
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 4
              MOVE I TO T-K (1, I)
              MOVE I TO T-K (2, I)
           END-PERFORM
           MOVE "a" TO T-V (1, 1)
           MOVE "b" TO T-V (1, 2)
           MOVE "c" TO T-V (1, 3)
           MOVE "d" TO T-V (1, 4)
           MOVE "w" TO T-V (2, 1)
           MOVE "x" TO T-V (2, 2)
           MOVE "y" TO T-V (2, 3)
           MOVE "z" TO T-V (2, 4)
           MOVE "none" TO R
           SEARCH ALL T-E (T-N (1))
               AT END MOVE "miss" TO R
               WHEN T-K (T-N (1), TX) = 3
                    MOVE T-V (T-N (1), TX) TO R
           END-SEARCH
           DISPLAY "C SEARCH   " R
           STOP RUN.
