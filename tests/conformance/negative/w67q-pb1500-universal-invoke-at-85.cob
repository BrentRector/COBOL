      *> reject-at: 85
      *> kb/Work PB1405 + PB1500 + PB1064 - the edition floor of the
      *> positive goldens 2002/w67q_pb1405_universal_externalized_name,
      *> 2002/w67q_pb1500_universal_no_match and
      *> 2002/w67q_pb1064_invoke_character_channel: an INVOKE (§14.9.23)
      *> through a universal object reference, resolved by the §9.3.6
      *> method-invocation rules against a METHOD-ID's AS name
      *> (§8.3.2.2), with its BY REFERENCE argument sharing the formal's
      *> storage (§14.2.3 GR8), is object orientation - an ISO/IEC
      *> 1989:2002 introduction absent from COBOL-85, so the 85 compiler
      *> refuses the class reference and the INVOKE (COBOLNET0900).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W67QN85.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS W67QN85C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U  USAGE OBJECT REFERENCE.
       01 NM PIC X(12) VALUE "MixedExt".
       PROCEDURE DIVISION.
       MAIN-P.
           INVOKE W67QN85C "NEW" RETURNING U.
           INVOKE U NM.
           STOP RUN.
       END PROGRAM W67QN85.

       IDENTIFICATION DIVISION.
       CLASS-ID. W67QN85C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. M2 AS "MixedExt".
       PROCEDURE DIVISION.
           DISPLAY "M2 RAN".
       END METHOD M2.
       END OBJECT.
       END CLASS W67QN85C.
