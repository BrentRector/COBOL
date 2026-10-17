*>--- conformance-case
*> id: l1-use-sr14-pair-in-two-statements
*> rules: [SR-14.9.49.3-14]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.49.3 SR14 - "The same pair of exception-name-2 and file-name-2 shall not be
      *> specified in more than one USE statement within the same procedure division." Two
      *> Format-3 USE statements in ONE procedure division each specify the pair
      *> (EC-I-O-AT-END, F1), so the pair is specified in more than one USE statement and the
      *> source shall be rejected. Format 3 arrives with the exception-condition model, so the
      *> rule exists at 2002, 2014 and 2023 and not at 85.
      *> (The accepting complements - the same pair twice inside ONE statement, and a repeated
      *> BARE exception-name-1, which is exception-name-1 and outside this rule - are
      *> tests/conformance/2002/l1_use_sr14_pair_twice_one_statement and
      *> tests/conformance/2002/l1_use_sr14_bare_name_repeated.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1S14N.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "l1s14n-1.dat"
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
           USE AFTER EXCEPTION CONDITION EC-I-O-AT-END FILE F1.
       ONE-PARA.
           CONTINUE.
       TWO-SECT SECTION.
           USE AFTER EXCEPTION CONDITION EC-I-O-AT-END FILE F1.
       TWO-PARA.
           CONTINUE.
       END DECLARATIVES.
       MAIN-SECT SECTION.
       MAIN.
           DISPLAY "ST1=" ST1
           STOP RUN.
