*>--- conformance-case
*> id: l1_mod_rem_zero_divisor
*> rules: [AR-15.64.3-2, AR-15.77.3-2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.64.3 rule 2 (MOD) and §15.77.3 rule 2 (REM) — "The value of argument-2 shall not be zero."
      *>
      *> WHAT THE RULE MAKES OBSERVABLE. §15.3: "The rules for a function may place constraints on the
      *> permissible values for arguments … If the evaluation of an argument results in an incorrect value
      *> for that argument … according to the rules specified in the function definition and no exception
      *> condition was raised during item identification or expression evaluation, the EC-ARGUMENT-FUNCTION
      *> exception condition is set to exist." So a zero argument-2 is not a wrong-ANSWER question — it is an
      *> exception-condition question, and with checking ON the USE declarative below is the observable.
      *> Each CAUGHT line is required by the rule; a missing one is the rule unenforced on that path.
      *>
      *> THE CONTROLS, derived from the returned-value rules so a body that never computes anything cannot
      *> pass by raising on everything:
      *>   §15.64.4 r1 — MOD = ((a) - ((b) * FUNCTION INTEGER((a)/(b)))). a = -11, b = 5: -11/5 = -2.2 and
      *>       INTEGER returns the greatest integer not greater than it, -3, so -11 - (5 * -3) = 4. That is
      *>       also the §15.64.4 NOTE's own row for argument-1 -11, argument-2 5.
      *>   §15.77.4 r1 — REM = ((a) - ((b) * FUNCTION INTEGER-PART((a)/(b)))). INTEGER-PART truncates toward
      *>       zero, so INTEGER-PART(-2.2) = -2 and -11 - (5 * -2) = -1. MOD and REM therefore DIFFER on this
      *>       pair, which is what makes the two controls discriminating rather than decorative.
      *>   The floating-point control is the same REM computation on a class-numeric COMP-2 argument
      *>       (§15.77.3 r1 admits class numeric; §8.5.2.1 Table 2 puts every floating-point usage in class
      *>       numeric): 11 - (5 * INTEGER-PART(2.2)) = 11 - 10 = 1.
      *>
      *> ⛔ THE RULE HAS THREE CARRIERS AND ALL THREE MUST BE WITNESSED. Argument-2's zero-ness is a
      *> property of a VALUE, so the rule binds wherever that value is evaluated — and this compiler
      *> evaluates MOD/REM on three bodies: the exact Int128 pair (ModScaled/RemScaled), the binary64 pair
      *> (ModReal/RemReal) and the SDIDI pair (CobolIntrinsics.Dec.cs ModDec/RemDec). Statements 1-5 reach
      *> the exact pair, statements 6-7 the binary64 REM, statements 8-11 the SDIDI pair.
      *>
      *> ⚠ WHY STATEMENTS 8-11 WRITE ARGUMENT-1 AS `AM + 1` AND NOT AS A PLAIN ITEM — DO NOT "SIMPLIFY" IT.
      *> §15.3 type 6 admits either form for an integer argument: "An arithmetic expression that will always
      *> result in an integer value or an integer data item shall be specified" — AM + 1 over PIC S9(9) is
      *> the first form, ordinary legal source, and the rule under test says nothing about how argument-1 is
      *> written. But the two forms do not travel the same road in this compiler: under a standard
      *> arithmetic mode an arithmetic-expression operand arrives on the SDIDI carrier
      *> (NumericRenderer.Combine) and IntrinsicRenderer's RenderDec gate — `if (!alwaysDec &&
      *> !AnyDecOrRealRaw(ic)) return null;` — routes MOD/REM to their Dec bodies ONLY then. With two plain
      *> PIC S9(9) items, OPTIONS ARITHMETIC IS STANDARD-DECIMAL alone falls straight back to ModScaled and
      *> this unit would re-measure statements 1-5 while claiming a third carrier. The shape is the one
      *> conformance:cases/15.81.4-Returned-value-rule/pb32_dec_carrier_intrinsic_argument already proves compiles and computes
      *> (`FUNCTION MOD(J + 10, 7)` under the same OPTIONS); here it carries the zero divisor.
      *>
      *> §11.9.5.2 GR3 puts the ARITHMETIC clause of §11.9.5.1 in effect for the source element that writes
      *> it, and GR4 makes the absence of the clause equivalent to NATIVE — which is why the two units are
      *> two source elements rather than one. The single >>TURN covers both (the same arrangement as
      *> conformance:cases/14.9.4-CALL/pb133_arg_mismatch_checked).
      *>
      *> FIVE ZERO SHAPES, because argument-2's zero-ness is a property of its VALUE, not of how it was
      *> written: a VALUE-0 item, a zero produced by run-time arithmetic, a floating-point zero, and the
      *> VALUE-0 item again on the SDIDI carrier for each of MOD and REM.
       >>TURN EC-ARGUMENT-FUNCTION CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1MODREMZ.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A    PIC S9(9) VALUE -11.
       01 NZ   PIC S9(9) VALUE 5.
       01 Z    PIC S9(9) VALUE 0.
       01 ZR   PIC S9(9) VALUE 0.
       01 FA   USAGE COMP-2 VALUE 11.0.
       01 FNZ  USAGE COMP-2 VALUE 5.0.
       01 FZ   USAGE COMP-2 VALUE 0.0.
       01 SR   PIC -9(9).
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-ARGUMENT-FUNCTION.
       H-P.
           DISPLAY "  CAUGHT".
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           COMPUTE ZR = NZ - 5.
           DISPLAY "1-MOD-CONTROL".
           MOVE FUNCTION MOD(A NZ) TO SR.
           DISPLAY "  MOD-M11-5=" SR.
           DISPLAY "2-MOD-ZERO-VALUE".
           MOVE FUNCTION MOD(A Z) TO SR.
           DISPLAY "3-MOD-ZERO-COMPUTED".
           MOVE FUNCTION MOD(A ZR) TO SR.
           DISPLAY "4-REM-CONTROL".
           MOVE FUNCTION REM(A NZ) TO SR.
           DISPLAY "  REM-M11-5=" SR.
           DISPLAY "5-REM-ZERO-VALUE".
           MOVE FUNCTION REM(A Z) TO SR.
           DISPLAY "6-REM-FLOAT-CONTROL".
           MOVE FUNCTION REM(FA FNZ) TO SR.
           DISPLAY "  REM-FLOAT=" SR.
           DISPLAY "7-REM-FLOAT-ZERO".
           MOVE FUNCTION REM(FA FZ) TO SR.
           CALL "L1MRZSD".
           DISPLAY "DONE".
           STOP RUN.
       END PROGRAM L1MODREMZ.
      *> The SDIDI carrier — the same rule, the same operand values, the same controls.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1MRZSD.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
      *> AM + 1 is -11, written as a §15.3 type 6 arithmetic expression (see the header note).
       01 AM   PIC S9(9) VALUE -12.
       01 NZ   PIC S9(9) VALUE 5.
       01 Z    PIC S9(9) VALUE 0.
       01 SR   PIC -9(9).
       PROCEDURE DIVISION.
       DECLARATIVES.
       HS SECTION.
           USE AFTER EXCEPTION CONDITION EC-ARGUMENT-FUNCTION.
       HS-P.
           DISPLAY "  CAUGHT".
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAINS SECTION.
       MAINS-P.
           DISPLAY "8-SD-MOD-CONTROL".
           MOVE FUNCTION MOD(AM + 1, NZ) TO SR.
           DISPLAY "  SD-MOD-M11-5=" SR.
           DISPLAY "9-SD-MOD-ZERO".
           MOVE FUNCTION MOD(AM + 1, Z) TO SR.
           DISPLAY "10-SD-REM-CONTROL".
           MOVE FUNCTION REM(AM + 1, NZ) TO SR.
           DISPLAY "  SD-REM-M11-5=" SR.
           DISPLAY "11-SD-REM-ZERO".
           MOVE FUNCTION REM(AM + 1, Z) TO SR.
           GOBACK.
       END PROGRAM L1MRZSD.
