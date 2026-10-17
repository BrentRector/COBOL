*>--- conformance-case
*> id: l1c29-sort-collating-slot1-national
*> rules: [SR-14.9.40.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.40.3 SR1 — alphabet-name-1 names a FOR NATIONAL alphabet (IS form)
      *> Rule: "Alphabet-name-1 shall reference an alphabet that defines an
      *> alphanumeric collating sequence."
      *> cite.py --check 14.9.40.3 "Alphabet-name-1 shall reference an
      *>   alphabet that defines an alphanumeric collating sequence"
      *>   -> OK  §14.9.40.3 1)  (Syntax rules)
      *> §12.3.7.4 GR7 b): "When the NATIONAL phrase is specified, ... a
      *> collating sequence referenced by alphabet-name-2 is a national
      *> collating sequence." -> OK  §12.3.7.4 7)
      *> N-NAT is FOR NATIONAL IS NATIVE, so its sequence is NATIONAL; the
      *> single word after IS is alphabet-name-1, so SR1 is violated. Every
      *> other element is legal (A-AN is declared but unused). National
      *> alphabets are 2002+, so the rule has no violating source at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C29C.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET A-AN IS STANDARD-1
           ALPHABET N-NAT FOR NATIONAL IS NATIVE.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT UF ASSIGN TO "L1C29CI.DAT".
           SELECT GF ASSIGN TO "L1C29CO.DAT".
           SELECT SF ASSIGN TO "L1C29CS.TMP".
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
               COLLATING SEQUENCE IS N-NAT
               USING UF
               GIVING GF.
           STOP RUN.
