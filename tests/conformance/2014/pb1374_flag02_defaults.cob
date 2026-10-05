      *> kb/Work PB1374 - the printed defaults of the FLAG-02 directive's two pairs of braces.
      *>
      *> ISO 1989:2023 7.3.14.2 prints  >> FLAG-02 { ALL | |option ...| } { ON | OFF }  with ALL and ON NOT
      *> underlined (the rendered page, not the OCR, decides it). 5.2.3: words "shown in uppercase and not
      *> underlined in general formats" are OPTIONAL words. 5.2.6.3: "If one of the alternatives contains only
      *> optional words, that alternative is the default and is selected unless another alternative is
      *> explicitly specified". So ALL is the default of the outer braces, ON the default of the trailing ones,
      *> and every spelling below is LEGAL: an option list with ON omitted, ON with ALL omitted, the bare
      *> directive, a lone OFF, and several options in any order with OFF. FLAG-02 is a 2014 introduction
      *> (obsolete at 2023, still supported), so 2014 is its introducing edition.
      *>
      *> Each directive sits between data description entries (7.3.14.3 SR1), and the PROCEDURE DIVISION runs
      *> only if the whole compilation group was accepted: any of them rejected with COBOLNET1622 (the pre-fix
      *> behaviour for ON, the bare form and a lone OFF) is a compile failure and the program never prints.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE SPEC AND NOT FROM A RUN: R is VALUE 5 -> "5".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1374F02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       >>FLAG-02 I-O-STATUS-07
       >>FLAG-02 ON
       >>FLAG-02
       >>FLAG-02 OFF
       >>FLAG-02 TERMINATE-WITH-VARYING MOVE-TO-SAME-NAME OFF
       >>FLAG-02 EC-PROGRAM-EXCEPTIONS OFF
       >>FLAG-02 RANGE-EXCEPTION-FOR-INDEX
       01 R PIC 9 VALUE 5.
       PROCEDURE DIVISION.
           DISPLAY R.
           STOP RUN.
