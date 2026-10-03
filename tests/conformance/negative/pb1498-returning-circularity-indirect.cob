      *> reject-at: 2002 2014 2023
      *> kb/Work PB1498 - ISO 9.3.8.2.3's closing sentence, reached
      *> INDIRECTLY through a third interface (owner decision kb/Work R63:
      *> the transitive closure over returning-item object references
      *> and a class's IMPLEMENTS). Interface-1 is CIND's (the implementing
      *> class), interface-2 is IIND. CIND's M returns an IHOP; IHOP's
      *> H returns a CIND, which IMPLEMENTS IIND - interface-2 is reached.
      *> IIND's M returns an IHOP, whose H returns a CIND - interface-1 is
      *> reached. Neither M names the other interface itself, and the pair
      *> still does not conform: COBOLNET0841.
       IDENTIFICATION DIVISION.
       INTERFACE-ID. IHOP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY. CLASS CIND.
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. H.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R USAGE OBJECT REFERENCE CIND.
       PROCEDURE DIVISION RETURNING R.
       END METHOD H.
       END INTERFACE IHOP.
       IDENTIFICATION DIVISION.
       INTERFACE-ID. IIND.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY. INTERFACE IHOP.
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. M.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R USAGE OBJECT REFERENCE IHOP.
       PROCEDURE DIVISION RETURNING R.
       END METHOD M.
       END INTERFACE IIND.
       IDENTIFICATION DIVISION.
       CLASS-ID. CIND INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY. CLASS BASE CLASS CIND INTERFACE IIND
           INTERFACE IHOP.
       IDENTIFICATION DIVISION.
       OBJECT. IMPLEMENTS IIND.
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. M.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R USAGE OBJECT REFERENCE IHOP.
       PROCEDURE DIVISION RETURNING R.
           SET R TO NULL
           GOBACK.
       END METHOD M.
       END OBJECT.
       END CLASS CIND.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1498X.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY. CLASS CIND.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE CIND.
       PROCEDURE DIVISION.
           INVOKE CIND "NEW" RETURNING O
           DISPLAY "OK"
           STOP RUN.
       END PROGRAM PB1498X.
