      *>--- conformance-case
      *> id: pb3102_set_entry_not_found_declared
      *> rules: []
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3102 (review finding R1-1) - SET program-pointer TO
      *> ENTRY that cannot locate its program DECLARES the fatal
      *> EC-PROGRAM-NOT-FOUND, so >>TURN ... CHECKING ON reaches it and
      *> the declarative runs. The ENTRY sender is the extension
      *> docs/CONFORMANCE.md 3.2 registers as
      *> set-program-pointer-to-entry; it binds to the same
      *> BoundSetEntry node as the ISO sender ADDRESS OF PROGRAM (golden
      *> pb1549_set_program_address_fatal_resume) and takes the same GR4
      *> semantics. This golden pins the ENTRY forms,
      *> literal and identifier: the per-name raise-site registry
      *> (EcRaiseSiteRegistryDriftTests) cannot see the BoundSetEntry
      *> row's loss, because three other sources declare the name.
      *>
      *> THE RULES.
      *> §8.4.3.13.4 GR4: "If the runtime system cannot locate the
      *>   program, the EC-PROGRAM-NOT-FOUND exception condition is set
      *>   to exist and the value of the address-identifier is the
      *>   predefined address NULL."
      *>   OK  §8.4.3.13.4 4)  (General rules)
      *> §14.6.13.1.1: "If checking for an exception condition is
      *>   enabled and an exception status indicator is set as a result
      *>   of an exception detected during the execution of a statement,
      *>   the associated exception condition is raised".
      *>   OK  §14.6.13.1.1  (General)
      *> §14.6.13.1.3 5): checking enabled and an applicable USE
      *>   statement: the declarative is executed; "If execution of the
      *>   declarative completes normally the execution of the run unit
      *>   is terminated abnormally" - so the declarative RESUMEs.
      *>   OK  §14.6.13.1.3 5)  (Fatal exception conditions)
      *> §14.9.33.4 GR2 a): NEXT STATEMENT's implicit CONTINUE
      *>   "immediately follows the end of the statement that was
      *>   executing".
      *>   OK  §14.9.33.4 2) a)  (General rules)
      *>
      *> DERIVATION.
      *> FOUND-SET -- PB3102SB is an outermost program of this run unit,
      *>   so ENTRY locates it; no condition is raised.
      *> NF-HANDLED -- PB3102SZ is not in the run unit: GR4 sets
      *>   EC-PROGRAM-NOT-FOUND, checking is enabled, the declarative
      *>   runs and RESUMEs at the statement after the SET ...
      *> LIT-NULL -- ... where PP is NULL (GR4).
      *> EC1=EC-PROGRAM-NOT-FOUND -- the last exception status.
      *> NF-HANDLED -- PP is set to PB3102SB again (found), then the
      *>   identifier leg: WS-NAME holds "PB3102SZ", which GR1 a) names
      *>   and GR4 cannot locate, so the declarative runs again ...
      *> ID-NULL -- ... and PP, which held PB3102SB, is NULL.
      *> EC2=EC-PROGRAM-NOT-FOUND -- the identifier leg's condition.
       >>TURN EC-PROGRAM-NOT-FOUND CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3102SE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PP USAGE PROGRAM-POINTER.
       01 WS-NAME PIC X(12) VALUE "PB3102SZ".
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-NF SECTION.
           USE AFTER EXCEPTION CONDITION EC-PROGRAM-NOT-FOUND.
       D-NF-P.
           DISPLAY "NF-HANDLED"
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       M-P.
           SET PP TO ENTRY "PB3102SB"
           IF PP NOT = NULL
               DISPLAY "FOUND-SET"
           END-IF
           SET PP TO ENTRY "PB3102SZ"
           IF PP = NULL
               DISPLAY "LIT-NULL"
           END-IF
           DISPLAY "EC1=" FUNCTION EXCEPTION-STATUS
           SET PP TO ENTRY "PB3102SB"
           SET PP TO ENTRY WS-NAME
           IF PP = NULL
               DISPLAY "ID-NULL"
           END-IF
           DISPLAY "EC2=" FUNCTION EXCEPTION-STATUS
           STOP RUN.
       END PROGRAM PB3102SE.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3102SB.
       PROCEDURE DIVISION.
       MAIN.
           GOBACK.
       END PROGRAM PB3102SB.
