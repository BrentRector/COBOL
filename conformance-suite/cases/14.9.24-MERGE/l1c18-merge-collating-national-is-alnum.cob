      *>--- conformance-case
      *> id: l1c18-merge-collating-national-is-alnum
      *> rules: [SR-14.9.24.3-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.24.3 SR6 — MERGE COLLATING SEQUENCE IS AA AB where
      *> alphabet-name-2 AB is an ALPHANUMERIC alphabet.
      *> Rule: "Alphabet-name-2 shall reference an alphabet that
      *> defines a national collating sequence."
      *> cite.py: OK  §14.9.24.3 6)  (Syntax rules)
      *> AA (alphabet-name-1) is an alphanumeric alphabet, so SR5 holds;
      *> AB is declared but alphanumeric, the wrong class for the
      *> alphabet-name-2 position.  alphabet-name-2 exists only from
      *> COBOL 2002 (below that the form itself is refused for another
      *> reason), hence 2002 and later.  Diagnostic COBOLNET0898 with
      *> the alphabet-name-2 message head.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C18E.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET AA IS NATIVE
           ALPHABET AB IS STANDARD-1.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "l1c18e1.dat".
           SELECT F2 ASSIGN TO "l1c18e2.dat".
           SELECT FO ASSIGN TO "l1c18eo.dat".
           SELECT SM ASSIGN TO "l1c18es.tmp".
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
               COLLATING SEQUENCE IS AA AB
               USING F1 F2 GIVING FO.
           STOP RUN.
