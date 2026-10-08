      *> reject-at: 85 2002 2014 2023
      *> ISO 1989:2023 §14.9.11.2 Format 1: DISPLAY ... [ UPON mnemonic-name-1 ]. A mnemonic-name "identifies an implementor-defined
      *> device-name, feature-name, or switch-name" (§8.3.2.2.16): a word with no data description, so it
      *> takes no subscript or reference modification, and no qualified format of §8.4.2.2.2 names one.
      *> MYOUT written with a subscript is no DISPLAY statement at any edition. COBOLNET2269 (kb/Work PB2499).
      *> DISPLAY's slot was already a word; the parse error now names the rule.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2499DS.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYSOUT IS MYOUT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-G.
          05 WS-X PIC X(4) VALUE "ABCD".
       PROCEDURE DIVISION.
       MP.
           DISPLAY WS-X UPON MYOUT (1).
           STOP RUN.
