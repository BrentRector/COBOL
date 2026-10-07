*> reject-at: 2023
*> ISO 1989:2023 13.16.3 syntax rule 24 h) - "A variable-length group." with 3.11 and 8.5.1.12.1. VG is a variable-length
*> group (it has a dynamic-length elementary item as a subordinate, 8.5.1.12.1), so it is NOT an alphanumeric group item
*> (3.11 excludes a variable-length group item by name) and letter c) "An alphanumeric group containing items with a
*> usage other than display" is not the rule it violates, even though V1 is USAGE BINARY. The .err pins the LETTER,
*> because every lettered exclusion produces COBOLNET1976 and only the quoted sentence differs (kb/Work PB947).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB947-COND-VAR-VLG-COMP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 VG.
          88 VG-A VALUE "XXXXXX".
          05 V1 PIC 9(4) COMP.
          05 V2 PIC X DYNAMIC LENGTH LIMIT IS 5.
       PROCEDURE DIVISION.
           IF VG-A DISPLAY "T" ELSE DISPLAY "F" END-IF
           STOP RUN.
