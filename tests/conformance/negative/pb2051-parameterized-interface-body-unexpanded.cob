      *> reject-at: 2002 2014 2023
      *> kb/Work PB2051 - 13.16.3 SR16 (cite.py OK): "The BASED clause may be specified only in data
      *> description entries in the linkage section, in the working-storage section, and in the local-storage
      *> section. The level number of such data description entries shall be 1 or 77." The interface twin of
      *> pb2051-parameterized-class-body-unexpanded: B is a level-05 entry of a method prototype's linkage
      *> section, in a PARAMETERIZED interface that nothing expands (9.3.13), so only the definition itself can
      *> answer for it.
       IDENTIFICATION DIVISION.
       INTERFACE-ID. PB2051ISK USING T.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS T.
       PROCEDURE DIVISION.
       METHOD-ID. PUT.
       DATA DIVISION.
       LINKAGE SECTION.
       01 X USAGE OBJECT REFERENCE T.
       01 G.
          05 B BASED PIC X.
       PROCEDURE DIVISION USING X G.
       END METHOD PUT.
       END INTERFACE PB2051ISK.
