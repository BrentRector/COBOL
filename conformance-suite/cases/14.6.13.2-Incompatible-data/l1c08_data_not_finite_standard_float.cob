      *>--- conformance-case
      *> id: l1c08_data_not_finite_standard_float
      *> rules: [GR-14.6.13.2-3]
      *> accept: [2014]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.6.13.2 GR3 — EC-DATA-NOT-FINITE for a STANDARD float
      *> sending operand, and its class/sign/same-usage-MOVE exemptions
      *> GR3: "When a sending operand is described with a standard
      *>   floating-point usage, and the content of the sending operand
      *>   would evaluate to true in an infinity class condition or to
      *>   true in a FLOAT-NOT-A-NUMBER class condition, the
      *>   EC-DATA-NOT-FINITE exception condition is set to exist,
      *>   except in the following circumstances: - a sending item is
      *>   referenced in a class condition, or - a sending item is
      *>   referenced in a sign condition, or - the sending and
      *>   receiving items in a MOVE statement are defined with the same
      *>   standard floating-point usage specification, with the
      *>   exception of endianness, or - a sending item is processed in
      *>   a VALIDATE statement."
      *> cite.py:
      *>   OK  §14.6.13.2 3)  (Incompatible data)  [raise sentence]
      *>   OK  §14.6.13.2 3)  (Incompatible data)  [sign exemption]
      *>   OK  §14.6.13.2 3)  (Incompatible data)  [same-usage MOVE]
      *>   OK  §14.6.13.1.3 5)  (Fatal exception conditions) [USE
      *>       declarative executed; Table 13: EC-DATA-NOT-FINITE Fatal]
      *>   OK  §14.9.33.4 2) a)  (General rules) "the implicit CONTINUE
      *>       statement immediately follows the end of the statement
      *>       that was executing when control was transferred to the
      *>       exception processing procedure"
      *> Checking is ON; the USE declarative prints the EC and RESUMEs
      *> AT NEXT STATEMENT, so a raise shows as a CAUGHT= line and the
      *> rest of the raising statement (an IF branch) is skipped.
      *> FN holds a NaN, FI +infinity (SET CONTENT OF ... TO ...).
      *> VALIDATE is A.4 optional and not claimed: that exemption is
      *> not exercised.
      *> DERIVATION OF EVERY EXPECTED LINE:
      *> CLASS-NAN=Y   FN in a FLOAT-NOT-A-NUMBER class condition:
      *>               exempt (class condition) - no CAUGHT, true.
      *> CLASS-INF=Y   FI in a FLOAT-INFINITY class condition: exempt.
      *> SIGN-POS=Y    FI in a sign condition: exempt; +inf > 0.
      *> SAME-USAGE    MOVE FN to GL (FLOAT-BINARY-64 HIGH-ORDER-LEFT)
      *>               and to GR (FLOAT-BINARY-64 HIGH-ORDER-RIGHT):
      *>               same standard usage, endianness excepted - both
      *>               exempt, no CAUGHT line precedes this one.
      *> CAUGHT=EC-DATA-NOT-FINITE  MOVE FN TO K: K is FLOAT-BINARY-32,
      *>               a DIFFERENT standard usage - not exempt, raised.
      *> M1
      *> CAUGHT=EC-DATA-NOT-FINITE  MOVE FI TO N (numeric display):
      *>               not a float receiver - raised.
      *> M2
      *> CAUGHT=EC-DATA-NOT-FINITE  IF FI > 0: a relation condition is
      *>               neither a class nor a sign condition - raised;
      *>               RESUME skips to after END-IF, so no REL-TRUE.
      *> M3
      *> CAUGHT=EC-DATA-NOT-FINITE  COMPUTE N = FN + 1: arithmetic
      *>               operand - raised.
      *> DONE
       >>TURN EC-DATA-NOT-FINITE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C08K.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FN USAGE FLOAT-BINARY-64 HIGH-ORDER-LEFT.
       01 FI USAGE FLOAT-BINARY-64 HIGH-ORDER-LEFT.
       01 GL USAGE FLOAT-BINARY-64 HIGH-ORDER-LEFT.
       01 GR USAGE FLOAT-BINARY-64 HIGH-ORDER-RIGHT.
       01 K  USAGE FLOAT-BINARY-32.
       01 N  PIC S9(5).
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-DATA-NOT-FINITE.
       H-P.
           DISPLAY "CAUGHT=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           SET CONTENT OF FN TO FLOAT-NOT-A-NUMBER.
           SET CONTENT OF FI TO FLOAT-INFINITY.
           IF FN IS FLOAT-NOT-A-NUMBER
              DISPLAY "CLASS-NAN=Y"
           ELSE
              DISPLAY "CLASS-NAN=N"
           END-IF.
           IF FI IS FLOAT-INFINITY
              DISPLAY "CLASS-INF=Y"
           ELSE
              DISPLAY "CLASS-INF=N"
           END-IF.
           IF FI IS POSITIVE
              DISPLAY "SIGN-POS=Y"
           ELSE
              DISPLAY "SIGN-POS=N"
           END-IF.
           MOVE FN TO GL.
           MOVE FN TO GR.
           DISPLAY "SAME-USAGE".
           MOVE FN TO K.
           DISPLAY "M1".
           MOVE FI TO N.
           DISPLAY "M2".
           IF FI > 0
              DISPLAY "REL-TRUE"
           END-IF.
           DISPLAY "M3".
           COMPUTE N = FN + 1.
           DISPLAY "DONE".
           STOP RUN.
