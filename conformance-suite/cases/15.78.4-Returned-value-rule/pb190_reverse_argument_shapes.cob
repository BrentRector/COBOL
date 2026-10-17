      *>--- conformance-case
      *> id: pb190_reverse_argument_shapes
      *> rules: [RV-15.78.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB190 - FUNCTION REVERSE, ISO 15.78.4 r1: "If argument-1 is a character string of length n, the
      *> returned value is a character string of length n such that for 1 <= j <= n, the character in position j
      *> of the returned value is the character from position n - j + 1 of argument-1."
      *>
      *> Row RV-15.78.4-1 held a GAP open on THREE legal argument-1 shapes that were recorded as compiling clean
      *> and then throwing NotImplementedCobolFeatureException at run time, producing no returned value at all.
      *> All three are re-measured here, because a row must not be closed from a code reading of the landings
      *> that look like they fixed it.
      *>   FIG - a FIGURATIVE CONSTANT. 8.4.3.2.3 SR8 admits a literal as an argument; 8.3.3.6.4 GR1 makes SPACE
      *>         an alphanumeric character value and GR3 b) fixes its length at ONE character position, so
      *>         n = 1 and r1's returned value is that same single space; into PIC X(10) it pads to ten spaces.
      *>   ALL - an ALL literal. 8.3.3.6.4 GR3 c) gives `ALL "AB"` the length of literal-1, so n = 2 and the
      *>         returned value is "BA" - position 1 takes position 2, position 2 takes position 1.
      *>   GRP - an alphanumeric GROUP containing a non-DISPLAY leaf. 8.5.2.1 makes an alphanumeric group item
      *>         class AND category alphanumeric, which 15.78.3 r1 admits, so the group's whole image is a legal
      *>         argument-1 whatever its leaves are. The image of `X(3)` + `S9(4) COMP-5` is FIVE character
      *>         positions and two of them are not printable, so the fixture asserts r1 itself - POSITION BY
      *>         POSITION against the same group's image - rather than pinning bytes: r1 holds for this argument
      *>         exactly when reversed(j) = image(n - j + 1) for all five j. That is the rule written out, and it
      *>         is independent of what the group image happens to be (a separate rule, a separate row).
      *>
      *> Surrogates need no special handling: 8.5.1.4 item 2 makes each two-octet UTF-16 code element "treated
      *> in COBOL as though it were itself a character", so a code-unit reversal IS r1's character reversal.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB190REVSHAPES.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G2.
          05 G2-A PIC X(3) VALUE "ABC".
          05 G2-N PIC S9(4) COMP-5 VALUE 4660.
       01 W-IMG PIC X(5).
       01 W-REV PIC X(5).
       01 W-X   PIC X(10).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION REVERSE(SPACE) TO W-X
           DISPLAY "FIG=[" W-X "]"
           MOVE FUNCTION REVERSE(ALL "AB") TO W-X
           DISPLAY "ALL=[" W-X "]"
           MOVE G2 TO W-IMG
           MOVE FUNCTION REVERSE(G2) TO W-REV
           IF W-REV(1:1) = W-IMG(5:1) AND W-REV(2:1) = W-IMG(4:1)
              AND W-REV(3:1) = W-IMG(3:1) AND W-REV(4:1) = W-IMG(2:1)
              AND W-REV(5:1) = W-IMG(1:1)
              DISPLAY "GRP=R1-HOLDS"
           ELSE
              DISPLAY "GRP=R1-FAILS"
           END-IF
           STOP RUN.
