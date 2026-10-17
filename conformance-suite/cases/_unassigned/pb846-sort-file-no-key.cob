*>--- conformance-case
*> id: pb846-sort-file-no-key
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO/IEC 1989:2023 §14.9.40.2 Format 1 - the file SORT's KEY phrase is printed in BRACES with an
      *> ellipsis, so at least one is required; only Format 2 brackets it (§14.9.40.3 SR15). kb/Work PB846
      *> relaxed the grammar's key-phrase list to zero-or-more for the table format, so the Format-1 arity is
      *> now screened in the binder - this case pins that the relaxation did not leak into the file format.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB846N2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "PB846SF".
           SELECT IF1 ASSIGN TO "PB846IF".
           SELECT OF1 ASSIGN TO "PB846OF".
       DATA DIVISION.
       FILE SECTION.
       SD SF.
       01 SR PIC X(4).
       FD IF1.
       01 IR PIC X(4).
       FD OF1.
       01 OR1 PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           SORT SF USING IF1 GIVING OF1
           STOP RUN.
