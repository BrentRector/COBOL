      *> reject-at: 2002 2014 2023
      *> kb/Work PB1394 - ISO 8.3.3.4.3 SR2: "Boolean-character-1 shall
      *>  be a boolean character, '0' or '1'". B"012" is a boolean
      *>  literal holding the character 2; beside the item B it used to
      *>  compile as B followed by "012" and print QQQ012. COBOLNET2630.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGW73ABL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B PIC X(3) VALUE "QQQ".
       PROCEDURE DIVISION.
           DISPLAY B"012".
           STOP RUN.
