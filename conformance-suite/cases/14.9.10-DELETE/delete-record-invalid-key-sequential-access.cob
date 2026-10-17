*>--- conformance-case
*> id: delete-record-invalid-key-sequential-access
*> rules: [SR-14.9.10.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.10.3 SR2: "The INVALID KEY and the NOT INVALID KEY
      *> phrases shall not be specified for a DELETE RECORD statement that
      *> references a file that is in sequential access mode."
      *> Tolerated (warning, bind unchanged) under --permissive: the L1-L3
      *> CCVS phrase-placement leniency family (kb/Work PB144).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB144N1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT R ASSIGN TO "n1.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               RELATIVE KEY IS R-K.
       DATA DIVISION.
       FILE SECTION.
       FD R.
       01 R-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 R-K PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           OPEN I-O R
           READ R
           DELETE R RECORD
               INVALID KEY CONTINUE
           END-DELETE
           STOP RUN.
