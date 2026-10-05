      *> reject-at: 2002 2014 2023
      *> kb/Work PB1425 - ISO 8.4.3.9.3 SR2: "Identifier-1 shall be an object reference; neither a universal
      *>   object reference nor the predefined object reference NULL shall be specified." The object of the
      *>   object-property identifier BAL OF NULL is NULL, so it is refused by SR2 (COBOLNET2918, the code of
      *>   this rule alone) - never by a parse error: the object of a property is any identifier (8.4.3.1.3
      *>   SR1), and NULL is one (8.4.3.1.2 Format 6). Everything else is valid: BAL is a REPOSITORY property
      *>   (SR1) of class PB1425NC with a GET PROPERTY method (SR3), and BAL OF O compiles.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1425NN.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1425NC
           PROPERTY BAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE PB1425NC.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB1425NC "NEW" RETURNING O
           DISPLAY BAL OF O
           DISPLAY BAL OF NULL
           STOP RUN.
       END PROGRAM PB1425NN.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1425NC INHERITS FROM BASE.
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
       END CLASS PB1425NC.
