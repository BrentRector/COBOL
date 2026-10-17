*>--- conformance-case
*> id: l1c29-sort-collating-slot2-alphanumeric
*> rules: [SR-14.9.40.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.40.3 SR2 — alphabet-name-2 names an ALPHANUMERIC alphabet
      *> Rule: "Alphabet-name-2 shall reference an alphabet that defines a
      *> national collating sequence."
      *> cite.py --check 14.9.40.3 "Alphabet-name-2 shall reference an
      *>   alphabet that defines a national collating sequence"
      *>   -> OK  §14.9.40.3 2)  (Syntax rules)
      *> A-REV has no FOR NATIONAL phrase, so by §12.3.7.4 GR7 a) its
      *> sequence is alphanumeric; as the SECOND word after IS it is
      *> alphabet-name-2, violating SR2. alphabet-name-1 (A-AN) is legal.
      *> alphabet-name-2 exists from 2002 (pb678-sort-collating-for-forms-at-85
      *> pins its 85 gate), so reject-at starts at 2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C29E.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET A-AN IS STANDARD-1
           ALPHABET A-REV IS "ZYX".
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT UF ASSIGN TO "L1C29EI.DAT".
           SELECT GF ASSIGN TO "L1C29EO.DAT".
           SELECT SF ASSIGN TO "L1C29ES.TMP".
       DATA DIVISION.
       FILE SECTION.
       FD  UF.
       01  UF-REC PIC X(4).
       FD  GF.
       01  GF-REC PIC X(4).
       SD  SF.
       01  SR.
           05 SK PIC X(1).
           05 SV PIC X(3).
       PROCEDURE DIVISION.
       MAIN-PARA.
           SORT SF ON ASCENDING KEY SK
               COLLATING SEQUENCE IS A-AN A-REV
               USING UF
               GIVING GF.
           STOP RUN.
