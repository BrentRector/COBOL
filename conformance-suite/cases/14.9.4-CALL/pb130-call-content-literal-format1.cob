*>--- conformance-case
*> id: pb130-call-content-literal-format1
*> rules: [FMT-14.9.4.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.4.2 Format 1's BY CONTENT prints `{ identifier-2 } ...` and nothing else - a literal
      *> operand belongs to Format 2 (the AS phrase). The old guard rejected expression operands and let
      *> the literal arm through (kb/Work PB130).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB130NG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       PROCEDURE DIVISION.
       MAIN.
           CALL "X" USING BY CONTENT "AB"
           STOP RUN.
