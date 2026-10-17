*>--- conformance-case
*> id: l1-use-input-phrase-twice
*> rules: [SR-14.9.49.3-7]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.49.3 SR7 — "The INPUT, OUTPUT, I-O, and EXTEND phrases may each be specified
      *> only once in the declaratives portion of a given procedure division." Two Format 1 USE
      *> statements in ONE declaratives portion each specify the INPUT phrase, so the INPUT
      *> phrase is specified twice and the source shall be rejected. SR7 is a FORMAT 1 rule and
      *> Format 1 exists at every edition, so all four reject.
      *> (The accepting complement — all four phrases, each written exactly once — is
      *> tests/conformance/2023/l1_use_four_mode_phrases.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1USE7N.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "l1use7n-1.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST1.
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1 PIC X(8).
       WORKING-STORAGE SECTION.
       01 ST1 PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       INPUT-ONE-SECT SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON INPUT.
       INPUT-ONE-PARA.
           CONTINUE.
       INPUT-TWO-SECT SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON INPUT.
       INPUT-TWO-PARA.
           CONTINUE.
       END DECLARATIVES.
       MAIN-SECT SECTION.
       MAIN.
           OPEN INPUT F1
           DISPLAY "ST1=" ST1
           STOP RUN.
