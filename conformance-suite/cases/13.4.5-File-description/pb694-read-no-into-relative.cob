*>--- conformance-case
*> id: pb694-read-no-into-relative
*> rules: [SR-13.4.5.3-3]
*> source-format: free
*> portable: unverified
*>---
*> ISO/IEC 1989:2023 §13.4.5.3 syntax rule 3 c): "When no record description entries
*> are specified: ... c) an INTO phrase shall be specified on all READ statements
*> associated with the file." SR3 is a FORMATS 1 AND 2 rule, so it binds a RELATIVE
*> file too (SR7 withdraws the record-less permission for an INDEXED file only). RLF
*> has no record description entry, and the random READ below omits INTO. This is the
*> RELATIVE arm of the READ binder (KeyedIoBinder), which the sequential arm reroutes
*> to; pb694-read-no-into-sequential is the sequential arm. kb/Work PB694 -> COBOLNET3983.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB694N2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RLF ASSIGN TO "pb694n2.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK.
       DATA DIVISION.
       FILE SECTION.
       FD  RLF RECORD CONTAINS 5 CHARACTERS.
       WORKING-STORAGE SECTION.
       01  RK PIC 9(4) VALUE 1.
       PROCEDURE DIVISION.
           OPEN INPUT RLF.
           READ RLF INVALID KEY CONTINUE END-READ.
           CLOSE RLF.
           STOP RUN.
