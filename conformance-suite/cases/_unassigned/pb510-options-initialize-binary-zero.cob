*>--- conformance-case
*> id: pb510-options-initialize-binary-zero
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO 11.9.10.2 prints the INITIALIZE clause's fills as the KEYWORDS "BINARY ZEROES", "HIGH-VALUES",
      *> "LOW-VALUES" and "SPACES" (all underlined, 5.2.2) - each in its ONE plural spelling - or
      *> literal-1, a one-byte hexadecimal-alphanumeric literal (11.9.10.3 SR1). "BINARY ZERO" is
      *> neither. kb/Work PB510: the lexer folded ZERO/ZEROS/ZEROES into one token, so this compiled and
      *> zero-filled storage. Refused COBOLNET2418 at every edition: the misspelled keyword fails the
      *> parse before the OPTIONS paragraph's 2023 introduction is gated at bind.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB510N3.
       OPTIONS.
           INITIALIZE ALL TO BINARY ZERO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W1 PIC X(2).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "[" W1 "]"
           STOP RUN.
