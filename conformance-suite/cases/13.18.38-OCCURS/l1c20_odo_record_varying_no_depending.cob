      *>--- conformance-case
      *> id: l1c20_odo_record_varying_no_depending
      *> rules: [GR-13.18.38.4-9]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §13.18.38.4 GR9 — a format 2 OCCURS record under RECORD
      *> VARYING without DEPENDING: WRITE, REWRITE and RELEASE size the
      *> record from data-name-1 of the OCCURS clause.
      *>
      *> THE RULES.
      *> §13.18.38.4 GR9: "If format 2 is specified in a record
      *>   description entry and the associated file description or
      *>   sort-merge description entry contains the VARYING phrase of
      *>   the RECORD clause, the records are variable length. If the
      *>   DEPENDING ON phrase of the RECORD clause is not specified,
      *>   the content of the data item referenced by data-name-1 of
      *>   the OCCURS clause shall be set to the number of occurrences
      *>   to be written before the execution of any RELEASE,
      *>   REWRITE, or WRITE statement referencing that record
      *>   description entry."
      *>   OK  §13.18.38.4 9)  (General rules)
      *> §13.18.43.4 GR13 c): with no data-name-1 and a
      *>   variable-occurrence item, the size is "the sum of the fixed
      *>   portion and that portion of the table described by the
      *>   number of occurrences at the time of execution of the
      *>   output statement".
      *>   OK  §13.18.43.4 13) c)  (General rules)
      *> §13.18.43.4 GR15: with data-name-1, after a READ "the
      *>   contents of the data item referenced by data-name-1 will
      *>   indicate the number of bytes in the record just read".
      *>   OK  §13.18.43.4 15)  (General rules)
      *> §14.9.35.4 GR16 (record sequential): if the record's size "is
      *>   not equal to the number of bytes in the record being
      *>   replaced, the execution of the REWRITE statement is
      *>   unsuccessful and the I-O status in the rewrite file
      *>   connector is set to '44'".
      *>   OK  §14.9.35.4 16)  (General rules)
      *> §14.9.40.4 GR15 b): with GIVING, "the size of any record
      *>   written to file-name-3 is the size of that record when it
      *>   was read from file-name-1" (the sort file, here fed by
      *>   RELEASE).
      *>   OK  §14.9.40.4 15)  (General rules)
      *>
      *> DERIVATION (size = 1 byte of FN/SN + 1 byte per occurrence).
      *> WRITE FN=1 "A" -> 2 bytes; FN=3 "BBB" -> 4; FN=6 "CCCCCC" -> 7.
      *> REWRITE of record 2 with FN still 3 (FC(2) now "X"): 4 = 4
      *>   bytes -> RW1=00. REWRITE of record 3 with FN set to 5:
      *>   6 bytes, record being replaced has 7 -> RW2=44 and the
      *>   record is unchanged (the empty USE procedure lets the run
      *>   continue).
      *> READ back through FI (DEPENDING ON RL, GR15):
      *>   W 02 [1A] / W 04 [3BXB] / W 07 [6CCCCCC].
      *> RELEASE SN=4 "DDDD" (5 bytes), SN=1 "E" (2), SN=2 "FF" (3);
      *>   SORT ascending SN GIVING FG, read back through FJ:
      *>   G 02 [1E] / G 03 [2FF] / G 05 [4DDDD].
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C20I.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FO ASSIGN TO "L1C20I1.DAT" FILE STATUS IS ST.
           SELECT FI ASSIGN TO "L1C20I1.DAT".
           SELECT SW ASSIGN TO "L1C20IW.TMP".
           SELECT FG ASSIGN TO "L1C20I2.DAT".
           SELECT FJ ASSIGN TO "L1C20I2.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD FO RECORD IS VARYING IN SIZE FROM 2 TO 7 CHARACTERS.
       01 FR.
          05 FN PIC 9.
          05 FC PIC X OCCURS 1 TO 6 TIMES DEPENDING ON FN.
       FD FI RECORD IS VARYING IN SIZE FROM 2 TO 7 CHARACTERS
             DEPENDING ON RL.
       01 IR PIC X(7).
       SD SW RECORD IS VARYING IN SIZE FROM 2 TO 7 CHARACTERS.
       01 SR.
          05 SN PIC 9.
          05 SC PIC X OCCURS 1 TO 6 TIMES DEPENDING ON SN.
       FD FG RECORD IS VARYING IN SIZE FROM 2 TO 7 CHARACTERS.
       01 GR7 PIC X(7).
       01 GR2 PIC X(2).
       FD FJ RECORD IS VARYING IN SIZE FROM 2 TO 7 CHARACTERS
             DEPENDING ON RL.
       01 JR PIC X(7).
       WORKING-STORAGE SECTION.
       01 ST  PIC XX.
       01 RL  PIC 99.
       01 K   PIC 9.
       01 EOF PIC X.
       PROCEDURE DIVISION.
       DECLARATIVES.
       FO-ERR SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON FO.
       FO-ERR-P.
           EXIT.
       END DECLARATIVES.
       MAIN SECTION.
       M-1.
           OPEN OUTPUT FO.
           MOVE 1 TO FN.
           MOVE "A" TO FC (1).
           WRITE FR.
           MOVE 3 TO FN.
           PERFORM VARYING K FROM 1 BY 1 UNTIL K > 3
               MOVE "B" TO FC (K)
           END-PERFORM.
           WRITE FR.
           MOVE 6 TO FN.
           PERFORM VARYING K FROM 1 BY 1 UNTIL K > 6
               MOVE "C" TO FC (K)
           END-PERFORM.
           WRITE FR.
           CLOSE FO.
           OPEN I-O FO.
           READ FO.
           READ FO.
           MOVE "X" TO FC (2).
           REWRITE FR.
           DISPLAY "RW1=" ST.
           READ FO.
           MOVE 5 TO FN.
           REWRITE FR.
           DISPLAY "RW2=" ST.
           CLOSE FO.
           OPEN INPUT FI.
           MOVE "N" TO EOF.
           PERFORM UNTIL EOF = "Y"
               READ FI
                   AT END MOVE "Y" TO EOF
                   NOT AT END DISPLAY "W " RL " [" IR (1:RL) "]"
               END-READ
           END-PERFORM.
           CLOSE FI.
           SORT SW ON ASCENDING KEY SN
               INPUT PROCEDURE IS REL-S
               GIVING FG.
           OPEN INPUT FJ.
           MOVE "N" TO EOF.
           PERFORM UNTIL EOF = "Y"
               READ FJ
                   AT END MOVE "Y" TO EOF
                   NOT AT END DISPLAY "G " RL " [" JR (1:RL) "]"
               END-READ
           END-PERFORM.
           CLOSE FJ.
           STOP RUN.
       REL-S SECTION.
       R-1.
           MOVE 4 TO SN.
           PERFORM VARYING K FROM 1 BY 1 UNTIL K > 4
               MOVE "D" TO SC (K)
           END-PERFORM.
           RELEASE SR.
           MOVE 1 TO SN.
           MOVE "E" TO SC (1).
           RELEASE SR.
           MOVE 2 TO SN.
           MOVE "F" TO SC (1).
           MOVE "F" TO SC (2).
           RELEASE SR.
