      *> FUNCTION ALL INTRINSIC NAMES THE TARGETED EDITION'S FUNCTIONS
      *> (kb/Work PB1083). ISO/IEC 1989:2023 Annex E.2 item 13: "If
      *> FUNCTION ALL INTRINSIC is specified in the REPOSITORY
      *> paragraph, the following new intrinsic function names are
      *> prohibited within the scope of that REPOSITORY paragraph as
      *> user-defined words" - BASECONVERT, CONCAT, CONVERT,
      *> FIND-STRING, MODULE-NAME, SMALLEST-ALGEBRAIC, SUBSTITUTE. They
      *> are COBOL-2023 functions, so under COBOL-2014 §12.3.8.3 SR13
      *> does not reach them and they are ordinary data-names. It used
      *> to refuse all three (COBOLNET1649) at --std 2014; at 2023 they
      *> are refused (negative/pb1083-all-intrinsic-2023-names).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1083ED.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION ALL INTRINSIC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CONCAT PIC 9 VALUE 1.
       01 FIND-STRING PIC 9 VALUE 2.
       01 BASECONVERT PIC 9 VALUE 3.
       PROCEDURE DIVISION.
           DISPLAY CONCAT FIND-STRING BASECONVERT
           STOP RUN.
