*>--- conformance-case
*> id: pb829_identification_header_optional
*> rules: [FMT-11.2.1]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
       PROGRAM-ID. PB829IH.
      *> kb/Work PB829 - ISO 11.2.1 prints the identification division header in BRACKETS,
      *>   [ IDENTIFICATION DIVISION. ]
      *> (rendered, PDF p293 / folio 263), so a program source unit may open directly on its
      *> PROGRAM-ID paragraph - this one, and the NESTED program below, which also omits it.
      *> Both units were `COBOL0001: unexpected 'PROGRAM-ID'` at every edition.  X3.23-1985
      *> required the header, so the header-less form is a 2002 relaxation
      *> (identification-header-optional-2002; the 85 negative is its twin).
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 MSG PIC X(5) VALUE "OUTER".
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL "PB829IN".
           DISPLAY "PB829IH " MSG.
           STOP RUN.
       PROGRAM-ID. PB829IN.
       PROCEDURE DIVISION.
       INNER-PARA.
           DISPLAY "PB829IN INNER".
           GOBACK.
       END PROGRAM PB829IN.
       END PROGRAM PB829IH.
