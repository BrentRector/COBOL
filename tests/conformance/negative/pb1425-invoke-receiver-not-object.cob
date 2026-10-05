      *> reject-at: 2002 2014 2023
      *> kb/Work PB1425 - ISO 14.9.23.3 SR1: "Identifier-1 shall be an object reference." An inline method
      *>   invocation is a legal INVOKE receiver only when the temporary it references is one (8.4.3.4.4 GR1:
      *>   the method's RETURNING description). WHO returns PIC 9(4), so `INVOKE A1 :: "WHO" "SPEAK"` names
      *>   a numeric receiver and is refused by SR1 (COBOLNET0824), naming the receiver as written.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1425N1.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1425N1K.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A1 USAGE OBJECT REFERENCE PB1425N1K.
       PROCEDURE DIVISION.
           INVOKE PB1425N1K "NEW" RETURNING A1
           INVOKE A1 :: "WHO" "SPEAK"
           STOP RUN.
       END PROGRAM PB1425N1.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1425N1K INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. WHO.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LW PIC 9(4).
       PROCEDURE DIVISION RETURNING LW.
           MOVE 1 TO LW.
       END METHOD WHO.
       METHOD-ID. SPEAK.
       PROCEDURE DIVISION.
           DISPLAY "SPOKEN".
       END METHOD SPEAK.
       END OBJECT.
       END CLASS PB1425N1K.
