*>--- conformance-case
*> id: pb694-read-no-into-sequential
*> rules: [SR-13.4.5.3-3]
*> source-format: free
*> portable: unverified
*>---
*> ISO/IEC 1989:2023 §13.4.5.3 syntax rule 3 c): "When no record description entries
*> are specified: ... c) an INTO phrase shall be specified on all READ statements
*> associated with the file." FIN has no record description entry, so its record area is
*> §14.9.30.4 GR6's implied entry, which has no record-name: "The execution of a READ
*> statement with the INTO phrase when there are no record description entries
*> subordinate to the file description entry proceeds as though there were one record
*> description entry describing an alphanumeric group item of the maximum size
*> established by the RECORD clause". The second READ below omits INTO. This is the
*> SEQUENTIAL arm of the READ binder; pb694-read-no-into-relative is the relative arm.
*> The first READ, with INTO, is legal. kb/Work PB694 -> COBOLNET3983.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB694N1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FIN ASSIGN TO "pb694n1.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  FIN RECORD CONTAINS 10 CHARACTERS.
       WORKING-STORAGE SECTION.
       01  W PIC X(10).
       PROCEDURE DIVISION.
           OPEN INPUT FIN.
           READ FIN INTO W AT END CONTINUE END-READ.
           READ FIN AT END CONTINUE END-READ.
           CLOSE FIN.
           STOP RUN.
