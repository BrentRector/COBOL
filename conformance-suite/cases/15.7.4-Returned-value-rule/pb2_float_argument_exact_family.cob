*>--- conformance-case
*> id: pb2_float_argument_exact_family
*> rules: [RV-15.7.4-1, RV-15.44.4-1, RV-15.49.4-1, RV-15.71.4-1,
*>    RV-15.72.4-1, RV-15.76.4-1, RV-15.77.4-1, AR-15.81.3-1,
*>    RV-15.81.4-1, FMT-15.7.2, FMT-15.42.2, DOC-A.1-92]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB2 - a FLOATING-POINT argument to an exact-family intrinsic.
      *> ISO 15.7.3 r1 and its siblings require class NUMERIC; a COMP-2 item IS class numeric (8.5.2.1
      *> Table 2), so every call below is legal COBOL. Before this fix the emitter dispatched on the
      *> FUNCTION's family rather than the ARGUMENT's type and handed a double to an Int128 parameter, so
      *> ten of these produced a raw Roslyn CS1503 that escaped as a backend error on legal source.
      *>
      *> ⛔ MOD LEFT THE FLOAT PAIR AT kb/Work PB248, and the correction is about WHICH RULE MOD CARRIES, not
      *> about the class reading above. This header's "class numeric, therefore legal" reasoning holds for
      *> every 'n'-family row here - REM included, whose 15.77.3 r1 says "shall be of class numeric" - but
      *> MOD's own 15.64.3 r1 says "Argument-1 and argument-2 shall be INTEGERS", which is 15.3's argument
      *> TYPE 6, not a class: "an arithmetic expression that will always result in an integer value or an
      *> integer data item". A floating-point item is neither (14.6.8.3 sets its content to "the algebraic
      *> value of the sending operand", so its declared value set contains non-integers) - the same reading
      *> that has rejected a PIC 9V9 operand at an integer position since PB40. MOD therefore takes integer
      *> operands here; the FLOAT-operand coercion it used to prove is now proved under --permissive by
      *> FloatIntegerArgumentPermissiveTests, which is the lane that semantics moved to.
      *>
      *> Every expected value is DERIVED FROM THE SPEC, not observed: the sign-discriminating pairs are the
      *> point of the fixture, because a wrong body agrees with a right one on positive operands.
      *>   15.64.4 MOD          floored  - result takes the sign of argument-2 => MOD(-7,3)  =  2
      *>   15.77.4 REM          truncated- result takes the sign of argument-1 => REM(-7,3)  = -1
      *>   15.44   INTEGER      greatest integer NOT GREATER than the argument => INTEGER(-3.5)      = -4
      *>   15.49   INTEGER-PART the integer part, truncated toward zero        => INTEGER-PART(-3.5) = -3
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2FLOATARG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A    USAGE COMP-2 VALUE -7.0.
       01 B    USAGE COMP-2 VALUE 3.0.
      *> MOD's own rule (15.64.3 r1) says "Argument-1 and argument-2 shall be integers", so its operands are
      *> INTEGER items here; REM's (15.77.3 r1) says "shall be of class numeric", so REM keeps the float pair.
       01 AI   PIC S9(4) VALUE -7.
       01 BI   PIC S9(4) VALUE 3.
       01 C    USAGE COMP-2 VALUE -3.5.
       01 D    USAGE COMP-2 VALUE 3.5.
       01 E    USAGE COMP-2 VALUE 7.25.
       01 R    USAGE COMP-2.
       01 FX   PIC S9(4)V99 VALUE 0.
       PROCEDURE DIVISION.
           COMPUTE R = FUNCTION MOD(AI BI)
           DISPLAY R
           COMPUTE R = FUNCTION REM(A B)
           DISPLAY R
           COMPUTE R = FUNCTION INTEGER(C)
           DISPLAY R
           COMPUTE R = FUNCTION INTEGER-PART(C)
           DISPLAY R
           COMPUTE R = FUNCTION SIGN(C)
           DISPLAY R
           COMPUTE R = FUNCTION ABS(C)
           DISPLAY R
           COMPUTE R = FUNCTION FRACTION-PART(D)
           DISPLAY R
           COMPUTE R = FUNCTION MAX(D E)
           DISPLAY R
           COMPUTE R = FUNCTION MIN(D E)
           DISPLAY R
           COMPUTE R = FUNCTION SUM(D E)
           DISPLAY R
           COMPUTE R = FUNCTION MEAN(D E)
           DISPLAY R
           COMPUTE R = FUNCTION RANGE(D E)
           DISPLAY R
           COMPUTE R = FUNCTION MEDIAN(D E)
           DISPLAY R
           COMPUTE R = FUNCTION MIDRANGE(D E)
           DISPLAY R
           COMPUTE R = FUNCTION ORD-MAX(D E)
           DISPLAY R
           COMPUTE R = FUNCTION ORD-MIN(D E)
           DISPLAY R
           COMPUTE FX = FUNCTION ABS(C)
           DISPLAY FX
           STOP RUN.
