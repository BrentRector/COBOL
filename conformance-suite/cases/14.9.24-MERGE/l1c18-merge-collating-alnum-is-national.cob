      *>--- conformance-case
      *> id: l1c18-merge-collating-alnum-is-national
      *> rules: [SR-14.9.24.3-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.24.3 SR5 — MERGE COLLATING SEQUENCE IS NN where
      *> alphabet-name-1 NN is a FOR NATIONAL alphabet.
      *> Rule: "Alphabet-name-1 shall reference an alphabet that
      *> defines an alphanumeric collating sequence."
      *> cite.py: OK  §14.9.24.3 5)  (Syntax rules)
      *> NN is declared (ALPHABET NN FOR NATIONAL IS NATIVE, a national
      *> collating sequence), so the name resolves; it is the wrong
      *> class for the alphabet-name-1 position.  A FOR NATIONAL
      *> alphabet exists only from COBOL 2002, hence 2002 and later.
      *> Diagnostic COBOLNET0898 with the alphabet-name-1 message head
      *> (the message's trailing clause reference is not asserted).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C18D.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET NN FOR NATIONAL IS NATIVE.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "l1c18d1.dat".
           SELECT F2 ASSIGN TO "l1c18d2.dat".
           SELECT FO ASSIGN TO "l1c18do.dat".
           SELECT SM ASSIGN TO "l1c18ds.tmp".
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  F1-REC        PIC X(5).
       FD  F2.
       01  F2-REC        PIC X(5).
       FD  FO.
       01  FO-REC        PIC X(5).
       SD  SM.
       01  SM-REC        PIC X(5).
       PROCEDURE DIVISION.
       MAIN-P.
           MERGE SM ON ASCENDING KEY SM-REC
               COLLATING SEQUENCE IS NN
               USING F1 F2 GIVING FO.
           STOP RUN.
