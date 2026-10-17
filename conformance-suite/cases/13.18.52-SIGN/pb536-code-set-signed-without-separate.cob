*>--- conformance-case
*> id: pb536-code-set-signed-without-separate
*> rules: [SR-13.18.52.3-3]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.13.3 SR3 a): "if alphabet-name-1 is specified, all elementary data items of all record
      *> description entries associated with the file shall be described as usage display, and any signed
      *> numeric data items shall be described with the SIGN IS SEPARATE clause"; 13.18.52.3 SR3 says the
      *> same from the SIGN clause's side. N is a signed numeric record item of a CODE-SET file and NO SIGN
      *> clause applies to it - neither its own nor one inherited from a containing group (13.18.52.4 GR1) -
      *> so the record description violates the rule: COBOLNET1672.
      *> The LEGAL inherited spelling, 05 G SIGN IS LEADING SEPARATE. 10 N PIC S9(4)., is the positive
      *> golden conformance:85/pb536_code_set_group_sign - the shape the screen used to refuse by running
      *> one phase too early (kb/Work PB536). No edition gate: the clause, the rule and the diagnostic are
      *> the same at every edition CODE-SET exists in.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB536NS.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET AL1 IS STANDARD-1.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb536ns.dat"
           ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  F CODE-SET IS AL1.
       01  R.
           05  G.
               10  N PIC S9(4).
       WORKING-STORAGE SECTION.
       01  DONE PIC X VALUE "N".
       PROCEDURE DIVISION.
           DISPLAY "UNREACHABLE" DONE
           STOP RUN.
