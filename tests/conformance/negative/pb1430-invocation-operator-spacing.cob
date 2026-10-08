      *> reject-at: 2002 2014 2023
      *> kb/Work PB1430 - ISO 8.7.4: "The invocation operator is the two
      *> contiguous COBOL characters '::', which shall be immediately
      *> preceded and followed by a separator space." A1:: "GETNAME" has
      *> no space before the operator and no literal beside it, so no
      *> 8.3.5 rule can see it; it used to compile and run. COBOLNET2993.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1430N2.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1430N2C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A1 USAGE OBJECT REFERENCE PB1430N2C.
       01 W  PIC X(8).
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB1430N2C "NEW" RETURNING A1.
           MOVE A1:: "GETNAME" TO W.
           DISPLAY W.
           STOP RUN.
       END PROGRAM PB1430N2.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1430N2C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. GETNAME.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R PIC X(7).
       PROCEDURE DIVISION RETURNING R.
       MAIN.
           MOVE "ACCOUNT" TO R.
       END METHOD GETNAME.
       END OBJECT.
       END CLASS PB1430N2C.
