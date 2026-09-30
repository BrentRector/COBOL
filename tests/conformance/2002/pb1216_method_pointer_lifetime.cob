      *> kb/Work PB1216 - pointer lifetime for a METHOD: a method is a
      *> runtime element, so its LOCAL-STORAGE ends with the activation
      *> (§8.6.4, §8.6.5, §13.18.5.4 4) as in pb1216_ls_and_cancel_
      *> pointer_lifetime) while the OBJECT's WORKING-STORAGE (instance
      *> data; a method has none, §13.5.3 SR1) persists with the object
      *> (§8.6.4 static item; §11.7).
      *> DERIVATION. METHOD-LS: HANDLED, [....] (W4 unchanged after the
      *> resumed MOVE). METHOD-WS [MWWW]: the instance item is alive.
       >>TURN EC-BOUND-PTR CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1216C.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1216K.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE PB1216K.
       01 P USAGE POINTER.
       01 W4 PIC X(4) VALUE "....".
       01 B PIC X(4) BASED.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-BP SECTION.
           USE AFTER EXCEPTION CONDITION EC-BOUND-PTR.
       D-BP-P.
           DISPLAY "HANDLED " FUNCTION EXCEPTION-STATUS
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       M-P.
           INVOKE PB1216K "NEW" RETURNING O
           INVOKE O "LSADDR" RETURNING P
           SET ADDRESS OF B TO P
           MOVE "...." TO W4
           MOVE B TO W4
           DISPLAY "METHOD-LS [" W4 "]"
           INVOKE O "WSADDR" RETURNING P
           SET ADDRESS OF B TO P
           MOVE "...." TO W4
           MOVE B TO W4
           DISPLAY "METHOD-WS [" W4 "]"
           STOP RUN.
       END PROGRAM PB1216C.
       IDENTIFICATION DIVISION.
       CLASS-ID. PB1216K INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WW PIC X(4) VALUE "MWWW".
       PROCEDURE DIVISION.
       METHOD-ID. LSADDR.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 LL PIC X(4) VALUE "MLLL".
       LINKAGE SECTION.
       01 LR USAGE POINTER.
       PROCEDURE DIVISION RETURNING LR.
           SET LR TO ADDRESS OF LL.
           GOBACK.
       END METHOD LSADDR.
       METHOD-ID. WSADDR.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LR USAGE POINTER.
       PROCEDURE DIVISION RETURNING LR.
           SET LR TO ADDRESS OF WW.
           GOBACK.
       END METHOD WSADDR.
       END OBJECT.
       END CLASS PB1216K.
