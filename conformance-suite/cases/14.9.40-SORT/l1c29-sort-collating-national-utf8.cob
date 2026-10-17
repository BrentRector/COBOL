*>--- conformance-case
*> id: l1c29-sort-collating-national-utf8
*> rules: [SR-14.9.40.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.40.3 SR2 — alphabet-name-2 is a UTF-8 alphabet (no collating sequence)
      *> Rule: "Alphabet-name-2 shall reference an alphabet that defines a
      *> national collating sequence."
      *> cite.py --check 14.9.40.3 "Alphabet-name-2 shall reference an
      *>   alphabet that defines a national collating sequence"
      *>   -> OK  §14.9.40.3 2)  (Syntax rules)
      *> §12.3.7.4 GR7 Table 6 (cite.py --check 12.3.7.4 "indicates for
      *> each operand of the ALPHABET clause whether the alphabet-name
      *> references a coded character set, a collating sequence, or both"
      *> -> OK  §12.3.7.4 7)): UTF-8 has NO Y in the collating-sequence
      *> column, so N-U8 defines no collating sequence at all -> SR2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C29F.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET N-U8 FOR NATIONAL IS UTF-8.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT UF ASSIGN TO "L1C29FI.DAT".
           SELECT GF ASSIGN TO "L1C29FO.DAT".
           SELECT SF ASSIGN TO "L1C29FS.TMP".
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
               COLLATING SEQUENCE FOR NATIONAL IS N-U8
               USING UF
               GIVING GF.
           STOP RUN.
