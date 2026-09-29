      *> reject-at: 2002 2014 2023
      *> kb/Work PB1112 -- ISO 14.8.2.3.3: an ACTIVE-CLASS formal passed BY
      *> CONTENT conforms by one of two alternatives, each a condition on
      *> the invocation AND a SET: 1) invoked with SELF, SUPER or an
      *> ACTIVE-CLASS reference, and the argument SETs into an ACTIVE-CLASS
      *> receiver; 2) "invoked with an object-class-name or with an object
      *> reference described with an object-class-name and the ONLY
      *> phrase", and the argument SETs into "an object reference
      *> described with that object-class-name and the ONLY phrase". The
      *> program invokes M through X (C1 ONLY) with the argument O
      *> (described C1, no ONLY): alternative 1)'s invocation fails, and
      *> 14.9.39.3 SR12 a)1. refuses O into a C1 ONLY receiver --
      *> COBOLNET0828. (Before the fix only SR14 into the formal was asked.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB12C.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS C12C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X USAGE OBJECT REFERENCE C12C ONLY.
       01 O USAGE OBJECT REFERENCE C12C.
       PROCEDURE DIVISION.
           INVOKE C12C "NEW" RETURNING X
           SET O TO X
           INVOKE X "M" USING BY CONTENT O
           STOP RUN.
       END PROGRAM PB12C.
       IDENTIFICATION DIVISION.
       CLASS-ID. C12C INHERITS FROM BASE.
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
       01 LK USAGE OBJECT REFERENCE ACTIVE-CLASS.
       PROCEDURE DIVISION USING LK.
           DISPLAY "IN-M".
       END METHOD M.
       END OBJECT.
       END CLASS C12C.
