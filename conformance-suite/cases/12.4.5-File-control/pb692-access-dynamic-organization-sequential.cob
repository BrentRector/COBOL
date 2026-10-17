*>--- conformance-case
*> id: pb692-access-dynamic-organization-sequential
*> rules: [SR-12.4.5.5.2-2]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 12.4.5.5.2 syntax rule 2 names TWO phrases -- "The DYNAMIC and RANDOM phrases
*> shall not be specified for a sequential file." This is the DYNAMIC half on the same
*> explicit ORGANIZATION IS SEQUENTIAL entry: one rule, two spellings, and a screen that
*> caught only the phrase its repro happened to write would be the two-arm dispatch again.
*> COBOLNET1858 at every edition (kb/Work PB692).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB692N2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb692n2.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS DYNAMIC.
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  F-REC PIC X(6).
       PROCEDURE DIVISION.
       MAIN.
           OPEN INPUT F.
           CLOSE F.
           STOP RUN.
