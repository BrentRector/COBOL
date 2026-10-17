*>--- conformance-case
*> id: pb848-function-pointer-to-less-below-2014
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO/IEC 1989:2023 §13.18.60.2 - USAGE FUNCTION-POINTER is a COBOL-2014 usage. kb/Work PB848 made TO an
      *> optional word in the three pointer usages (not underlined on the printed folio 503, §5.2.3), so
      *> `USAGE FUNCTION-POINTER PBF848N` is conforming at 2014+; this pins that the TO-less spelling does not
      *> leak below the edition that introduced the usage - the introduction gate still names it.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PBF848N.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-RES PIC S9(9).
       PROCEDURE DIVISION RETURNING L-RES.
           MOVE 0 TO L-RES
           GOBACK.
       END FUNCTION PBF848N.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB848N1.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION PBF848N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FP USAGE FUNCTION-POINTER PBF848N.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
       END PROGRAM PB848N1.
