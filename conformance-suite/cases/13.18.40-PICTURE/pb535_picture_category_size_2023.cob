*>--- conformance-case
*> id: pb535_picture_category_size_2023
*> rules: [GR-13.18.40.4-3, GR-13.18.40.4-4, GR-13.18.40.4-6, GR-13.18.40.4-11]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB535 - ISO 1989:2023 13.18.40.4 GR3's EIGHT CATEGORIES, each one constructible, each one
      *> sized by GR4. "A PICTURE clause defines the subject of the entry to fall into one of the following
      *> categories of data: alphabetic, alphanumeric, alphanumeric-edited, boolean, national,
      *> national-edited, numeric, numeric-edited." Three of them - boolean (GR8), national (GR9) and
      *> national-edited (GR10) - are the COBOL-2002 additions, so this is the edition at which the whole
      *> list is expressible; the introduction gate below 2002 is COBOLNET0900 (constructs boolean-data-2002,
      *> national-data-2002, national-edited-2002, and negative golden pb492-national-edited-at-85).
      *>
      *> FUNCTION LENGTH returns the argument's length in character or boolean positions (ISO 15.50.1), which
      *> below is GR4's count of the symbols of character-string-1 that represent a character position - GR14
      *> excluding exactly 'P', 'V' and the 'S' of an item with no SIGN SEPARATE phrase:
      *>
      *> C01 AAAA          alphabetic (GR5)          = 4     four 'A'
      *> C02 XXX9          alphanumeric (GR6)        = 4     three 'X' + one '9'
      *> C03 XX0XX         alphanumeric-edited (GR7) = 5     four 'X' + the inserted zero
      *> C04 1(4)          boolean (GR8)             = 4     four boolean positions - GR4's other half
      *> C05 N(3)          national (GR9)            = 3     three national character positions (GR1)
      *> C06 NNBNN         national-edited (GR10)    = 5     four 'N' + the inserted space
      *> C07 S9(3)V99      numeric (GR11)            = 5     five '9'; the 'S' and the 'V' are not counted
      *> C08 9(3)PP        numeric (GR11)            = 3     three '9'; neither 'P' is counted
      *> C09 ZZ,ZZ9.99     numeric-edited (GR13 a)   = 9     every symbol is a character position
      *> C10 999CR         numeric-edited (GR13 a)   = 5     GR14 counts EACH character of 'CR'
      *> C11 9(5)E+99      numeric-edited (GR13 b)   = 9     five '9', the 'E', the '+' and two '9'
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB535C23.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 C01 PIC AAAA.
       01 C02 PIC XXX9.
       01 C03 PIC XX0XX.
       01 C04 PIC 1(4).
       01 C05 PIC N(3).
       01 C06 PIC NNBNN.
       01 C07 PIC S9(3)V99.
       01 C08 PIC 9(3)PP.
       01 C09 PIC ZZ,ZZ9.99.
       01 C10 PIC 999CR.
       01 C11 PIC 9(5)E+99.
       PROCEDURE DIVISION.
           DISPLAY "C01=" FUNCTION LENGTH(C01)
           DISPLAY "C02=" FUNCTION LENGTH(C02)
           DISPLAY "C03=" FUNCTION LENGTH(C03)
           DISPLAY "C04=" FUNCTION LENGTH(C04)
           DISPLAY "C05=" FUNCTION LENGTH(C05)
           DISPLAY "C06=" FUNCTION LENGTH(C06)
           DISPLAY "C07=" FUNCTION LENGTH(C07)
           DISPLAY "C08=" FUNCTION LENGTH(C08)
           DISPLAY "C09=" FUNCTION LENGTH(C09)
           DISPLAY "C10=" FUNCTION LENGTH(C10)
           DISPLAY "C11=" FUNCTION LENGTH(C11)
           STOP RUN.
