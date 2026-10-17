      *>--- conformance-case
      *> id: pb2938_wide_refmod_position_and_length
      *> rules: []
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> PB2938 - a reference-modifier leftmost-position or length whose
      *> VALUE is past 32 or 64 bits is still the value the program
      *> holds (8.4.3.3.4 5): "If the evaluation of leftmost-position
      *> or length results in a non-integer value, a zero value, or a
      *> value that references a position outside the area of
      *> identifier-1, the EC-BOUND-REF-MOD exception condition is set
      *> to exist." The runtime narrows a position to a saturating long
      *> so that stays true; this golden pins the CONDITION on every
      *> shape the narrowing reads (the message text that names the
      *> value is asserted by WideRefModDiagnosticTests, because a
      *> COBOL program cannot read it).
      *> K = 2 holds 21 digits and selects X(2:1): a wide item in
      *> range is not an error. A = 10**20 + 1 is outside the 10
      *> positions as a leftmost position and as a length, N is
      *> negative, B = 5 * 10**9 fits a long but not an int, and an
      *> expression (A + 5) and a literal past 64 bits are the same
      *> violation. Each sets EC-BOUND-REF-MOD, the USE declarative
      *> displays the condition and RESUME AT NEXT STATEMENT (14.9.33)
      *> leaves the receiver unchanged; the write reference X(A:1) and
      *> the length form X(1:A) are covered too.
       >>TURN EC-BOUND-REF-MOD CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2938A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(10) VALUE "ABCDEFGHIJ".
       01 Y PIC X VALUE ".".
       01 K PIC 9(21) VALUE 2.
       01 A PIC 9(21) VALUE 100000000000000000001.
       01 N PIC S9(21) VALUE -100000000000000000001.
       01 B PIC 9(10) VALUE 5000000000.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-BOUND-REF-MOD.
       H-P.
           DISPLAY "CAUGHT=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           MOVE X(K:1) TO Y
           DISPLAY "X(K:1)    Y=" Y
           MOVE ALL "." TO Y
           MOVE X(A:1) TO Y
           DISPLAY "X(A:1)    Y=" Y " (unchanged)"
           MOVE X(1:A) TO Y
           DISPLAY "X(1:A)    Y=" Y " (unchanged)"
           MOVE X(1:N) TO Y
           DISPLAY "X(1:N)    Y=" Y " (unchanged)"
           MOVE X(B:1) TO Y
           DISPLAY "X(B:1)    Y=" Y " (unchanged)"
           MOVE X(A + 5:1) TO Y
           DISPLAY "X(A+5:1)  Y=" Y " (unchanged)"
           MOVE X(100000000000000000001:1) TO Y
           DISPLAY "X(10**20+1:1) Y=" Y " (unchanged)"
           MOVE "Z" TO X(A:1)
           DISPLAY "X(A:1)    X=" X " (unchanged)"
           STOP RUN.
