      *> kb/Work PB1449 -- a property reached through an INTERFACE-typed
      *> object reference.  ISO 11.7.2: the METHOD-ID format, with its
      *> {GET|SET} PROPERTY property-name-1 arm, is shared by method
      *> definitions and prototypes; 11.7.4 GR6/GR7: "If the GET phrase is
      *> specified, this method is a get property method for
      *> property-name-1" (SET likewise).  8.4.3.9.3 SR3: "a get property
      *> method shall exist for property-name-1 in the object referenced
      *> by identifier-1" -- here an object whose class implements the
      *> interface, PB1449A through a PROPERTY clause (13.18.42.4 GR1/GR2)
      *> and PB1449B through explicit GET/SET PROPERTY methods.
      *> Expected values (8.4.3.9.4 GR1 sending: the GET runs; GR2
      *> receiving: the SET runs):
      *>   A=00100  BAL OF I, I holding a PB1449A: the implicit GET, VALUE 100
      *>   A=00042  after MOVE 42 TO BAL OF I: the implicit SET stored 42
      *>   B=00007  BAL OF I, I holding a PB1449B: the explicit GET, W-BAL+1
      *>            with W-BAL VALUE 6
      *>   B=00021  after MOVE 10 TO BAL OF I: the explicit SET stores
      *>            2*10 = 20, and the explicit GET returns 20+1
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1449P.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1449A
           CLASS PB1449B
           INTERFACE PB1449I
           PROPERTY BAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A USAGE OBJECT REFERENCE PB1449A.
       01 B USAGE OBJECT REFERENCE PB1449B.
       01 I USAGE OBJECT REFERENCE PB1449I.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB1449A "NEW" RETURNING A.
           INVOKE PB1449B "NEW" RETURNING B.
           SET I TO A.
           DISPLAY "A=" BAL OF I.
           MOVE 42 TO BAL OF I.
           DISPLAY "A=" BAL OF I.
           SET I TO B.
           DISPLAY "B=" BAL OF I.
           MOVE 10 TO BAL OF I.
           DISPLAY "B=" BAL OF I.
           STOP RUN.
       END PROGRAM PB1449P.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. PB1449I.
       PROCEDURE DIVISION.
       METHOD-ID. GET PROPERTY BAL.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-R PIC 9(5).
       PROCEDURE DIVISION RETURNING LK-R.
       END METHOD.
       METHOD-ID. SET PROPERTY BAL.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-V PIC 9(5).
       PROCEDURE DIVISION USING LK-V.
       END METHOD.
       END INTERFACE PB1449I.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1449A INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           INTERFACE PB1449I.
       IDENTIFICATION DIVISION.
       OBJECT. IMPLEMENTS PB1449I.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BAL PIC 9(5) VALUE 100 PROPERTY.
       PROCEDURE DIVISION.
       END OBJECT.
       END CLASS PB1449A.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1449B INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           INTERFACE PB1449I.
       IDENTIFICATION DIVISION.
       OBJECT. IMPLEMENTS PB1449I.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-BAL PIC 9(5) VALUE 6.
       PROCEDURE DIVISION.
       METHOD-ID. GET PROPERTY BAL.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-R PIC 9(5).
       PROCEDURE DIVISION RETURNING LK-R.
       MAIN.
           ADD 1 TO W-BAL GIVING LK-R.
       END METHOD.
       METHOD-ID. SET PROPERTY BAL.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-V PIC 9(5).
       PROCEDURE DIVISION USING LK-V.
       MAIN.
           MULTIPLY 2 BY LK-V GIVING W-BAL.
       END METHOD.
       END OBJECT.
       END CLASS PB1449B.
