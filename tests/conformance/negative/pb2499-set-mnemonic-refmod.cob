      *> reject-at: 85 2002 2014 2023
      *> ISO 1989:2023 §14.9.39.2 Format 3: SET { mnemonic-name-1 } ... TO { ON | OFF }. A mnemonic-name "identifies an implementor-defined
      *> device-name, feature-name, or switch-name" (§8.3.2.2.16): a word with no data description, so it
      *> takes no subscript or reference modification, and no qualified format of §8.4.2.2.2 names one.
      *> MYSW written with a reference modification is no SET statement at any edition. COBOLNET2269 (kb/Work PB2499).
      *> The grammar spelled each receiver as a dataReference and the binder read only the word, so the
      *> suffix compiled clean and vanished; each receiver is now a word and the suffix a named syntax error.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2499SR.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SWITCH-1 IS MYSW ON STATUS IS SW-ON.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-G.
          05 WS-X PIC X(4) VALUE "ABCD".
       PROCEDURE DIVISION.
       MP.
           SET MYSW (1:1) TO ON.
           STOP RUN.
