      *> ISO 1989:2023 §10.6 (class definition structure): the CLASS-level ENVIRONMENT DIVISION applies to the
      *> factory and object definitions. The DEVLOG-738 latent-bug pin (fixed P9 close): when the OBJECT
      *> paragraph carries its OWN environment division, the former singular env read SHADOWED the class-level
      *> one - here the class-level SPECIAL-NAMES CURRENCY SIGN "U" must still reach the OBJECT half's PICTURE
      *> binder (pre-fix this program failed to compile: 'E' was an unknown PICTURE symbol in the object half; the U symbol here is SR22-legal),
      *> while the object's own bare ENVIRONMENT DIVISION header (legal: 12.3.3 SR3 forbids only the SOURCE-COMPUTER, OBJECT-COMPUTER and REPOSITORY paragraphs there) must not shadow it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OOENVP9.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CENVP9.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE CENVP9.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE CENVP9 "NEW" RETURNING O.
           INVOKE O "SHOW".
           STOP RUN.
       END PROGRAM OOENVP9.

       IDENTIFICATION DIVISION.
       CLASS-ID. CENVP9 INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN IS "U".
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9(3)V99 VALUE 12.34.
       01 ED PIC U999.99.
       PROCEDURE DIVISION.
       METHOD-ID. SHOW.
       PROCEDURE DIVISION.
       MAIN.
           MOVE N TO ED.
           DISPLAY "ED=" ED.
       END METHOD SHOW.
       END OBJECT.
       END CLASS CENVP9.
