*>--- conformance-case
*> id: pb134_delete_file_format2
*> rules: [GR-14.9.10.4-12, GR-14.9.10.4-18, FMT-14.9.10.2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB134 - ISO 14.9.10.2 Format 2 whole: [OVERRIDE] {file-name-1}... with the OPTIONAL word
      *> ON absent from the exception phrase (5.2.3). GR12: two names execute as if one statement per name,
      *> in order; GR14: an absent file deletes successfully (status '05'), so no exception fires. GR18/19:
      *> OVERRIDE skips the fixed-file-attribute match - this implementation validates none (documented),
      *> so accepting the word is the obligation. The old grammar bound OVERRIDE as the file-name.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DF1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "df1a.dat".
           SELECT F2 ASSIGN TO "df1b.dat".
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1 PIC X(10).
       FD F2.
       01 R2 PIC X(10).
       PROCEDURE DIVISION.
       MAIN.
           DELETE FILE OVERRIDE F1 F2
               EXCEPTION DISPLAY "DEL-EXC"
           END-DELETE
           DISPLAY "DF-OK"
           STOP RUN.
