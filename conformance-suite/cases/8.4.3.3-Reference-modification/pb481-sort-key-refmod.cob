*>--- conformance-case
*> id: pb481-sort-key-refmod
*> rules: [SR-8.4.3.3.3-5]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB481 - ISO 14.9.40.2 Format 1 prints KEY { data-name-1 } ..., and 8.4.3.3.3's NOTE forbids a
*> reference-modifier where a general format prints data-name-n. The key used to reach the reference resolver,
*> which answered with the base item S-KEY and dropped the (1:3): the file was sorted on all six characters, a
*> silent wrong answer. MERGE (14.9.24.2) prints the same phrase and takes the same screen.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB481N2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF  ASSIGN TO "pb481n2.tmp".
           SELECT IF1 ASSIGN TO "pb481n2.in".
           SELECT OF1 ASSIGN TO "pb481n2.out".
       DATA DIVISION.
       FILE SECTION.
       SD SF.
       01 S-REC.
          05 S-KEY PIC X(6).
       FD IF1.
       01 I-REC PIC X(6).
       FD OF1.
       01 O-REC PIC X(6).
       PROCEDURE DIVISION.
       MAIN-PARA.
           SORT SF ON ASCENDING KEY S-KEY(1:3)
               USING IF1 GIVING OF1.
           STOP RUN.
