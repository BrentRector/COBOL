      *> reject-at: 2002 2014 2023
      *> kb/Work PB1498 - ISO 9.3.8.2.3: "If the description of the
      *> returning item of a method in interface-1 directly or indirectly
      *> references interface-2, the description of the returning item of
      *> the corresponding method in interface-2 shall not directly or
      *> indirectly reference interface-1." Interface-1 is CCIRC's (the
      *> implementing class), interface-2 is ICIRC. CCIRC's M returns a
      *> CCIRC, which IMPLEMENTS ICIRC - it reaches interface-2; ICIRC's M
      *> returns a CCIRC - it references interface-1 directly. "Indirectly"
      *> is the transitive closure (owner decision kb/Work R63), so CCIRC
      *> does not conform to ICIRC: COBOLNET0841. (Before the fix nothing
      *> examined the sentence and the program ran.)
       IDENTIFICATION DIVISION.
       INTERFACE-ID. ICIRC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY. CLASS CCIRC.
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. M.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R USAGE OBJECT REFERENCE CCIRC.
       PROCEDURE DIVISION RETURNING R.
       END METHOD M.
       END INTERFACE ICIRC.
       IDENTIFICATION DIVISION.
       CLASS-ID. CCIRC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY. CLASS BASE CLASS CCIRC INTERFACE ICIRC.
       IDENTIFICATION DIVISION.
       OBJECT. IMPLEMENTS ICIRC.
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. M.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R USAGE OBJECT REFERENCE CCIRC.
       PROCEDURE DIVISION RETURNING R.
           SET R TO SELF
           GOBACK.
       END METHOD M.
       END OBJECT.
       END CLASS CCIRC.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1498D.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY. CLASS CCIRC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE CCIRC.
       PROCEDURE DIVISION.
           INVOKE CCIRC "NEW" RETURNING O
           DISPLAY "OK"
           STOP RUN.
       END PROGRAM PB1498D.
