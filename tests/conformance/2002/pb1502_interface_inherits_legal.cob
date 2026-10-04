      *> kb/Work PB1502 -- the LEGAL side of interface multiple inheritance (ISO 11.6.3 SR5 / SR6; 9.3.10).
      *> (1) SR5 (cite.py --check 11.6.3 "If a given method-name is inherited from more than one interface, the method
      *>     prototype in each inherited interface shall be such that this interface conforms to all inherited
      *>     interfaces" -> OK 11.6.3 5)): ICA and ICB declare the IDENTICAL prototype SPEAK, ICC inherits both, so ICC
      *>     conforms to both (a diamond is not a conflict either: ICD inherits ICC and ICA).  A class implementing ICC
      *>     implements the one SPEAK, and INVOKE through an ICC reference reaches it.
      *> (2) SR5 with a COVARIANT pair: IRU's SPEAK returns a universal object reference and IRK's returns a KCL
      *>     reference.  Inheriting both, IRM presents the one that conforms to the other (9.3.8.2.3 rule 5 a): a class
      *>     reference is an object reference), so IRM conforms to both; a class implementing IRM with a KCL-returning
      *>     SPEAK conforms to each.
      *> (3) SR6 (cite.py --check 11.6.3 "A given interface-name shall not appear more than once in an INHERITS
      *>     clause" -> OK 11.6.3 6)) is about the WRITTEN interface-name: ALX and ALY are two REPOSITORY names for the
      *>     one externalized interface "EXT-ALI" (12.3.8.3), so ALB INHERITS FROM ALX ALY writes no name twice and
      *>     inherits ONE interface.  A class implementing ALB answers INVOKE through an ALB reference.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1502L.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS IMPCC
           CLASS IMPRM
           CLASS IMPAB
           INTERFACE ICC
           INTERFACE ICD
           INTERFACE IRM
           INTERFACE ALB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O1 USAGE OBJECT REFERENCE IMPCC.
       01 O2 USAGE OBJECT REFERENCE IMPRM.
       01 O3 USAGE OBJECT REFERENCE IMPAB.
       01 VC USAGE OBJECT REFERENCE ICC.
       01 VD USAGE OBJECT REFERENCE ICD.
       01 VR USAGE OBJECT REFERENCE IRM.
       01 VA USAGE OBJECT REFERENCE ALB.
       01 UR USAGE OBJECT REFERENCE.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE IMPCC "NEW" RETURNING O1.
           SET VC TO O1.
           INVOKE VC "SPEAK".
           SET VD TO O1.
           INVOKE VD "SPEAK".
           INVOKE IMPRM "NEW" RETURNING O2.
           SET VR TO O2.
           INVOKE VR "SPEAK" RETURNING UR.
           INVOKE UR "WHO".
           INVOKE IMPAB "NEW" RETURNING O3.
           SET VA TO O3.
           INVOKE VA "PING".
           STOP RUN.
       END PROGRAM PB1502L.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. ICA.
       PROCEDURE DIVISION.
       METHOD-ID. SPEAK.
       PROCEDURE DIVISION.
       END METHOD SPEAK.
       END INTERFACE ICA.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. ICB.
       PROCEDURE DIVISION.
       METHOD-ID. SPEAK.
       PROCEDURE DIVISION.
       END METHOD SPEAK.
       END INTERFACE ICB.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. ICC INHERITS FROM ICA ICB.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           INTERFACE ICA
           INTERFACE ICB.
       END INTERFACE ICC.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. ICD INHERITS FROM ICC ICA.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           INTERFACE ICC
           INTERFACE ICA.
       END INTERFACE ICD.

       IDENTIFICATION DIVISION.
       CLASS-ID. IMPCC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           INTERFACE ICD.
       IDENTIFICATION DIVISION.
       OBJECT. IMPLEMENTS ICD.
       PROCEDURE DIVISION.
       METHOD-ID. SPEAK.
       PROCEDURE DIVISION.
           DISPLAY "SPEAK".
       END METHOD SPEAK.
       END OBJECT.
       END CLASS IMPCC.

       IDENTIFICATION DIVISION.
       CLASS-ID. KCL INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. WHO.
       PROCEDURE DIVISION.
           DISPLAY "KCL".
       END METHOD WHO.
       END OBJECT.
       END CLASS KCL.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. IRU.
       PROCEDURE DIVISION.
       METHOD-ID. SPEAK.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R USAGE OBJECT REFERENCE.
       PROCEDURE DIVISION RETURNING R.
       END METHOD SPEAK.
       END INTERFACE IRU.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. IRK.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS KCL.
       PROCEDURE DIVISION.
       METHOD-ID. SPEAK.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R USAGE OBJECT REFERENCE KCL.
       PROCEDURE DIVISION RETURNING R.
       END METHOD SPEAK.
       END INTERFACE IRK.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. IRM INHERITS FROM IRU IRK.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           INTERFACE IRU
           INTERFACE IRK.
       END INTERFACE IRM.

       IDENTIFICATION DIVISION.
       CLASS-ID. IMPRM INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           CLASS KCL
           INTERFACE IRM.
       IDENTIFICATION DIVISION.
       OBJECT. IMPLEMENTS IRM.
       PROCEDURE DIVISION.
       METHOD-ID. SPEAK.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R USAGE OBJECT REFERENCE KCL.
       PROCEDURE DIVISION RETURNING R.
           INVOKE KCL "NEW" RETURNING R.
       END METHOD SPEAK.
       END OBJECT.
       END CLASS IMPRM.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. ALI AS "EXT-ALI".
       PROCEDURE DIVISION.
       METHOD-ID. PING.
       PROCEDURE DIVISION.
       END METHOD PING.
       END INTERFACE ALI.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. ALB INHERITS FROM ALX ALY.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           INTERFACE ALX AS "EXT-ALI"
           INTERFACE ALY AS "EXT-ALI".
       END INTERFACE ALB.

       IDENTIFICATION DIVISION.
       CLASS-ID. IMPAB INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           INTERFACE ALB.
       IDENTIFICATION DIVISION.
       OBJECT. IMPLEMENTS ALB.
       PROCEDURE DIVISION.
       METHOD-ID. PING.
       PROCEDURE DIVISION.
           DISPLAY "PING".
       END METHOD PING.
       END OBJECT.
       END CLASS IMPAB.
