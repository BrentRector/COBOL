*> reject-at: 2002 2014 2023
*> ISO 1989:2023 §12.4.5.2 syntax rule 7, second half: data-name-1 "shall not be subordinate to the file
*> description entry for file-name-1." N2-NAME lives inside NF2's own record area, so every READ of NF2 would
*> overwrite the name the next OPEN of NF2 reads - the operand that selects the physical file would be file
*> content. COBOLNET1811 at every edition that has the USING phrase (2002 on, kb/Work PB746; kb/Work PB324).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB324N2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT NF2 ASSIGN USING N2-NAME
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  NF2.
       01  N2-REC.
           05  N2-NAME PIC X(10).
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT NF2.
           CLOSE NF2.
           STOP RUN.
