*> reject-at: 2002 2014 2023
*> ISO 1989:2023 §12.4.5.2 syntax rule 7, first half: "Data-name-1 shall reference an alphanumeric data item
*> and shall not be subordinate to the file description entry for file-name-1." §9.1.21 states the same
*> requirement in the concepts - "The USING phrase references an alphanumeric data item whose content at the
*> time an OPEN, SORT, or MERGE statement for that file is executed uniquely identifies the specific physical
*> file to be accessed". WS-NUM is a category-NUMERIC item, so it cannot be data-name-1: COBOLNET1810 at every
*> edition that has the USING phrase (2002 on, kb/Work PB746), since the rule is the clause's own (kb/Work PB324).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB324N1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT NF1 ASSIGN USING WS-NUM
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  NF1.
       01  N1-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01  WS-NUM PIC 9(5) VALUE 12345.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT NF1.
           CLOSE NF1.
           STOP RUN.
