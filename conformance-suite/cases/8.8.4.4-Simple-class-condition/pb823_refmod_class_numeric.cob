      *>--- conformance-case
      *> id: pb823_refmod_class_numeric
      *> rules: [GR-8.8.4.4.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 1989:2023 8.4.3.3.4 GR6 c): a reference-modified numeric item is "considered class and category
      *> alphanumeric" (national when its usage is national), so 8.8.4.4.4 GR3 n) 2. governs its NUMERIC class
      *> test - true only if the content "consists entirely of the characters 0, 1, 2, 3, ..., 9", with NO
      *> operational sign admitted. S PIC S9(4) VALUE -1234 is stored as the over-punched image 123M (the default
      *> ibm sign encoding, 13.18.52.4 GR5 b), so:
      *>   S (1:4)       NUMERIC -> NO   (n) 2.: 'M' is not a digit)
      *>   S             NUMERIC -> YES  (n) 1. a.: the operational sign agrees with the data description)
      *>   TS (1) (1:4)  NUMERIC -> NO   (the same slice through a subscript)
      *>   S (1:3)       NUMERIC -> YES  ("123" - all digits)
      *>   S (1:3)       ALPHABETIC is LEGAL (SR4 names the OPERAND's category, alphanumeric here) -> NO
      *> kb/Work PB823: the renderer asked the ITEM's picture (numeric, signed) about the slice and answered YES.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB823REFMODNUM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S PIC S9(4) VALUE -1234.
       01 T.
          05 TS PIC S9(4) OCCURS 2.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE -1234 TO TS (1)
           IF S (1:4) IS NUMERIC DISPLAY "REFMOD-NUM=Y"
           ELSE DISPLAY "REFMOD-NUM=N" END-IF
           IF S IS NUMERIC DISPLAY "WHOLE-NUM=Y"
           ELSE DISPLAY "WHOLE-NUM=N" END-IF
           IF TS (1) (1:4) IS NUMERIC DISPLAY "SUB-REFMOD-NUM=Y"
           ELSE DISPLAY "SUB-REFMOD-NUM=N" END-IF
           IF S (1:3) IS NUMERIC DISPLAY "DIGITS-NUM=Y"
           ELSE DISPLAY "DIGITS-NUM=N" END-IF
           IF S (1:3) IS ALPHABETIC DISPLAY "DIGITS-ALPHA=Y"
           ELSE DISPLAY "DIGITS-ALPHA=N" END-IF
           STOP RUN.
