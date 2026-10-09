      *> reject-at: 2002 2014
      *> kb/Work PB1965 - before COBOL 2023 Annex C maps FINAL SIGMA
      *> U+03C2 to SIGMA U+03C3 (the mapping Annex E.2 item 14 deleted
      *> at 2023), so Xς and Xσ are ONE method-name and the class
      *> defines it twice. At 2023 they are two (the positive golden
      *> 2023/pb1965_annex_c_word_keys).
       IDENTIFICATION DIVISION.
       CLASS-ID. PB1965NC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. Xς.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "METHOD FINAL-SIGMA".
       END METHOD Xς.
       METHOD-ID. Xσ.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "METHOD SIGMA".
       END METHOD Xσ.
       END OBJECT.
       END CLASS PB1965NC.
