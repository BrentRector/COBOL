      *> reject-at: 2002 2014 2023
      *> kb/Work PB1450 - ISO/IEC 1989:2023 section 8.4.3.9.3 SR7: "The data description of the item specified in the
      *> RETURNING phrase of the get property method shall be the same as the data description of the item specified
      *> as the USING parameter of the set property method."
      *>   cite.py: OK  8.4.3.9.3 7)  (Syntax rules)
      *> The pair is read as the class sees it: the GET is inherited from the superclass (PIC 9(5)) and the SET is
      *> written in the subclass (PIC X(3)), so the subclass's property has two descriptions.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1450ID.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1450IS.
           PROPERTY BAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A USAGE OBJECT REFERENCE PB1450IS.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB1450IS "NEW" RETURNING A.
           MOVE 5 TO BAL OF A.
           STOP RUN.
       END PROGRAM PB1450ID.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1450IB INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-BAL PIC 9(5).
       PROCEDURE DIVISION.
       METHOD-ID. GET PROPERTY BAL.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-R PIC 9(5).
       PROCEDURE DIVISION RETURNING LK-R.
       MAIN.
           MOVE W-BAL TO LK-R.
       END METHOD.
       END OBJECT.
       END CLASS PB1450IB.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1450IS INHERITS FROM PB1450IB.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1450IB.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. SET PROPERTY BAL.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-V PIC X(3).
       PROCEDURE DIVISION USING LK-V.
       MAIN.
           DISPLAY "SET:" LK-V.
       END METHOD.
       END OBJECT.
       END CLASS PB1450IS.
