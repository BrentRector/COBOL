*>--- conformance-case
*> id: pb1030-reference-to-refused-redefines
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1030 - ISO 13.18.44.3 SR5: "The data description entry for data-name-2 shall not
      *> contain an OCCURS clause", so OV's description is refused (COBOLNET1701). The statement that
      *> references OV says so instead of announcing a COBOL.NET gap at run time.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1030E.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET AL IS NATIVE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TB.
          05 E PIC X OCCURS 5.
       01 X PIC X(4) VALUE "ABCD".
       01 OD.
          05 OT PIC X(3) OCCURS 2.
          05 OV REDEFINES OT PIC X(6).
       PROCEDURE DIVISION.
           DISPLAY OV.
           STOP RUN.
