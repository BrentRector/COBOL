      *> reject-at: 2002 2014 2023
      *> kb/Work PB1051 -- ISO 9.3.8.2.3 rule 1 (cite.py --check 9.3.8.2.3 "The number of parameters are
      *> the same, with consistent BY REFERENCE and BY VALUE specifications" -> OK 9.3.8.2.3 1)):
      *> the prototype's formal is BY VALUE and the implementation's is BY REFERENCE, so the class does not conform
      *> to the interface (COBOLNET0841, ISO 11.8.3 SR2 / 9.3.11).
       IDENTIFICATION DIVISION.
       INTERFACE-ID. IBVN7.
       PROCEDURE DIVISION.
       METHOD-ID. M.
       DATA DIVISION.
       LINKAGE SECTION.
       01 A PIC S9(4) COMP-5.
       PROCEDURE DIVISION USING BY VALUE A.
       END METHOD M.
       END INTERFACE IBVN7.

       IDENTIFICATION DIVISION.
       CLASS-ID. CBVN7 INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           INTERFACE IBVN7.
       IDENTIFICATION DIVISION.
       OBJECT. IMPLEMENTS IBVN7.
       PROCEDURE DIVISION.
       METHOD-ID. M.
       DATA DIVISION.
       LINKAGE SECTION.
       01 A PIC S9(4) COMP-5.
       PROCEDURE DIVISION USING BY REFERENCE A.
           CONTINUE.
       END METHOD M.
       END OBJECT.
       END CLASS CBVN7.
