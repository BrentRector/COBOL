      *>--- conformance-case
      *> id: l1_exception_no_location_arms
      *> rules: [GR-7.3.25.4-7, RV-15.30.3-1, RV-15.31.3-1,
      *>    RV-15.32.3-1, DOC-A.1-139, DOC-A.1-204]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §15.30.3 r1 / §15.31.3 r1 / §15.32.3 r1 - the NO-LOCATION
      *> arm, on BOTH of the enabling mechanisms the antecedent names:
      *> "the LOCATION option of the TURN directive or PERFORM statement
      *> that enabled checking for the exception condition associated
      *> with the last exception status is not specified and the
      *> implementor does not save the location information". One
      *> consequent per function:
      *>   §15.30.3 r1  EXCEPTION-LOCATION   -> one alphanumeric space
      *>   §15.31.3 r1  EXCEPTION-LOCATION-N -> one national space char
      *>   §15.32.3 r1  EXCEPTION-STATEMENT  -> 63 spaces
      *> COBOL.NET saves no location information when LOCATION is absent
      *> - the §7.3.25.4 GR7 determination ("If the LOCATION phrase is
      *> not specified, the implementor shall specify whether this
      *> information is made available or not"; Annex A.1 item 204) - so
      *> the antecedent holds on both legs and r1 governs both.
      *>
      *> LEG A - the TURN-directive leg. The >>TURN below carries no
      *> WITH LOCATION. §14.9.43.4 8) b): STRING sets EC-OVERFLOW-STRING
      *> when the pointer passes the receiver - 7 characters DELIMITED
      *> BY SIZE into a PIC X(3). Table 13 makes it nonfatal and
      *> §14.6.13.1.4 1) hands control to ON OVERFLOW, so the run
      *> continues.
      *>
      *> LEG B - the PERFORM leg. No >>TURN names EC-USER-NOLOC, so
      *> §14.9.28.4 GR14 supplies the implicit TURN directive before
      *> imperative-statement-1, and "If LOCATION is specified, that
      *> implicit TURN directive contains LOCATION" - which this PERFORM
      *> does not specify. §14.6.13.1.1: "All user-defined exception
      *> conditions shall be nonfatal", so the WHEN phrase completes and
      *> execution continues at the end of the PERFORM (§14.9.28.4
      *> GR20). §14.6.13.1.1 also makes the last exception status a
      *> RUN-UNIT entity, so it is still readable after END-PERFORM.
      *>
      *> A-S / B-S are §15.33.3 r1's 31-character exception-name - the
      *> leg markers, and the proof that a condition really was raised.
      *> A-LL / A-NL / B-LL / B-NL are FUNCTION LENGTH of the returned
      *> value (§15.50.4 r3 counts alphanumeric character positions, r2
      *> counts national character positions), so 1 is exactly what "one
      *> ... space character" asserts. A-N / B-N are DISPLAY-OF of the
      *> national value and show the character itself is a space.
       >>TURN EC-OVERFLOW-STRING CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1ECNL01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-DST PIC X(3).
       PROCEDURE DIVISION.
       MAIN-P.
           STRING "ABCDEFG" DELIMITED BY SIZE INTO WS-DST
               ON OVERFLOW CONTINUE
           END-STRING.
           DISPLAY "A-S=[" FUNCTION EXCEPTION-STATUS "]".
           DISPLAY "A-L=[" FUNCTION EXCEPTION-LOCATION "]".
           DISPLAY "A-LL="
               FUNCTION LENGTH(FUNCTION EXCEPTION-LOCATION).
           DISPLAY "A-N=["
               FUNCTION DISPLAY-OF(FUNCTION EXCEPTION-LOCATION-N) "]".
           DISPLAY "A-NL="
               FUNCTION LENGTH(FUNCTION EXCEPTION-LOCATION-N).
           DISPLAY "A-T=[" FUNCTION EXCEPTION-STATEMENT "]".
           PERFORM
               RAISE EXCEPTION EC-USER-NOLOC
           WHEN EC-USER-NOLOC
               CONTINUE
           END-PERFORM.
           DISPLAY "B-S=[" FUNCTION EXCEPTION-STATUS "]".
           DISPLAY "B-L=[" FUNCTION EXCEPTION-LOCATION "]".
           DISPLAY "B-LL="
               FUNCTION LENGTH(FUNCTION EXCEPTION-LOCATION).
           DISPLAY "B-N=["
               FUNCTION DISPLAY-OF(FUNCTION EXCEPTION-LOCATION-N) "]".
           DISPLAY "B-NL="
               FUNCTION LENGTH(FUNCTION EXCEPTION-LOCATION-N).
           DISPLAY "B-T=[" FUNCTION EXCEPTION-STATEMENT "]".
           STOP RUN.
