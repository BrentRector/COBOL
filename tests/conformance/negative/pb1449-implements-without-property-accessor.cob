      *> reject-at: 2002 2014 2023
      *> kb/Work PB1449 - an interface's GET PROPERTY prototype is a get property method (ISO/IEC 1989:2023 section
      *> 11.7.4 GR6: "If the GET phrase is specified, this method is a get property method for property-name-1."), so a
      *> class that implements the interface shall define one.  Section 9.3.11 / 11.8.3 SR2: the class shall conform to
      *> the interface it implements.
      *>   cite.py: OK  11.7.4 6)
      *> PB1449N's object describes BAL without the PROPERTY clause and defines no GET PROPERTY BAL method.
       IDENTIFICATION DIVISION.
       INTERFACE-ID. PB1449J.
       PROCEDURE DIVISION.
       METHOD-ID. GET PROPERTY BAL.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-R PIC 9(5).
       PROCEDURE DIVISION RETURNING LK-R.
       END METHOD.
       END INTERFACE PB1449J.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1449N INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           INTERFACE PB1449J.
       IDENTIFICATION DIVISION.
       OBJECT. IMPLEMENTS PB1449J.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BAL PIC 9(5) VALUE 100.
       PROCEDURE DIVISION.
       END OBJECT.
       END CLASS PB1449N.
