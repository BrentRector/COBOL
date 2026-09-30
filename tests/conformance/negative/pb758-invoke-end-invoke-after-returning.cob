      *> reject-at: 2002 2014 2023
      *> kb/Work PB758 / decision R58 - END-INVOKE is NOT COBOL, so `INVOKE ... END-INVOKE` is a plain syntax error.
      *> ISO §14.9.23.2: the INVOKE general format ends at `[ RETURNING identifier-4 ]` and nothing follows it.
      *> ISO §14.5.1 (Table 12): INVOKE has no conditional phrase and no explicit scope terminator, so there is
      *> no scope for a terminator to delimit (§14.5.3.2). END-INVOKE is in no ISO word list (§8.9 lists 23
      *> END-xxx words, END-INVOKE not among them) and is therefore a user-defined word (§8.3.2.1, kb/Work
      *> PB1689); as a user word it cannot stand where a statement is expected. Not a declined ISO facility
      *> (§4.2.7) - there is no facility - so the answer is the ordinary syntax error, not a named decline.
      *> Control: the same program with ` END-INVOKE` removed compiles and prints 0042 (ec_oo_universal_both and
      *> the many INVOKE ... RETURNING goldens compile this shape).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P758NEG1.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CP758A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE CP758A.
       01 W PIC 9(4) VALUE 7.
       PROCEDURE DIVISION.
       MAIN-P.
           INVOKE CP758A "NEW" RETURNING O.
           INVOKE O "TAKE" RETURNING W END-INVOKE.
           DISPLAY W.
           STOP RUN.
       END PROGRAM P758NEG1.

       IDENTIFICATION DIVISION.
       CLASS-ID. CP758A INHERITS FROM BASE.
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
       01 LK PIC 9(4).
       PROCEDURE DIVISION RETURNING LK.
       MAIN-P.
           MOVE 42 TO LK.
       END METHOD TAKE.
       END OBJECT.
       END CLASS CP758A.
