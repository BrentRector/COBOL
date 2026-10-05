      *> kb/Work PB1425 - identifier Format 7 whose identifier-3 is a function-identifier with arguments, followed
      *> by a reference modifier: NM OF FUNCTION F (5) (2:3).
      *>
      *> THE RULE. ISO/IEC 1989:2023 8.4.3.1.4 GR1 applies d) "OF for object properties applies the property-name
      *> on the left to the identifier on the right" before g) "a reference modifier applies to the identifier on
      *> the left" - the reference modifier is applied LAST, to the whole identifier NM OF FUNCTION F (5), whose
      *> value is the get method's PIC X(6) RETURNING item (8.4.3.9.3 SR5). It cannot be the function's: F returns
      *> an object reference, which 8.4.3.3.3 SR1 never lets a reference modifier reference.
      *>
      *> EXPECTED OUTPUT (each value derived from those rules):
      *>   REFMOD BCD     characters 2 through 4 of NM's initial value ABCDEF.
      *>   WHOLE ABCDEF   the same reference without the modifier.
      *>   NUM 00005      BAL OF FUNCTION F (5): F set BAL to its argument.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB1425RFF.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1425RFA
           PROPERTY BAL.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-SEED PIC 9.
       01 L-OBJ USAGE OBJECT REFERENCE PB1425RFA.
       PROCEDURE DIVISION USING L-SEED RETURNING L-OBJ.
           INVOKE PB1425RFA "NEW" RETURNING L-OBJ
           MOVE L-SEED TO BAL OF L-OBJ
           GOBACK.
       END FUNCTION PB1425RFF.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1425RFP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1425RFA
           FUNCTION PB1425RFF
           PROPERTY BAL
           PROPERTY NM.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "REFMOD " NM OF FUNCTION PB1425RFF (5) (2:3)
           DISPLAY "WHOLE " NM OF FUNCTION PB1425RFF (5)
           DISPLAY "NUM " BAL OF FUNCTION PB1425RFF (5)
           STOP RUN.
       END PROGRAM PB1425RFP.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1425RFA INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BAL PIC 9(5) VALUE 100 PROPERTY.
       01 NM PIC X(6) VALUE "ABCDEF" PROPERTY.
       PROCEDURE DIVISION.
       END OBJECT.
       END CLASS PB1425RFA.
