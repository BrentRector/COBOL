      *> kb/Work PB1497 (sibling sweep) -- an ACTIVE-CLASS formal of an
      *> INHERITED method, invoked on an instance of the SUBCLASS.
      *> ISO 14.8.2.3.2 rule 4 b) (cite.py --check 14.8.2.3.2 "an object
      *> reference described with an object-class-name and the ONLY
      *> phrase" -> OK 14.8.2.3.2 4)): Y is described DAC1 ONLY and the
      *> method is invoked with Y, so the argument conforms; 13.18.60.4
      *> GR22 e) (cite.py --check 13.18.60.4 "the object referenced by
      *> this data item shall be of the same class as the object that was
      *> used to invoke the method" -> OK 13.18.60.4 22)) makes the
      *> formal a DAC1 object although M is declared in CAC1: the formal
      *> is the ACTIVATION's class, not the declaring class. Before this
      *> the generated call passed a DAC1 field to a CAC1 ref parameter.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1497S.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CAC1
           CLASS DAC1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 Y USAGE OBJECT REFERENCE DAC1 ONLY.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE DAC1 "NEW" RETURNING Y.
           INVOKE Y "M" USING Y.
           STOP RUN.
       END PROGRAM PB1497S.

       IDENTIFICATION DIVISION.
       CLASS-ID. CAC1 INHERITS FROM BASE.
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
       01 A USAGE OBJECT REFERENCE ACTIVE-CLASS.
       PROCEDURE DIVISION USING A.
           DISPLAY "IN-M".
       END METHOD M.
       END OBJECT.
       END CLASS CAC1.

       IDENTIFICATION DIVISION.
       CLASS-ID. DAC1 INHERITS FROM CAC1.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CAC1.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       END OBJECT.
       END CLASS DAC1.
