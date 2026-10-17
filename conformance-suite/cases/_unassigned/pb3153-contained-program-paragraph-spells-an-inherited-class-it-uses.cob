*>--- conformance-case
*> id: pb3153-contained-program-paragraph-spells-an-inherited-class-it-uses
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3153 - ISO 8.3.2.2: "Within a source element, a given
      *>   user-defined word may be used as only one type of user-defined
      *>   word". A contained program is a source element of its own
      *>   (10.5). PB3153N1 USES PB3153C as a class-name (the class
      *>   condition X IS PB3153C; the class is declared by the
      *>   CONTAINING program's SPECIAL-NAMES, which 8.4.6.1 lets it
      *>   reference) and DECLARES PB3153C as a paragraph-name: two types
      *>   in PB3153N1's source element, refused COBOLNET2692 exactly as
      *>   the same shape is in one program. PB3153N2 declares the same
      *>   paragraph and never uses the class: legal (8.4.6.1), so it
      *>   draws no diagnostic.
      *> cite.py --check 8.3.2.2 "a given user-defined word may be used
      *>   as only one type of user-defined word" -> OK 8.3.2.2; 10.5 "A
      *>   source element is a source unit excluding any contained source
      *>   units" -> OK 10.5; 8.4.6.1 "may be referenced only by
      *>   statements and entries either in that source element that
      *>   contains a configuration section or in any source element
      *>   contained within that source element" -> OK 8.4.6.1
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3153N.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CLASS PB3153C IS "0" THRU "9".
       PROCEDURE DIVISION.
       MAIN-P.
           CALL "PB3153N1".
           CALL "PB3153N2".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3153N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X VALUE "5".
       PROCEDURE DIVISION.
       MAIN-P.
           IF X IS PB3153C DISPLAY "DIGIT" END-IF.
           PERFORM PB3153C.
           EXIT PROGRAM.
       PB3153C.
           DISPLAY "IN".
       END PROGRAM PB3153N1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3153N2.
       PROCEDURE DIVISION.
       MAIN-P.
           PERFORM PB3153C.
           EXIT PROGRAM.
       PB3153C.
           DISPLAY "IN2".
       END PROGRAM PB3153N2.
       END PROGRAM PB3153N.
