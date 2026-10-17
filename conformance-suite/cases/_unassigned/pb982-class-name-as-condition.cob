*>--- conformance-case
*> id: pb982-class-name-as-condition
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB982 - a class-name forms a class condition only after the identifier it tests: ISO
      *> 8.8.4.4.2 "identifier-1 IS [NOT] class-name-1". The identifier-less form is an EVALUATE selection
      *> object only (14.9.13.3 SR5), never an IF condition. IF MY-DIG used to compile clean and abort the
      *> run unit. Expected: COBOLNET2318 at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB982NCN.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CLASS MY-DIG IS "0" THRU "9".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X PIC X VALUE "5".
       PROCEDURE DIVISION.
           IF MY-DIG
               DISPLAY "NEVER"
           END-IF
           STOP RUN.
