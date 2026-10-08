      *> reject-at: 85 2002 2014 2023
      *> ISO 1989:2023 §14.9.51.2 Format 1: ... ADVANCING { mnemonic-name-1 | PAGE }. A mnemonic-name "identifies an implementor-defined
      *> device-name, feature-name, or switch-name" (§8.3.2.2.16): a word with no data description, so it
      *> takes no subscript or reference modification, and no qualified format of §8.4.2.2.2 names one.
      *> MYTOP written with a qualifier is no WRITE statement at any edition. COBOLNET2269 (kb/Work PB2499).
      *> The slot shares identifier-2's dataReference, so the binder refuses the suffix by name; it used
      *> to miss the mnemonic and report the declared name as 'not defined' (COBOLNET1639).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2499WQ.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           C01 IS MYTOP.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRTF ASSIGN TO "PB2499WQ.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD PRTF.
       01 PR PIC X(4).
       WORKING-STORAGE SECTION.
       01 WS-G.
          05 WS-X PIC X(4) VALUE "ABCD".
       PROCEDURE DIVISION.
       MP.
           OPEN OUTPUT PRTF.
           WRITE PR FROM WS-X AFTER ADVANCING MYTOP OF WS-G.
           CLOSE PRTF.
           STOP RUN.
