      *> kb/Work PB2051 -- a PARAMETERIZED class or interface definition binds AS ITSELF, so every rule of its
      *> own data division, environment division and method headers is asked of it whether or not anything
      *> expands it.  Its parameter-names stand only where an object-class-name or interface-name is permitted
      *> (ISO 11.3.4 GR6 / 11.6.4 GR4, cite.py OK), and this program writes them in each such position of a
      *> data description and a method header: USAGE OBJECT REFERENCE formal, FACTORY OF formal, formal ONLY,
      *> an interface formal, the RAISING phrase.  All of it is legal, so binding the definitions as themselves
      *> must refuse none of it -- PB2051INONE and PB2051PAIR are never expanded, PB2051HOLD is.
      *>
      *> DERIVATION OF THE EXPECTED OUTPUT.  12.3.8.4 GR5 (cite.py OK): "The class object-class-name-1 is created
      *> from the parameterized class object-class-name-2 by replacing each specification of the formal parameter
      *> by the corresponding actual parameter", and 9.3.12: "An expansion of a parameterized class is treated in
      *> all respects the same as if it were a class that is not a parameterized class".  So PB2051HOLD-BOX is
      *> PB2051HOLD with ELEM read as PB2051BOX: PUT stores B, and SHOW-IT invokes PB2051BOX's SHOW, which
      *> displays its V, VALUE 7 -> "BOX 007".  The unexpanded definitions create no class and display nothing.
       IDENTIFICATION DIVISION.
       CLASS-ID. PB2051BOX INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 V PIC 9(3) VALUE 7.
       PROCEDURE DIVISION.
       METHOD-ID. SHOW.
       PROCEDURE DIVISION.
           DISPLAY "BOX " V.
       END METHOD SHOW.
       END OBJECT.
       END CLASS PB2051BOX.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. PB2051INONE USING T.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS T.
       PROCEDURE DIVISION.
       METHOD-ID. PUT.
       DATA DIVISION.
       LINKAGE SECTION.
       01 X USAGE OBJECT REFERENCE T.
       01 F USAGE OBJECT REFERENCE FACTORY OF T ONLY.
       PROCEDURE DIVISION USING X F RAISING T.
       END METHOD PUT.
       END INTERFACE PB2051INONE.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB2051PAIR INHERITS FROM BASE USING A I.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           CLASS A
           INTERFACE I.
       FACTORY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 MAKER USAGE OBJECT REFERENCE FACTORY OF A.
       PROCEDURE DIVISION.
       END FACTORY.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 LEFT-ONE  USAGE OBJECT REFERENCE A ONLY.
       01 RIGHT-ONE USAGE OBJECT REFERENCE I.
       PROCEDURE DIVISION.
       METHOD-ID. SET-PAIR.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L USAGE OBJECT REFERENCE A.
       01 R USAGE OBJECT REFERENCE I.
       PROCEDURE DIVISION USING L R RAISING A I.
           SET LEFT-ONE TO L.
           SET RIGHT-ONE TO R.
       END METHOD SET-PAIR.
       END OBJECT.
       END CLASS PB2051PAIR.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB2051HOLD INHERITS FROM BASE USING ELEM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           CLASS ELEM.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ITEM USAGE OBJECT REFERENCE ELEM.
       PROCEDURE DIVISION.
       METHOD-ID. PUT.
       DATA DIVISION.
       LINKAGE SECTION.
       01 X USAGE OBJECT REFERENCE ELEM.
       PROCEDURE DIVISION USING X.
           SET ITEM TO X.
       END METHOD PUT.
       METHOD-ID. SHOW-IT.
       PROCEDURE DIVISION.
           INVOKE ITEM "SHOW".
       END METHOD SHOW-IT.
       END OBJECT.
       END CLASS PB2051HOLD.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2051M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB2051BOX
           CLASS PB2051HOLD
           CLASS PB2051HOLD-BOX EXPANDS PB2051HOLD USING PB2051BOX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B USAGE OBJECT REFERENCE PB2051BOX.
       01 H USAGE OBJECT REFERENCE PB2051HOLD-BOX.
       PROCEDURE DIVISION.
           INVOKE PB2051BOX "NEW" RETURNING B.
           INVOKE PB2051HOLD-BOX "NEW" RETURNING H.
           INVOKE H "PUT" USING B.
           INVOKE H "SHOW-IT".
           STOP RUN.
       END PROGRAM PB2051M.
