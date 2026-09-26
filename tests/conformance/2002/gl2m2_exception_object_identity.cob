      *> ISO §8.4.3.6.4 GR1 + GR2 — EXCEPTION-OBJECT is the CURRENT
      *>   exception object (null when none), ONE instance per run unit
      *> GR1: "EXCEPTION-OBJECT references the current exception object.
      *>   If an exception object is not associated with the current
      *>   exception, EXCEPTION-OBJECT is set to null."
      *>   cite.py --check 8.4.3.6.4 "EXCEPTION-OBJECT references the
      *>     current exception object. ..." -> OK  §8.4.3.6.4 1)
      *> GR2: "There is one instance of EXCEPTION-OBJECT in a run unit."
      *>   cite.py --check 8.4.3.6.4 "There is one instance of
      *>     EXCEPTION-OBJECT in a run unit." -> OK  §8.4.3.6.4 2)
      *> What sets it (each -> OK under cite.py --check):
      *>   §14.9.29.4 2) "If identifier-1 is specified, EXCEPTION-OBJECT
      *>     is set to reference the object referenced by identifier-1."
      *>   §14.6.13.1.5 1) "The predefined object reference
      *>     EXCEPTION-OBJECT is set to the content of the object
      *>     reference specified in the RAISE statement ..." and (same
      *>     clause) "If execution of the declarative completes normally,
      *>     execution continues with the statement following the RAISE
      *>     statement."
      *>   §14.9.49.4 14) a) the USE AFTER EXCEPTION OBJECT L1G2XE
      *>     declarative is executed for an instance of L1G2XE; 15)
      *>     "Upon entry to the associated declarative, the predefined
      *>     object reference EXCEPTION-OBJECT references the exception
      *>     object."
      *>   §14.9.29.4 1) "If exception-name-1 is specified, the
      *>     associated exception condition is raised, and
      *>     EXCEPTION-OBJECT is set to null." EC-USER-L1G2 is a
      *>     level-3 user name (§14.6.13.1.1 "The level-3
      *>     exception-names for user-defined exceptions shall start
      *>     with the characters 'EC-USER-'"), nonfatal (§14.6.13.1.6
      *>     Table 13: EC-USER-suffix NF); with no applicable handler
      *>     "the RAISE statement acts as a CONTINUE statement"
      *>     (§14.9.29.4 1) NOTE).
      *>   §14.9.39.4 28) SET LAST EXCEPTION TO OFF: "The predefined
      *>     object reference EXCEPTION-OBJECT is set to null, and the
      *>     last exception status is set to indicate no exception."
      *>   §14.6.13.1.1 "a last exception status exists for the entire
      *>     run unit" - nothing else in this program changes it.
      *>   §8.8.4.2.15 "The relation 'identifier-3 = identifier-4' has a
      *>     true value if the object referenced by identifier-3 is the
      *>     same object that is referenced by identifier-4" - so every
      *>     "=" below tests IDENTITY, not merely non-null.
      *> Legal source: L1G2XE INHERITS FROM BASE (REPOSITORY CLASS BASE,
      *>   §11.3.3 SR2), so "NEW" is in its factory interface (§16.1,
      *>   §14.9.23.3 SR3). E goes to the subprogram BY CONTENT because
      *>   §14.9.4.3 SR10 (a Format 1 rule) forbids BY REFERENCE for a
      *>   class-object argument. The formal LS-E is BY REFERENCE by
      *>   default: §14.2.3 4) "If neither the BY REFERENCE nor the BY
      *>   VALUE phrase is specified prior to the first parameter, the
      *>   BY REFERENCE phrase is assumed." -> OK. The BY CONTENT
      *>   object-reference argument conforms by §14.8.2.3.3: "the
      *>   conformance rules shall be the same as if a SET statement
      *>   were performed ... with the argument as the sending operand
      *>   and the corresponding formal parameter as the receiving
      *>   operand." -> OK  §14.8.2.3.3 2).
      *> DERIVATION of every .out line (E and F are two DISTINCT
      *>   L1G2XE instances):
      *>   START-NULL   nothing raised yet: no exception object is
      *>                associated with the current exception (GR1).
      *>   DECL-E       RAISE E: inside the declarative EXCEPTION-OBJECT
      *>                references E's object (14.9.29.4 2), 14.9.49.4
      *>                15)).
      *>   AFTER-E      back after the RAISE: the current exception is
      *>                still the raised object E (GR1).
      *>   SUB-E        a separately compiled program of the same run
      *>                unit reads the SAME register (GR2): it references
      *>                E's object, identical to its BY CONTENT copy of
      *>                E. Per-program registers would give SUB-NULL.
      *>   NAMED-NULL   RAISE EXCEPTION EC-USER-L1G2: a named condition
      *>                has no object - null (14.9.29.4 1), GR1).
      *>   SUB-NULL     the one register, now null, seen by the
      *>                subprogram too (GR2).
      *>   DECL-F       RAISE F: the register now references F's object,
      *>                not E's - identity, not "any non-null".
      *>   AFTER-F      back after the RAISE: still F (GR1).
      *>   OFF-NULL     SET LAST EXCEPTION TO OFF: null (14.9.39.4 28)).
      *> 2002 dir: the OO / exception-object facility is a COBOL-2002
      *>   introduction; the rule is the same at 2002/2014/2023.
       >>TURN EC-USER-L1G2 CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1G2M04.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS L1G2XE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 E USAGE OBJECT REFERENCE L1G2XE.
       01 F USAGE OBJECT REFERENCE L1G2XE.
       PROCEDURE DIVISION.
       DECLARATIVES.
       EO-SEC SECTION.
           USE AFTER EXCEPTION OBJECT L1G2XE.
       EO-P.
           IF EXCEPTION-OBJECT = E
               DISPLAY "DECL-E"
           ELSE
               IF EXCEPTION-OBJECT = F
                   DISPLAY "DECL-F"
               ELSE
                   DISPLAY "DECL-NEITHER"
               END-IF
           END-IF.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           IF EXCEPTION-OBJECT = NULL
               DISPLAY "START-NULL"
           ELSE
               DISPLAY "START-NOT-NULL"
           END-IF
           INVOKE L1G2XE "NEW" RETURNING E
           INVOKE L1G2XE "NEW" RETURNING F
           RAISE E
           IF EXCEPTION-OBJECT = E
               DISPLAY "AFTER-E"
           ELSE
               DISPLAY "AFTER-NOT-E"
           END-IF
           CALL "L1G2M04S" USING BY CONTENT E
           RAISE EXCEPTION EC-USER-L1G2
           IF EXCEPTION-OBJECT = NULL
               DISPLAY "NAMED-NULL"
           ELSE
               DISPLAY "NAMED-NOT-NULL"
           END-IF
           CALL "L1G2M04S" USING BY CONTENT E
           RAISE F
           IF EXCEPTION-OBJECT = F
               DISPLAY "AFTER-F"
           ELSE
               DISPLAY "AFTER-NOT-F"
           END-IF
           SET LAST EXCEPTION TO OFF
           IF EXCEPTION-OBJECT = NULL
               DISPLAY "OFF-NULL"
           ELSE
               DISPLAY "OFF-NOT-NULL"
           END-IF
           STOP RUN.
       END PROGRAM L1G2M04.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1G2M04S.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS L1G2XE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LS-E USAGE OBJECT REFERENCE L1G2XE.
       PROCEDURE DIVISION USING LS-E.
       SUB-P.
           IF EXCEPTION-OBJECT = NULL
               DISPLAY "SUB-NULL"
           ELSE
               IF EXCEPTION-OBJECT = LS-E
                   DISPLAY "SUB-E"
               ELSE
                   DISPLAY "SUB-OTHER"
               END-IF
           END-IF
           GOBACK.
       END PROGRAM L1G2M04S.

       IDENTIFICATION DIVISION.
       CLASS-ID. L1G2XE INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       END CLASS L1G2XE.
