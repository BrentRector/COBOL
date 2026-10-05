      *> kb/Work PB1374 - the printed defaults of the FLAG-14 directive's two pairs of braces.
      *>
      *> ISO 1989:2023 7.3.15.2 prints  >> FLAG-14 { ALL | |option ...| } { ON | OFF }  with ALL and ON NOT
      *> underlined (the rendered page, not the OCR, decides it). 5.2.3: words "shown in uppercase and not
      *> underlined in general formats" are OPTIONAL words. 5.2.6.3: "If one of the alternatives contains only
      *> optional words, that alternative is the default and is selected unless another alternative is
      *> explicitly specified". So ALL is the default of the outer braces, ON the default of the trailing ones,
      *> and every spelling below is LEGAL: an option list with ON omitted, ALL with ON omitted, ON with ALL
      *> omitted, the bare directive, a lone OFF, and several options in any order with OFF.
      *>
      *> Each directive sits between data description entries (7.3.15.3 SR1), and the PROCEDURE DIVISION runs
      *> only if the whole compilation group was accepted: any of them rejected with COBOLNET1622 (the pre-fix
      *> behaviour for every spelling with ON or ALL omitted) is a compile failure and the program never prints.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE SPEC AND NOT FROM A RUN: R is VALUE 7 -> "7".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1374F14.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       >>FLAG-14 VALUE-ZERO
       >>FLAG-14 ALL
       >>FLAG-14 ON
       >>FLAG-14
       >>FLAG-14 OFF
       >>FLAG-14 WRITE-END-OF-PAGE READ-PREVIOUS EVALUATE OFF
       >>FLAG-14 I-O-STATUS-04 I-O-STATUS-07
       01 R PIC 9 VALUE 7.
       PROCEDURE DIVISION.
           DISPLAY R.
           STOP RUN.
