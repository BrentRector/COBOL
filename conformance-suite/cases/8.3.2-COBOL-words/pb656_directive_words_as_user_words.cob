      *>--- conformance-case
      *> id: pb656_directive_words_as_user_words
      *> rules: [SR-8.3.2.1-2]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB656 - ISO 8.3.2.1 2): "Compiler-directive words,
      *>   except those that are also reserved words, may be used as
      *>   user-defined words and system-names." (cite.py --check
      *>   8.3.2.1 -> OK  8.3.2.1 2)). Each word below is in the 8.12
      *>   list and NOT in the 2023 8.9 list, so at 2023 it is a
      *>   computer-name (a system-name, 8.3.2.3), a data-name, a
      *>   condition-name, a section-name and a paragraph-name - in the
      *>   same compilation as the directives that spell them. COBOL is
      *>   one of them at 2023 only: through 2014 it is a reserved word
      *>   (negative/pb656-cobol-reserved-through-2014; docs/CONFORMANCE
      *>   D-RW3).
       >>DEFINE PB656V AS 5
       >>TURN EC-SIZE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB656DW.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. DEFINE.
       OBJECT-COMPUTER. PROPAGATE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 COBOL      PIC X(3) VALUE "CBL".
       01 TURN       PIC 9    VALUE 3.
       01 PUSH       PIC X    VALUE "P".
       01 LISTING    PIC X(2) VALUE "LS".
       01 PARAMETER  PIC 99   VALUE 42.
       01 LEAP-SECOND PIC 9   VALUE 1.
       01 FIXED      PIC X    VALUE "F".
       01 EQUATE     PIC 9    VALUE 0.
           88 CHECKING        VALUE 7.
       PROCEDURE DIVISION.
       UNDEFINE SECTION.
       POP.
           DISPLAY COBOL " " TURN " " PUSH " " LISTING
           DISPLAY PARAMETER " " LEAP-SECOND " " FIXED
           SET CHECKING TO TRUE
           IF CHECKING
               DISPLAY "CK=" EQUATE
           END-IF
       >>IF PB656V = 5
           DISPLAY "DEFINED"
       >>END-IF
           STOP RUN.
