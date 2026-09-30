      *> reject-at: 2002 2014 2023
      *> kb/Work PB758 / decision R58 - END-INVOKE after USING ... RETURNING is a plain syntax error.
      *> ISO §14.9.23.2: the INVOKE format is `INVOKE ... [ USING ... ] [ RETURNING identifier-4 ]` and ends
      *> there; ISO §14.5.1 (Table 12) gives INVOKE no explicit scope terminator (§14.5.3.2 allows a scope to be
      *> terminated only by "its associated scope terminator as specified in Table 12"). END-INVOKE is a
      *> user-defined word (§8.3.2.1; not in §8.9), which cannot begin a statement.
      *> Control: with ` END-INVOKE` removed the same program compiles and prints 0043.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P758NEG2.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CP758B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE CP758B.
       01 A PIC 9(4) VALUE 42.
       01 W PIC 9(4) VALUE 7.
       PROCEDURE DIVISION.
       MAIN-P.
           INVOKE CP758B "NEW" RETURNING O.
           INVOKE O "TAKE" USING A RETURNING W END-INVOKE
           DISPLAY W.
           STOP RUN.
       END PROGRAM P758NEG2.

       IDENTIFICATION DIVISION.
       CLASS-ID. CP758B INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. TAKE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-IN PIC 9(4).
       01 LK PIC 9(4).
       PROCEDURE DIVISION USING LK-IN RETURNING LK.
       MAIN-P.
           ADD 1 TO LK-IN GIVING LK.
       END METHOD TAKE.
       END OBJECT.
       END CLASS CP758B.
