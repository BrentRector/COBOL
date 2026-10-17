*>--- conformance-case
*> id: pb892-performed-declarative-resume-below-2002
*> rules: [GR-14.9.33.4-2]
*> source-format: free
*> portable: unverified
*>---
      *> The REJECT half of conformance:2002/pb892_performed_declarative_resume
      *> (kb/Work PB892). RESUME (ISO 14.9.33), USE AFTER EXCEPTION
      *> CONDITION (14.9.49) and the >>TURN directive (7.3.25) are all
      *> ISO/IEC 1989:2002 introductions, so at COBOL-85 this source does
      *> not describe a program: the compiler refuses the first construct
      *> the edition does not have with COBOLNET0900.
       >>TURN EC-USER-RZ CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB892PRN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       HZ SECTION. USE AFTER EXCEPTION CONDITION EC-USER-RZ.
       HZ-P.
           ADD 1 TO K.
           DISPLAY "DECL " K.
           IF K = 4
               RESUME AT TAIL-P
           END-IF.
           RESUME AT NEXT STATEMENT.
           DISPLAY "NEVER".
       HZ-Q.
           DISPLAY "HZ-Q " K.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           PERFORM HZ-P 2 TIMES.
           DISPLAY "AFTER-TIMES " K.
           PERFORM HZ.
           DISPLAY "AFTER-SECTION " K.
           PERFORM HZ-P.
           DISPLAY "NOT-REACHED".
           STOP RUN.
       TAIL-P.
           DISPLAY "TAIL " K.
           RAISE EXCEPTION EC-USER-RZ.
           DISPLAY "AFTER-RAISE " K.
           STOP RUN.
