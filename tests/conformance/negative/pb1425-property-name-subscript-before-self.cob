      *> reject-at: 2002 2014 2023
      *> kb/Work PB1425 - BAL (2) OF SELF writes a subscript on the property-name, before its object. ISO 8.4.3.1.2
      *>   Format 7 is "property-name-1 OF identifier-3" - property-name-1 takes no subscript - and 8.4.2.3.2 puts
      *>   subscripts after the WHOLE qualified-data-name-1. The word-object spelling BAL (2) OF D already drew
      *>   COBOLNET2776; with SELF (a non-word object) the (2) used to be dropped silently.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1425SBP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1425SBA.
       PROCEDURE DIVISION.
           STOP RUN.
       END PROGRAM PB1425SBP.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1425SBA INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           PROPERTY BAL.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BAL PIC 9(5) VALUE 100 PROPERTY.
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. SHOW.
       PROCEDURE DIVISION.
           DISPLAY BAL (2) OF SELF.
       END METHOD SHOW.
       END OBJECT.
       END CLASS PB1425SBA.
