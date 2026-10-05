      *> reject-at: 2002 2014 2023
      *> kb/Work PB1425 - a subscript written after a non-word property object subscripts nothing. ISO 8.4.2.3.2
      *>   writes subscripts only as "qualified-data-name-1 [ ( subscript ... ) ]", and 8.4.2.3.3 SR2: "If a
      *>   subscript is specified, the data description entry describing qualified-data-name-1 ... shall contain an
      *>   OCCURS clause or shall be subordinate to a data description entry that contains an OCCURS clause."
      *>   In BAL OF U AS PB1425TSA (2) neither the object-view U AS PB1425TSA (8.4.3.5.2) nor the object property
      *>   (8.4.3.1.2 Format 7; its value is the get method's RETURNING item, 8.4.3.9.3 SR5) is a qualified data
      *>   name of a table. The (2) used to be dropped silently and the program displayed BAL.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1425TSP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1425TSA
           PROPERTY BAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U USAGE OBJECT REFERENCE.
       PROCEDURE DIVISION.
           DISPLAY BAL OF U AS PB1425TSA (2)
           STOP RUN.
       END PROGRAM PB1425TSP.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1425TSA INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BAL PIC 9(5) VALUE 100 PROPERTY.
       PROCEDURE DIVISION.
       END OBJECT.
       END CLASS PB1425TSA.
