*>--- conformance-case
*> id: pb510-blank-when-zeros
*> rules: [FMT-13.18.8.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.8.2 prints "BLANK WHEN ZERO" with BLANK and ZERO underlined: ZERO is a KEYWORD of the
      *> clause (5.2.2 "They are required in order to select the functionality associated with that
      *> keyword"), not the figurative constant, and ZEROS is a different reserved word (8.9). The
      *> spellings are interchangeable only where the figurative constant itself is written (8.3.3.6.2).
      *> kb/Work PB510: the lexer folded ZERO/ZEROS/ZEROES into ONE token, so this compiled at every
      *> edition and blanked the item. Refused COBOLNET2418.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB510N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B1 PIC 9(3) BLANK WHEN ZEROS.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 0 TO B1
           DISPLAY "[" B1 "]"
           STOP RUN.
