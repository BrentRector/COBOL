      *> reject-at: 2002 2014 2023
      *> kb/Work PB1045 - a constant entry is one of the entries the GLOBAL clause may be written in
      *> (ISO §13.18.27.3 1) a) "A constant entry."), and an instance definition may carry none of them:
      *> ISO §13.18.27.3 4) "The GLOBAL clause shall not be specified in a factory definition, an
      *> instance definition, or a method definition." This constant entry in the OBJECT paragraph's
      *> working storage compiled clean, because the class arm asked only the bound files, reports and
      *> level-1 data items. (OO and the constant entry are both COBOL-2002; hence these editions.)
       IDENTIFICATION DIVISION.
       CLASS-ID. PB1045K.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 KC CONSTANT IS GLOBAL AS 5.
       01 W PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       METHOD-ID. M.
       PROCEDURE DIVISION.
           DISPLAY KC
           GOBACK.
       END METHOD M.
       END OBJECT.
       END CLASS PB1045K.
