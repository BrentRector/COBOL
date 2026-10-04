      *> reject-at: 2002 2014 2023
      *> ISO 11.7.3 SR4 b) and 9.3.10 ("The inheriting interface shall always conform to each of the inherited
      *> interfaces"): N5OC declares its OWN SPEAK with no formal and inherits SPEAK USING X from N5OA.  The own
      *> prototype shares the inherited one's method resolution signature in this implementation (the method-name,
      *> kb/Work PB1519), so SR4 b) refuses it -- and a contradicting SPEAK could not conform to N5OA in any case
      *> (9.3.8.2.3 rule 1).  The interface twin of the class-side redefinition without OVERRIDE.  COBOLNET2761.
      *> kb/Work PB1502.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1502N3.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
       END PROGRAM PB1502N3.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. N5OA.
       PROCEDURE DIVISION.
       METHOD-ID. SPEAK.
       DATA DIVISION.
       LINKAGE SECTION.
       01 X PIC 9.
       PROCEDURE DIVISION USING X.
       END METHOD SPEAK.
       END INTERFACE N5OA.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. N5OC INHERITS FROM N5OA.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           INTERFACE N5OA.
       PROCEDURE DIVISION.
       METHOD-ID. SPEAK.
       PROCEDURE DIVISION.
       END METHOD SPEAK.
       END INTERFACE N5OC.
