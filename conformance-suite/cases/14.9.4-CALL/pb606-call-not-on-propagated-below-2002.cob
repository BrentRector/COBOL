*>--- conformance-case
*> id: pb606-call-not-on-propagated-below-2002
*> rules: [GR-14.9.4.4-3]
*> source-format: free
*> portable: unverified
*>---
      *> The REJECT half of conformance:cases/14.9.4-CALL/pb606_call_not_on_propagated
      *> (kb/Work PB606). ISO 14.9.4.4 GR3 i)'s "if an exception condition
      *> is propagated from the called program" needs the GOBACK RAISING
      *> phrase (14.9.18) and USE AFTER EXCEPTION CONDITION - ISO/IEC
      *> 1989:2002 introductions. At COBOL-85 this source does not describe
      *> a program, so the compiler refuses the first construct the edition
      *> does not have with COBOLNET0900 rather than running NOT ON
      *> EXCEPTION over a dropped condition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB606N85.
       PROCEDURE DIVISION.
       MAIN-P.
           CALL "PB606N85S"
               NOT ON EXCEPTION DISPLAY "NOT-ON"
           END-CALL.
           STOP RUN.
       END PROGRAM PB606N85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB606N85S.
       PROCEDURE DIVISION RAISING EC-USER-PB6N.
       S-P.
           GOBACK RAISING EXCEPTION EC-USER-PB6N.
       END PROGRAM PB606N85S.
