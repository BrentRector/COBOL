*>--- conformance-case
*> id: pb488-cond-var-any-length
*> rules: [SR-13.16.3-24]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 13.16.3 syntax rule 24 - "... f) A data item described with the ANY LENGTH clause."
*>
*> An ANY LENGTH formal has no length until it is bound to an argument (13.18.2.4), so a condition-name
*> over it has no fixed-size subject to compare - which is why the rule excludes it rather than leaving the
*> comparison implementor-defined. kb/Work PB488 measured the contained-program spelling compiling and
*> evaluating. ANY LENGTH is a COBOL-2002 addition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB488-COND-VAR-ANY-LENGTH.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ARG PIC X(3) VALUE "AAA".
       PROCEDURE DIVISION.
           CALL "PB488-COND-VAR-ANY-LENGTH-SUB" USING ARG
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB488-COND-VAR-ANY-LENGTH-SUB.
       DATA DIVISION.
       LINKAGE SECTION.
       01 AL PIC X ANY LENGTH.
       88 AL-C VALUE "AAA".
       PROCEDURE DIVISION USING AL.
           IF AL-C DISPLAY "T" ELSE DISPLAY "F" END-IF
           GOBACK.
       END PROGRAM PB488-COND-VAR-ANY-LENGTH-SUB.
       END PROGRAM PB488-COND-VAR-ANY-LENGTH.
