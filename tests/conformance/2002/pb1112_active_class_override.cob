      *> kb/Work PB1112 (override leg) -- an OVERRIDE of a method with an
      *> ACTIVE-CLASS formal, written in the subclass with the identical
      *> ACTIVE-CLASS formal.
      *> ISO 11.7.3 SR3 (cite.py --check 11.7.3 "If the OVERRIDE phrase is
      *> specified, there shall be a method with the same method
      *> resolution signature" -> OK 11.7.3 3)) holds the override to
      *> 9.3.8.2.3, whose rule 2 d) (cite.py --check 9.3.8.2.3 "If the
      *> parameter in interface-2 is described with the ACTIVE-CLASS
      *> phrase, the corresponding parameter in interface-1 is described
      *> with the ACTIVE-CLASS phrase" -> OK 9.3.8.2.3 2) d)) asks only
      *> for ACTIVE-CLASS on both sides with the same FACTORY presence:
      *> the CONTAINING class (AO2A here, BO2A there) is no part of the
      *> description (13.18.60.4 GR22 e) binds it at activation).
      *> B is described BO2A ONLY and the method is invoked with B
      *> (14.8.2.3.2 rule 4 b)): BO2A's override runs, so B-M; A is the
      *> same shape over AO2A, so A-M.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1112O.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS AO2A
           CLASS BO2A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B USAGE OBJECT REFERENCE BO2A ONLY.
       01 A USAGE OBJECT REFERENCE AO2A ONLY.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE BO2A "NEW" RETURNING B.
           INVOKE B "M" USING B.
           INVOKE AO2A "NEW" RETURNING A.
           INVOKE A "M" USING A.
           STOP RUN.
       END PROGRAM PB1112O.

       IDENTIFICATION DIVISION.
       CLASS-ID. AO2A INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. M.
       DATA DIVISION.
       LINKAGE SECTION.
       01 X USAGE OBJECT REFERENCE ACTIVE-CLASS.
       PROCEDURE DIVISION USING X.
           DISPLAY "A-M".
       END METHOD M.
       END OBJECT.
       END CLASS AO2A.

       IDENTIFICATION DIVISION.
       CLASS-ID. BO2A INHERITS FROM AO2A.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS AO2A.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. M OVERRIDE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 X USAGE OBJECT REFERENCE ACTIVE-CLASS.
       PROCEDURE DIVISION USING X.
           DISPLAY "B-M".
       END METHOD M.
       END OBJECT.
       END CLASS BO2A.
