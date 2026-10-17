*>--- conformance-case
*> id: pb110_code_set_identity
*> rules: [GR-12.3.7.4-7, SR-13.18.13.3-1, SR-13.18.13.3-3,
*>    GR-13.18.13.4-1, GR-13.18.13.4-2, GR-13.18.13.4-7, FMT-13.18.13.2,
*>    DOC-A.1-187]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.13 CODE-SET (kb/Work PB110 - the clause parsed as the '85 one-name form and was read by NOTHING;
      *> the 2002 FOR format was a parse error). GR2: on OPEN the on-medium coded character set is the one referenced
      *> by alphabet-name-1; GR7 c (12.3.7.4) makes STANDARD-1's correspondence to the native set the IDENTITY on the
      *> ISO 646 characters, so the conversion is the identity and the round trip is byte-exact - the CLAIMED case
      *> (CONFORMANCE.md section 2 row 27; an alphabet whose on-medium representation would differ is the documented
      *> A.3 item 27 non-support, COBOLNET1672 - negative pb110-code-set-nonidentity). SR1 is checked (a FOR NATIONAL
      *> alphabet here is 1672; a LOCALE alphabet is 1669 - negative pb110-code-set-locale-alphabet).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB110CS.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET STD IS STANDARD-1.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb110cs.dat"
           ORGANIZATION IS LINE SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  F1 CODE-SET IS STD.
       01  R1              PIC X(6).
       WORKING-STORAGE SECTION.
       01  DONE            PIC X VALUE "N".
       PROCEDURE DIVISION.
           OPEN OUTPUT F1
           MOVE "HELLO!" TO R1
           WRITE R1
           CLOSE F1
           OPEN INPUT F1
           READ F1 AT END MOVE "Y" TO DONE END-READ
           DISPLAY "R1=[" R1 "]"
           DISPLAY "DONE=" DONE
           CLOSE F1
           STOP RUN.
