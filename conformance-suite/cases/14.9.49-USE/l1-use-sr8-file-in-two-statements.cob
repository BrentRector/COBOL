*>--- conformance-case
*> id: l1-use-sr8-file-in-two-statements
*> rules: [SR-14.9.49.3-8]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.49.3 SR8 - "The same file-name shall not appear in more than one USE AFTER
      *> EXCEPTION statement within the same procedure division." Two Format-1 USE statements in
      *> ONE procedure division each name F1, so the file-name appears in more than one such
      *> statement and the source shall be rejected. SR8 is a FORMAT 1 rule and Format 1 exists
      *> at every edition, so all four reject.
      *> (The accepting complement - ONE statement naming the same file twice - is
      *> tests/conformance/85/l1_use_sr8_file_named_twice.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1USE8N.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "l1use8n-1.dat"
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
       ONE-SECT SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F1.
       ONE-PARA.
           CONTINUE.
       TWO-SECT SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F1.
       TWO-PARA.
           CONTINUE.
       END DECLARATIVES.
       MAIN-SECT SECTION.
       MAIN.
           OPEN INPUT F1
           DISPLAY "ST1=" ST1
           STOP RUN.
