      *> PB2695 - a subscript, or an OCCURS DEPENDING count, whose VALUE is past 64 bits is still the value the
      *> program holds (8.4.2.3.4 GR2): "If the value of the subscript is not a positive integer or is less than one
      *> or is greater than the highest permissible occurrence number, the EC-BOUND-SUBSCRIPT exception condition is
      *> set to exist." The runtime narrows an occurrence number to a saturating long so that stays true; this golden
      *> pins the CONDITION on every wide item shape the narrowing reads (the message text it names the value in is
      *> asserted by WideSubscriptDiagnosticTests, because a COBOL program cannot read it).
      *> K = 2 holds 21 digits and selects EL(2): a wide item in range is not an error. A = 10**20 + 1 and
      *> N = -(10**20 + 1) are outside 1..3 (above the highest and not a positive integer), and S = 10**20 + 1.50 is
      *> not an integer (8.4.2.3.4 GR1b): each sets EC-BOUND-SUBSCRIPT, the USE declarative displays the condition and
      *> RESUME AT NEXT STATEMENT (14.9.33) leaves the receiver unchanged. The ODO table ODE has DEPENDING ON W = 10**20,
      *> outside 1..3 (13.18.38.4 GR7): a reference to its element sets EC-BOUND-ODO.
       >>TURN EC-BOUND-SUBSCRIPT EC-BOUND-ODO CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2695A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 I PIC 9 VALUE 9.
       01 TBL.
          05 EL PIC 9 OCCURS 3 TIMES.
       01 K PIC 9(21) VALUE 2.
       01 A PIC 9(21) VALUE 100000000000000000001.
       01 N PIC S9(21) VALUE -100000000000000000001.
       01 S PIC 9(21)V99 VALUE 100000000000000000001.50.
       01 W PIC 9(21) VALUE 100000000000000000000.
       01 OTBL.
          05 ODE PIC 9 OCCURS 1 TO 3 TIMES DEPENDING ON W.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-BOUND-SUBSCRIPT
               EC-BOUND-ODO.
       H-P.
           DISPLAY "CAUGHT=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           MOVE 1 TO EL(1)
           MOVE 2 TO EL(2)
           MOVE 3 TO EL(3)
           MOVE 9 TO I
           MOVE EL(K) TO I
           DISPLAY "EL(K)  I=" I
           MOVE 9 TO I
           MOVE EL(A) TO I
           DISPLAY "EL(A)  I=" I " (unchanged)"
           MOVE EL(N) TO I
           DISPLAY "EL(N)  I=" I " (unchanged)"
           MOVE EL(S) TO I
           DISPLAY "EL(S)  I=" I " (unchanged)"
           MOVE ODE(1) TO I
           DISPLAY "ODE(1) I=" I " (unchanged)"
           STOP RUN.
