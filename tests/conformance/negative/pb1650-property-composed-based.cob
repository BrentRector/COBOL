      *> reject-at: 2002 2014 2023
      *> kb/Work PB1650 - ISO 13.16.3 SR21 (cite.py OK): "The PROPERTY clause shall
      *> not be specified in the same data description entry as: a) a BASED
      *> clause". 13.18.57.4 GR1 (cite.py OK): the TYPE clause's effect is "as
      *> though the data description identified by type-name-1 had been coded in
      *> place", so P carries the BASED clause of T beside its PROPERTY clause.
       IDENTIFICATION DIVISION.
       CLASS-ID. PB1650CPB INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T TYPEDEF BASED PIC X(4).
       01 P TYPE T PROPERTY.
       PROCEDURE DIVISION.
       END OBJECT.
       END CLASS PB1650CPB.
