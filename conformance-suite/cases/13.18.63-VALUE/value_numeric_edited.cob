*>--- conformance-case
*> id: value_numeric_edited
*> rules: [SR-13.18.63.3-11]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> Numeric-edited VALUE rework, COBOL-2023 (ISO §13.18.63 SR6/SR11; Annex E.2
      *> item 28 + E.3.3 item 43). VCR 35: a figurative ZERO on a numeric-edited item
      *> is now treated as the numeric literal zero and EDITED per PICTURE (no longer
      *> the left-justified "0000000") — and BLANK WHEN ZERO now effects the init
      *> (NOTE 2), so NE-BWZ initializes to spaces. The integer/decimal forms of the
      *> literal zero (NE-LIT0) are edited at all editions (SR6 exemption). VCR 86: a
      *> non-zero numeric literal VALUE (NE-NUM) is a 2023 capability, edited per the
      *> MOVE rules.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NUMED-VAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NE-FIG  PIC $ZZ9.99 VALUE ZERO.
       01 NE-BWZ  PIC $ZZ9.99 BLANK WHEN ZERO VALUE ZERO.
       01 NE-LIT0 PIC $ZZ9.99 VALUE 0.
       01 NE-NUM  PIC $ZZ9.99 VALUE 12.5.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "FIG=["  NE-FIG  "]".
           DISPLAY "BWZ=["  NE-BWZ  "]".
           DISPLAY "LIT0=[" NE-LIT0 "]".
           DISPLAY "NUM=["  NE-NUM  "]".
           STOP RUN.
