*>--- conformance-case
*> id: pb110-code-set-nonidentity
*> rules: [SR-13.18.13.3-3, GR-13.18.13.4-1, FMT-13.18.13.2]
*> source-format: free
*> portable: unverified
*>---
      *> Annex A.3 item 27: "The CODE-SET clause is dependent upon a device capable of supporting the specified
      *> code." A literal-phrase alphabet's coded character set has remapped ordinals - an on-medium representation
      *> that differs from the native encoding - and this processor documents non-support of alternate device code
      *> sets (CONFORMANCE.md section 2 row 27): COBOLNET1672, never a silent identity (kb/Work PB110). The identity
      *> sets (NATIVE, STANDARD-1/2, UTF-16) are claimed - golden 2023/pb110_code_set_identity.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB110CN.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET REV IS "Z" THRU "A".
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "f1.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  F1 CODE-SET IS REV.
       01  R1 PIC X(4).
       PROCEDURE DIVISION.
           STOP RUN.
