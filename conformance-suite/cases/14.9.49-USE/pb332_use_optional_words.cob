      *>--- conformance-case
      *> id: pb332_use_optional_words
      *> rules: [FMT-14.9.49.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> !! ALL FOUR OPTIONAL WORDS OF USE FORMAT 1, OMITTED (kb/Work PB332).
      *> ISO 14.9.49.2 Format 1 prints
      *>     USE [ GLOBAL ] AFTER STANDARD { EXCEPTION | ERROR } PROCEDURE ON { ... }
      *> with USE, GLOBAL, EXCEPTION, ERROR, INPUT, OUTPUT, I-O and EXTEND UNDERLINED and AFTER,
      *> STANDARD, PROCEDURE and ON NOT underlined - measured off the PDF's vector rectangles and
      *> confirmed on the 600 dpi render of printed folio 774. 5.2.3 makes a non-underlined uppercase
      *> word an OPTIONAL word, and 8.3.2.4.3 says such a word "may be specified at the user's option
      *> with no effect on the semantics of the format". Only STANDARD and ON were accepted as
      *> optional before PB332 - the two words the witness corpus happened to omit.
      *> DERIVATION - the expected lines follow from the rules, nothing from the compiler.
      *>  . Neither file is ever opened. 14.9.6.4 GR1: "If the file connector is not open, the CLOSE
      *>    statement is unsuccessful and the I-O status indicator for the file connector is set to
      *>    '42'"; 9.1.13.7 rule 2 names the same value. So both handlers report 42.
      *>  . 14.9.49.4 GR6 a): "If file-name-1 is specified, the associated procedure is executed when
      *>    the condition described in the USE statement occurs" - each file-scoped declarative runs
      *>    once, in the order its CLOSE is executed.
      *>  . 8.3.2.4.3 is what makes the two declaratives one statement form: the TERSE section, which
      *>    writes none of the four optional words, must behave exactly as the FULL one, which writes
      *>    all four. The two blocks below are byte-identical but for the tag and the file.
      *>  . Status 42 is a "4x" value, which Table 13 classifies EC-I-O-LOGIC-ERROR / Fatal, so what
      *>    happens after the USE procedure falls to 14.9.49.4 GR7 c): "the implementor determines
      *>    what action is taken as described in 9.1.13". This compiler takes the 14.9.49.4 GR7 b) action -
      *>    control returns to an implicit CONTINUE after the offending statement, which is the
      *>    surveyed behaviour - so the AFTER- lines show the same 42 the handler saw. That choice is
      *>    NOT what this golden is about: its subject is 8.3.2.4.3, and TERSE and FULL are asserted
      *>    to agree line for line WHATEVER the resumption rule, because an optional word cannot
      *>    change the semantics of the format.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB332UOW.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb332uow-1.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST1.
           SELECT F2 ASSIGN TO "pb332uow-2.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST2.
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1 PIC X(8).
       FD F2.
       01 R2 PIC X(8).
       WORKING-STORAGE SECTION.
       01 ST1 PIC XX.
       01 ST2 PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       TERSE-SECT SECTION.
           USE ERROR F1.
       TERSE-PARA.
           DISPLAY "TERSE=" ST1.
       FULL-SECT SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F2.
       FULL-PARA.
           DISPLAY "FULL=" ST2.
       END DECLARATIVES.
       MAIN-SECT SECTION.
       MAIN.
           CLOSE F1
           DISPLAY "AFTER-F1=" ST1
           CLOSE F2
           DISPLAY "AFTER-F2=" ST2
           DISPLAY "DONE"
           STOP RUN.
