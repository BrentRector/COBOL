*>--- conformance-case
*> id: pb124-convert-type-argument
*> rules: [AR-15.3-14]
*> source-format: free
*> portable: unverified
*>---
      *> Table 21 lists Type1 for CONVERT, but 15.19.3's argument rules and 15.19.4's returned-value rules
      *> define NOTHING for a type-name (a type declaration has neither storage nor a value for any
      *> source-format to read) - a standard-text inconsistency recorded in docs/CONFORMANCE.md. COBOL.NET
      *> rejects with the reason named, not "undefined data-name" (kb/Work PB124 wave 4, AR-15.3-14).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB124NG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TFIX TYPEDEF.
          05 TA PIC X(3).
       01 TVLG TYPEDEF.
          05 TN PIC 9.
          05 TE PIC X OCCURS 1 TO 5 TIMES DEPENDING ON TN.
       01 N PIC 9 VALUE 3.
       01 VLG.
          05 FIXED-PART PIC X(2).
          05 E PIC X OCCURS 1 TO 5 TIMES DEPENDING ON N.
       01 R PIC 9(4).
       01 RS PIC X(8).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION CONVERT(TFIX ANY ANUM HEX) TO RS
           STOP RUN.
