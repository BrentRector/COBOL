*>--- conformance-case
*> id: pb829-identification-header-omitted-85
*> rules: [FMT-11.2.1]
*> source-format: free
*> portable: unverified
*>---
       PROGRAM-ID. PB829IX.
      *> kb/Work PB829 - X3.23-1985 REQUIRED the IDENTIFICATION DIVISION header; ISO 2002 made it
      *> optional (2023 11.2.1 prints `[ IDENTIFICATION DIVISION. ]` in brackets).  Below 2002 the
      *> header-less unit is refused by the named relaxation gate identification-header-optional-2002
      *> (COBOLNET0900), not by a raw `unexpected 'PROGRAM-ID'` parse error.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "PB829IX".
           STOP RUN.
