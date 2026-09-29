      *> reject-at: 2002 2014 2023
      *> kb/Work PB1112 -- ISO 14.8.2.3.2 rule 4: an ACTIVE-CLASS formal
      *> passed BY REFERENCE takes a) an ACTIVE-CLASS argument only when
      *> "the method to be activated shall be invoked with the predefined
      *> object references SELF or SUPER, or with an object reference
      *> described with the ACTIVE-CLASS phrase", or b) an argument
      *> "described with an object-class-name and the ONLY phrase" when it
      *> is invoked with that class-name or an ONLY reference to it. Here N
      *> invokes M through O2, described C1 WITHOUT ONLY, with the
      *> ACTIVE-CLASS argument Y: neither alternative holds -- COBOLNET0828.
      *> (Before the fix the invocation was never consulted and this ran.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB12R.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS C12R.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE C12R.
       PROCEDURE DIVISION.
           INVOKE C12R "NEW" RETURNING O
           INVOKE O "N"
           STOP RUN.
       END PROGRAM PB12R.
       IDENTIFICATION DIVISION.
       CLASS-ID. C12R INHERITS FROM BASE.
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
       METHOD-ID. N.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 Y USAGE OBJECT REFERENCE ACTIVE-CLASS.
       01 O2 USAGE OBJECT REFERENCE C12R.
       PROCEDURE DIVISION.
           SET Y TO SELF
           SET O2 TO SELF
           INVOKE O2 "M" USING BY REFERENCE Y.
       END METHOD N.
       END OBJECT.
       END CLASS C12R.
