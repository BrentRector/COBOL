      *>--- conformance-case
      *> id: pb369_global_file_use_selection
      *> rules: [SR-13.18.27.3-1, GR-14.9.49.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.49.4 GR4 (FORMATS 1 AND 2), the Format-1 half: for an
      *> I-O exception raised by a statement in a contained program the
      *> declarative selected is a) the qualifying declarative in the
      *> source element that contains the statement, else b) a qualifying
      *> declarative with the GLOBAL attribute in the next inclusive
      *> directly containing source element, repeated outward (kb/Work
      *> PB369; the Format-2 half is pb369_global_report_use_selection).
      *> GF is a GLOBAL file (§13.18.27.3 SR1 d)) whose OPEN INPUT fails:
      *> the file does not exist and is not OPTIONAL, status 35.
      *>   PB369GFA: USE GLOBAL AFTER ERROR ON GF   -> "OUTER-GLOBAL"
      *>   PB369GFB (in A): its OWN USE AFTER ERROR ON GF (the inherited
      *>     global file-name, §13.18.27.4 GR2)     -> "B-OWN"
      *>   PB369GFC (in B): no declaratives.
      *> B's OPEN -> a) B's own beats A's GLOBAL one:      "B-OWN 35"
      *> C's OPEN -> B's is not GLOBAL, so b) goes to A:   "OUTER-GLOBAL 35"
      *> A's OPEN -> a) A's own:                           "OUTER-GLOBAL 35"
      *> EDITION: nested programs, GLOBAL and USE are COBOL-85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB369GFA.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT GF ASSIGN TO "pb369gf-does-not-exist.dat"
               FILE STATUS IS GF-ST.
       DATA DIVISION.
       FILE SECTION.
       FD GF GLOBAL.
       01 GF-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01 GF-ST PIC XX VALUE "00" GLOBAL.
       PROCEDURE DIVISION.
       DECLARATIVES.
       A-D1 SECTION.
           USE GLOBAL AFTER STANDARD ERROR PROCEDURE ON GF.
       A-D1-P.
           DISPLAY "OUTER-GLOBAL " GF-ST.
       END DECLARATIVES.
       A-MAIN SECTION.
       A-1.
           CALL "PB369GFB".
           OPEN INPUT GF.
           DISPLAY "A DONE".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB369GFB.
       PROCEDURE DIVISION.
       DECLARATIVES.
       B-D1 SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON GF.
       B-D1-P.
           DISPLAY "B-OWN " GF-ST.
       END DECLARATIVES.
       B-MAIN SECTION.
       B-1.
           OPEN INPUT GF.
           CALL "PB369GFC".
           EXIT PROGRAM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB369GFC.
       PROCEDURE DIVISION.
       C-1.
           OPEN INPUT GF.
           EXIT PROGRAM.
       END PROGRAM PB369GFC.
       END PROGRAM PB369GFB.
       END PROGRAM PB369GFA.
