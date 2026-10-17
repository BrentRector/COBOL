      *> reject-at: 2002 2014 2023
      *> kb/Work PB780 review R1-1 (sibling) - a constant entry FROM a compilation variable is as if "the
      *> text represented by compilation-variable-name-1 were written" (13.10.4 GR1, cite.py --check 13.10.4
      *> "as if literal-1 or the text represented by compilation-variable-name-1 were written"), and the
      *> DEFINE directive names "a particular literal value" (7.3.11.1), here X"23": 12.3.7.3 SR26 (cite.py
      *> --check 12.3.7.3 "It shall be neither a figurative constant nor a hexadecimal literal") refuses
      *> it. The form check read the specification FROM PB780VC, so the clause compiled and ran. The
      *> constant-name twin is pb780-currency-symbol-constant-chain-hex.
       >>DEFINE PB780VC AS X"23"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGPB780CF.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN IS "EUR" WITH PICTURE SYMBOL KF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 KF CONSTANT FROM PB780VC.
       PROCEDURE DIVISION.
           STOP RUN.
