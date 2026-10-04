      *> reject-at: 2002 2014 2023
      *> ISO §8.4.3.9.3 SR4: "If the object property is used as a receiving
      *> item, a set property method shall exist for property-name-1 in the
      *> object referenced by identifier-1".  R is PROPERTY WITH NO SET, so
      *> as the receiver of a Format 5 SET it has no set property method.
      *> Since kb/Work PB1275 a SET statement classifies its receivers (the
      *> store taxonomy is total), so the refusal names SR4 - it was the
      *> blanket "outside the classified store taxonomy" for every SET.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1275N1.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1275NC
           PROPERTY R.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A USAGE OBJECT REFERENCE PB1275NC.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB1275NC "NEW" RETURNING A.
           SET R OF A TO A.
           STOP RUN.
       END PROGRAM PB1275N1.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1275NC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R USAGE OBJECT REFERENCE PROPERTY WITH NO SET.
       END OBJECT.
       END CLASS PB1275NC.
