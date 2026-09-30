      *> PB1689 control - un-reserving END-INVOKE must not break the vendor INVOKE terminator
      *>   after RETURNING identifier (same program as ec_oo_universal_both, terminator added).
      *> ISO §14.9.23.4 GR7c: for an INVOKE through a UNIVERSAL object reference, "the rules for conformance
      *> specified in 14.8.2, Parameters and 14.8.3, Returning items apply. If a violation of these rules is
      *> detected, the EC-OO-UNIVERSAL exception condition is set to exist IF CHECKING FOR IT IS ENABLED IN
      *> BOTH the activated method and the activating runtime element, the method invocation is not successful,
      *> and execution continues as specified in General rule 7g."
      *>
      *> The returning item is PIC 9(6) against the method's PIC 9(4) — a MOVE could deliver it, so §9.3.6
      *> match rule 7 BINDS the method; the same-PICTURE rule of §14.8.3.3 is then the violation that
      *> §14.9.23.4 GR7c detects at run time because the receiver is universal (kb/Work PB1500: an ARGUMENT
      *> whose description differs is not
      *> a violation of a bound method - it resolves no method, §9.3.6 6), EC-OO-METHOD - see
      *> 2002/w67q_pb1500_universal_no_match). Checking is enabled ONCE at the top of the compilation
      *> group, so it covers both the activating program and the method: enabled in BOTH, so the condition IS
      *> set, the declarative selects, and RESUME AT NEXT STATEMENT keeps the run unit alive.
      *>
      *> The paired golden ec_oo_universal_not_both is the same program with checking enabled only in the
      *> ACTIVATOR — there the condition is NOT set and nothing may be attributed to EC-OO-UNIVERSAL.
       >>TURN EC-OO-UNIVERSAL CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. V55BOTH.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CUNIV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE.
       01 C USAGE OBJECT REFERENCE CUNIV.
       01 W PIC 9(6) VALUE 000007.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-OO-UNIVERSAL.
       H-P.
           DISPLAY "HANDLED=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           INVOKE CUNIV "NEW" RETURNING C.
           SET O TO C.
           INVOKE O "TAKE" RETURNING W END-INVOKE.
           DISPLAY "AFTER".
           STOP RUN.
       END PROGRAM V55BOTH.

       IDENTIFICATION DIVISION.
       CLASS-ID. CUNIV INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. TAKE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK PIC 9(4).
       PROCEDURE DIVISION RETURNING LK.
       MAIN-P.
           DISPLAY "IN-TAKE".
       END METHOD TAKE.
       END OBJECT.
       END CLASS CUNIV.
