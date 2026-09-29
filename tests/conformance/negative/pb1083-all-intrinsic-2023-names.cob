      *> reject-at: 2023
      *> ISO/IEC 1989:2023 §12.3.8.3 SR13 with Annex E.2 item 13 (kb/Work
      *> PB1083): under FUNCTION ALL INTRINSIC the COBOL-2023 function
      *> names CONCAT, FIND-STRING and BASECONVERT "are prohibited within
      *> the scope of that REPOSITORY paragraph as user-defined words".
      *> At COBOL-2014 the same program compiles
      *> (2014/pb1083_all_intrinsic_edition_names). COBOLNET1649.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1083E3.
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
