*>--- conformance-case
*> id: pb780-currency-symbol-constant-chain-hex
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB780 review R1-1 - a constant entry written AS another constant-name (KY AS KX) is as if
      *> KX's literal-1 were written (13.10.4 GR1, cite.py --check 13.10.4 "as if literal-1 or the text
      *> represented by compilation-variable-name-1 were written"), so 12.3.7.3 SR26 (cite.py --check
      *> 12.3.7.3 "It shall be neither a figurative constant nor a hexadecimal literal") is asked of X"23",
      *> the literal KY stands for - never of the word KX, its specification (13.10.3 SR9's subject). The
      *> form check read the specification, so PICTURE SYMBOL KY compiled and ran while PICTURE SYMBOL KX
      *> drew the diagnostic. The FROM twin is pb780-currency-symbol-constant-from-hex.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGPB780CY.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN IS "EUR" WITH PICTURE SYMBOL KY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 KX CONSTANT AS X"23".
       01 KY CONSTANT AS KX.
       PROCEDURE DIVISION.
           STOP RUN.
