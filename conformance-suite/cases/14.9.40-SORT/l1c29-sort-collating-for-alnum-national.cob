*>--- conformance-case
*> id: l1c29-sort-collating-for-alnum-national
*> rules: [SR-14.9.40.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.40.3 SR1 — FOR ALPHANUMERIC IS names a FOR NATIONAL alphabet
      *> Rule: "Alphabet-name-1 shall reference an alphabet that defines an
      *> alphanumeric collating sequence."
      *> cite.py --check 14.9.40.3 "Alphabet-name-1 shall reference an
      *>   alphabet that defines an alphanumeric collating sequence"
      *>   -> OK  §14.9.40.3 1)  (Syntax rules)
      *> §12.3.7.4 GR7 b) -> OK  §12.3.7.4 7): a FOR NATIONAL alphabet's
      *> collating sequence is national. The FOR ALPHANUMERIC operand is
      *> alphabet-name-1 (§14.9.40.2 Format 1), so SR1 is violated.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C29D.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET N-NAT FOR NATIONAL IS NATIVE.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT UF ASSIGN TO "L1C29DI.DAT".
           SELECT GF ASSIGN TO "L1C29DO.DAT".
           SELECT SF ASSIGN TO "L1C29DS.TMP".
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
               COLLATING SEQUENCE FOR ALPHANUMERIC IS N-NAT
               USING UF
               GIVING GF.
           STOP RUN.
