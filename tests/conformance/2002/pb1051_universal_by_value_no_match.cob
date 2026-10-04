      *> kb/Work PB1051 -- a BY VALUE method formal and a UNIVERSAL object reference.
      *> ISO 14.9.23.3 SR6 (cite.py --check 14.9.23.3 "If identifier-1 references a universal object reference,
      *> neither the BY CONTENT nor the BY VALUE phrase shall be specified and the BY REFERENCE phrase, if not
      *> specified explicitly, is assumed implicitly" -> OK 14.9.23.3 6)): every argument of a universal
      *> invocation is BY REFERENCE. 9.3.6 match rule 3 a) (cite.py --check 9.3.6 "For each parameter of the
      *> invocation that is passed by reference there shall be a corresponding parameter in the invoked method"
      *> -> OK 9.3.6 3)): that parameter must be specified with the BY REFERENCE phrase, so a method whose formal
      *> is BY VALUE does not MATCH, the search ends at resolution step 6) and EC-OO-METHOD is set to exist
      *> (14.9.23.4 GR7 b)). The declarative selects the non-matching INVOKE and RESUME AT NEXT STATEMENT
      *> continues; the BY REFERENCE method of the same class binds and runs.
      >>TURN EC-OO-METHOD CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1051U.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CBU.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U USAGE OBJECT REFERENCE.
       01 N PIC S9(4) COMP-5 VALUE 42.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-OO-METHOD.
       H-P.
           DISPLAY "NO-MATCH".
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           INVOKE CBU "NEW" RETURNING U.
           INVOKE U "BYVAL" USING N.
           DISPLAY "AFTER-BYVAL".
           INVOKE U "BYREF" USING N.
           DISPLAY "AFTER-BYREF".
           STOP RUN.
       END PROGRAM PB1051U.

       IDENTIFICATION DIVISION.
       CLASS-ID. CBU INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. BYVAL.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LN PIC S9(4) COMP-5.
       PROCEDURE DIVISION USING BY VALUE LN.
           DISPLAY "BYVAL RAN".
       END METHOD BYVAL.
       METHOD-ID. BYREF.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LN PIC S9(4) COMP-5.
       PROCEDURE DIVISION USING BY REFERENCE LN.
           DISPLAY "BYREF RAN".
       END METHOD BYREF.
       END OBJECT.
       END CLASS CBU.
