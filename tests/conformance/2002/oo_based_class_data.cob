      *> kb/Work PB956 -- BASED data and ADDRESS OF targets in every data division a class definition has.
      *> ISO 13.16.3 SR16: the BASED clause "may be specified only in data description entries in the linkage
      *> section, in the working-storage section, and in the local-storage section" -- a factory and an instance
      *> definition have a working-storage section, a method definition only a local-storage and a linkage section
      *> (13.5.3 SR1: working-storage "only in a factory definition or an instance definition, but not in a method
      *> definition", kb/Work PB1308); 13.18.5.3 bars only a class-object subject and a
      *> dynamic-length / variable-length subject. 8.6.4: local-storage items are "allocated and set to initial
      *> state each time the runtime element containing them is activated", so each recursive REC activation
      *> owns its own LV and its own LB address; 13.18.5.4 GR2: every implicit data-address pointer starts NULL.
      *> Expected output, from those rules:
      *>   FACTORY  ALLOCATE FB INITIALIZED (14.9.3.4 GR7 -- as INITIALIZE ... TO DEFAULT: FB2 = zero) + 7  -> F:HI007
      *>   REC(0) -> REC(1) -> REC(2): each LB addresses ITS OWN activation's LV ("L0 ", "L1 ", "L2 "),
      *>     printed innermost first, then re-based onto the object's OW ("OBJ").
      *>   WSM twice: WC, WB and WP are OBJECT data (working-storage of the instance definition), so WP keeps the
      *>     address of WC and the second activation adds to the same WC -> W:1, W:2.
      *>   GET property PV reads the object item whose address M1 took (the record now lives on a cell).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB956BAS.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB956C
           PROPERTY PV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE PB956C.
       01 N PIC 9 VALUE 0.
       01 V PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB956C "FM".
           INVOKE PB956C "NEW" RETURNING O.
           INVOKE O "REC" USING N.
           INVOKE O "WSM".
           INVOKE O "WSM".
           INVOKE O "M1".
           MOVE PV OF O TO V.
           DISPLAY "P:" V.
           MOVE "ZZZZ" TO PV OF O.
           INVOKE O "M1".
           STOP RUN.
       END PROGRAM PB956BAS.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB956C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       FACTORY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  FB BASED.
           05 FB1 PIC X(2).
           05 FB2 PIC 9(3).
       PROCEDURE DIVISION.
       METHOD-ID. FM.
       PROCEDURE DIVISION.
           ALLOCATE FB INITIALIZED.
           MOVE "HI" TO FB1.
           ADD 7 TO FB2.
           DISPLAY "F:" FB1 FB2.
       END METHOD FM.
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  OW PIC X(3) VALUE "OBJ".
       01  PV PIC X(4) VALUE "PROP" PROPERTY.
       01  PB PIC X(4) BASED.
       01  WC PIC 9 VALUE 0.
       01  WB PIC 9 BASED.
       01  WP USAGE POINTER.
       PROCEDURE DIVISION.
       METHOD-ID. REC.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01  LB PIC X(3) BASED.
       01  LV PIC X(3) VALUE "L0 ".
       01  P  USAGE POINTER.
       LINKAGE SECTION.
       01  D  PIC 9.
       PROCEDURE DIVISION USING D.
           MOVE D TO LV(2:1).
           SET P TO ADDRESS OF LV.
           SET ADDRESS OF LB TO P.
           IF D < 2
               ADD 1 TO D
               INVOKE SELF "REC" USING D
               SUBTRACT 1 FROM D
           END-IF.
           DISPLAY "R" D ":" LB.
           SET ADDRESS OF LB TO ADDRESS OF OW.
           DISPLAY "R" D ":" LB.
       END METHOD REC.
       METHOD-ID. WSM.
       PROCEDURE DIVISION.
           IF WP = NULL
               SET WP TO ADDRESS OF WC
           END-IF.
           SET ADDRESS OF WB TO WP.
           ADD 1 TO WB.
           DISPLAY "W:" WC.
       END METHOD WSM.
       METHOD-ID. M1.
       PROCEDURE DIVISION.
           SET ADDRESS OF PB TO ADDRESS OF PV.
           DISPLAY "M1:" PB.
       END METHOD M1.
       END OBJECT.
       END CLASS PB956C.
