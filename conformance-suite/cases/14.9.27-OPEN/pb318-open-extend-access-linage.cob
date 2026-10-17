*>--- conformance-case
*> id: pb318-open-extend-access-linage
*> rules: [SR-14.9.27.3-2]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 14.9.27.3 SR2 - "The EXTEND phrase shall be specified only if the access mode of the
*> file connector referenced by file-name-1 is sequential and the LINAGE clause is not specified in
*> the file description entry for file-name-1." One sentence, two conjuncts: FR (ACCESS MODE IS
*> RANDOM) violates the first and FL (a LINAGE clause) the second. Both compiled clean until
*> kb/Work PB318; the EXTEND on the LINAGE file ran against an unmodelled page state.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB318EXL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FR ASSIGN TO "pb318exr.dat"
               ORGANIZATION IS RELATIVE ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK.
           SELECT FL ASSIGN TO "pb318exl.dat".
       DATA DIVISION.
       FILE SECTION.
       FD FR.
       01 FR-REC PIC X(4).
       FD FL LINAGE IS 5 LINES.
       01 FL-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 RK PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           OPEN EXTEND FR
           CLOSE FR
           OPEN EXTEND FL
           CLOSE FL
           STOP RUN.
