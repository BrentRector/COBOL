      *> kb/Work PB1497 -- an ACTIVE-CLASS formal and an ACTIVE-CLASS
      *> returning item in an INTERFACE method prototype's LINKAGE SECTION.
      *> ISO 13.18.60.3 SR16 (cite.py --check 13.18.60.3 "The ACTIVE-CLASS
      *> phrase may be specified only in a factory definition, an instance
      *> definition, or the linkage or local-storage section of a method
      *> definition" -> OK 13.18.60.3 16)) and 10.6.1 NOTE (a
      *> method-definition in an interface-definition defines a method
      *> prototype -> OK 10.6.1): the prototype IS a method definition, so
      *> its linkage section may carry ACTIVE-CLASS.
      *> ISO 9.3.8.2.3 rule 2 d) (cite.py --check 9.3.8.2.3 "If the
      *> parameter in interface-2 is described with the ACTIVE-CLASS
      *> phrase, the corresponding parameter in interface-1 is described
      *> with the ACTIVE-CLASS phrase" -> OK 9.3.8.2.3 2) d)) and rule 5 d)
      *> (cite.py --check 9.3.8.2.3 "If the returning item in interface-2
      *> is described with the ACTIVE-CLASS phrase, the corresponding
      *> returning item in interface-1 is described with the ACTIVE-CLASS
      *> phrase" -> OK 9.3.8.2.3 5) d)): the class CR1497 implements
      *> IR1497 with the same ACTIVE-CLASS formal and returning item, so
      *> it conforms (the containing class is no part of the description).
      *> ISO 14.8.2.3.2 rule 4 b) (-> OK 14.8.2.3.2 4)): X is described
      *> CR1497 ONLY and the method is invoked with X, so the argument
      *> conforms. M displays IN-M, then SETs its ACTIVE-CLASS formal to
      *> SELF (14.9.39.3 SR14); ME returns SELF through its ACTIVE-CLASS
      *> returning item into Y (14.8.3.3 rule 2 b)1.: an ONLY description
      *> of the invoking class), and M runs again through Y.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1497D.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CR1497.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X USAGE OBJECT REFERENCE CR1497 ONLY.
       01 Y USAGE OBJECT REFERENCE CR1497 ONLY.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE CR1497 "NEW" RETURNING X.
           INVOKE X "M" USING X.
           INVOKE X "M" USING X.
           INVOKE X "ME" RETURNING Y.
           INVOKE Y "M" USING Y.
           STOP RUN.
       END PROGRAM PB1497D.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. IR1497.
       PROCEDURE DIVISION.
       METHOD-ID. M.
       DATA DIVISION.
       LINKAGE SECTION.
       01 A USAGE OBJECT REFERENCE ACTIVE-CLASS.
       PROCEDURE DIVISION USING A.
       END METHOD M.
       METHOD-ID. ME.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R USAGE OBJECT REFERENCE ACTIVE-CLASS.
       PROCEDURE DIVISION RETURNING R.
       END METHOD ME.
       END INTERFACE IR1497.

       IDENTIFICATION DIVISION.
       CLASS-ID. CR1497 INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           INTERFACE IR1497.
       IDENTIFICATION DIVISION.
       OBJECT. IMPLEMENTS IR1497.
       PROCEDURE DIVISION.
       METHOD-ID. M.
       DATA DIVISION.
       LINKAGE SECTION.
       01 A USAGE OBJECT REFERENCE ACTIVE-CLASS.
       PROCEDURE DIVISION USING A.
           DISPLAY "IN-M".
           SET A TO SELF.
       END METHOD M.
       METHOD-ID. ME.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R USAGE OBJECT REFERENCE ACTIVE-CLASS.
       PROCEDURE DIVISION RETURNING R.
           SET R TO SELF.
       END METHOD ME.
       END OBJECT.
       END CLASS CR1497.
