      *>--- conformance-case
      *> id: l1c23_record_format3_variable
      *> rules: [GR-13.18.43.4-18, DOC-A.1-148]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §13.18.43.4 18) and 17) (DOC-A.1-148) — format 3 record sizes come from each description
      *> GR18: "When format 3 of the RECORD clause is used, integer-4 and integer-5 refer to the minimum
      *>   number of bytes in the smallest size record and the maximum number of bytes in the largest
      *>   size record, respectively. However, in this case, the size of each record is completely
      *>   defined in the record description entry."
      *>   cite.py: OK  §13.18.43.4 18)  (General rules)
      *> GR17: "It is implementor defined whether format 3 of the RECORD clause produces fixed-length
      *>   records or variable-length records."   cite.py: OK  §13.18.43.4 17)  (General rules)
      *>   docs/CONFORMANCE.md DOC-A.1-148 documents the choice: VARIABLE-length -- "each WRITE or
      *>   REWRITE writes the record named in the statement at that record's own size, and a READ
      *>   delivers the size that was written -- records are NEVER padded to integer-5".
      *> Observation: F3 (format 3, CONTAINS 5 TO 10) writes R3A (5 bytes) and R3B (10 bytes).  The same
      *> physical file is then read through G3, a format 2 VARYING 5 TO 10 file whose DEPENDING item LG
      *> receives each record's size (§13.18.43.4 15): "after the successful execution of a READ or
      *> RETURN statement for the file, the contents of the data item referenced by data-name-1 will
      *> indicate the number of bytes in the record just read"; cite.py: OK  §13.18.43.4 15)).
      *> EXPECTED OUTPUT, derived:
      *>   WA 00 / WB 00       both WRITEs succeed (5 and 10 lie in 5..10).
      *>   G 00 L=05 [ABCDE]   GR18: R3A's size is its own description's, 5 bytes; under the
      *>                       documented variable choice it is not padded to integer-5 (a FIXED
      *>                       choice would read back L=10 here).
      *>   G 00 L=10 [FGHIJKLMNO]  R3B's own size, 10 bytes.
      *>   G 10                end of file after exactly two records.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C23P3.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F3 ASSIGN TO "L1C23F3.DAT"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS3.
           SELECT G3 ASSIGN TO "L1C23F3.DAT"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FSG.
       DATA DIVISION.
       FILE SECTION.
       FD  F3
           RECORD CONTAINS 5 TO 10 CHARACTERS.
       01  R3A PIC X(5).
       01  R3B PIC X(10).
       FD  G3
           RECORD IS VARYING IN SIZE FROM 5 TO 10 CHARACTERS
               DEPENDING ON LG.
       01  RG PIC X(10).
       WORKING-STORAGE SECTION.
       01  FS3 PIC XX.
       01  FSG PIC XX.
       01  LG PIC 99.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F3
           MOVE "ABCDE" TO R3A
           WRITE R3A
           DISPLAY "WA " FS3
           MOVE "FGHIJKLMNO" TO R3B
           WRITE R3B
           DISPLAY "WB " FS3
           CLOSE F3
           OPEN INPUT G3
           READ G3
           DISPLAY "G " FSG " L=" LG " [" RG (1:LG) "]"
           READ G3
           DISPLAY "G " FSG " L=" LG " [" RG (1:LG) "]"
           READ G3 AT END
               CONTINUE
           END-READ
           DISPLAY "G " FSG
           CLOSE G3
           STOP RUN.
