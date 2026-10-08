      *> reject-at: 2002 2014 2023
      *> kb/Work PB1544 -- ISO 14.9.23.3 SR16: "If literal-2 or its corresponding formal parameter is
      *> specified with the BY VALUE phrase, literal-2 shall be a numeric literal." A symbolic-character
      *> is literal-2 - it "defines a figurative constant" (12.3.7.4 GR11 a)) - and never numeric
      *> (8.3.3.6.3 SR1 a) admits only ZERO), asked of the ONE literal-alias resolution: INVOKE read
      *> the bare word as identifier-3 and answered "not defined".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1544N1.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SA IS 66.
       REPOSITORY.
           CLASS CBV1544.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE CBV1544.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE CBV1544 "NEW" RETURNING O.
           INVOKE O "M" USING BY VALUE SA.
           STOP RUN.
       END PROGRAM PB1544N1.

       IDENTIFICATION DIVISION.
       CLASS-ID. CBV1544 INHERITS FROM BASE.
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
       01 LN PIC S9(4) COMP-5.
       PROCEDURE DIVISION USING BY VALUE LN.
           CONTINUE.
       END METHOD M.
       END OBJECT.
       END CLASS CBV1544.
