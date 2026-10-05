      *> reject-at: 2002 2014 2023
      *> kb/Work PB1425 - ISO 8.4.3.5.3 SR2: "An object-view shall not be specified as a receiving operand."
      *>   The receiver of SET Format 5 and the RETURNING identifier of INVOKE are receiving operands, so
      *>   both object-views are refused by SR2 (COBOLNET2871), never by the reserved-word diagnostic AS
      *>   used to draw.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1425W2.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1425W2C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U USAGE OBJECT REFERENCE.
       01 D USAGE OBJECT REFERENCE PB1425W2C.
       PROCEDURE DIVISION.
           INVOKE PB1425W2C "NEW" RETURNING U AS PB1425W2C.
           SET U AS PB1425W2C TO D.
           STOP RUN.
       END PROGRAM PB1425W2.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1425W2C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       END CLASS PB1425W2C.
