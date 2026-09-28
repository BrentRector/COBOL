      *> kb/Work PB1405 - ISO §8.3.2.2 1): when an INVOKE statement
      *> references a method-name using a universal object reference and
      *> the COBOL call convention is implied, "that method-name is
      *> treated as a COBOL word that maps to the externalized name of the
      *> method to be invoked" - the name METHOD-ID's AS phrase gives it
      *> (§11.7.4 GR1 b)), and the same name the typed INVOKE resolves.
      *> So M2 AS "MixedExt" is found by "MixedExt" (as a COBOL word, in
      *> either case) through literal-1 and through identifier-2, exactly
      *> as the typed reference T finds it; its declared word "M2" names
      *> no externalized method, so that INVOKE resolves nothing and sets
      *> EC-OO-METHOD (§9.3.6 6)), which the declarative selects.
      >>TURN EC-OO-METHOD CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W67QXN.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS W67QXNC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U  USAGE OBJECT REFERENCE.
       01 T  USAGE OBJECT REFERENCE W67QXNC.
       01 NM PIC X(12).
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-OO-METHOD.
       H-P.
           DISPLAY "HANDLED=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           INVOKE W67QXNC "NEW" RETURNING T.
           SET U TO T.
           INVOKE T "MixedExt".
           INVOKE U "MixedExt".
           INVOKE U "MIXEDEXT".
           MOVE "mixedext" TO NM.
           INVOKE U NM.
           INVOKE U "M1".
           INVOKE U "M2".
           DISPLAY "AFTER-DECLARED-WORD".
           MOVE "M2" TO NM.
           INVOKE U NM.
           DISPLAY "DONE".
           STOP RUN.
       END PROGRAM W67QXN.

       IDENTIFICATION DIVISION.
       CLASS-ID. W67QXNC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. M1.
       PROCEDURE DIVISION.
           DISPLAY "M1 RAN".
       END METHOD M1.
       METHOD-ID. M2 AS "MixedExt".
       PROCEDURE DIVISION.
           DISPLAY "M2 RAN".
       END METHOD M2.
       END OBJECT.
       END CLASS W67QXNC.
