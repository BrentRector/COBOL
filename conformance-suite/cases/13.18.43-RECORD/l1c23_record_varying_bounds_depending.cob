      *>--- conformance-case
      *> id: l1c23_record_varying_bounds_depending
      *> rules: [SR-13.18.43.3-6, GR-13.18.43.4-7, GR-13.18.43.4-12]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §13.18.43.4 7) and 12) — VARYING min/max bounds; DEPENDING item untouched by I-O verbs
      *> GR7: "Format 2 is used to specify variable-length records. Integer-2 specifies the minimum
      *>   number of bytes to be contained in any record of the file. Integer-3 specifies the maximum
      *>   number of bytes in any record of the file."
      *>   cite.py: OK  §13.18.43.4 7)  (General rules)
      *> GR12: "If data-name-1 is specified, the execution of a DELETE, RELEASE, REWRITE, START, or
      *>   WRITE statement or the unsuccessful execution of a READ or RETURN statement does not alter
      *>   the content of the data item referenced by data-name-1."
      *>   cite.py: OK  §13.18.43.4 12)  (General rules)
      *> Supporting rules:
      *>   GR13 a) the size written is "the content of the data item referenced by data-name-1"
      *>     cite.py: OK  §13.18.43.4 13)  (General rules)
      *>   GR14 a) out-of-range size: "If a REWRITE or WRITE statement is being executed, the
      *>     EC-I-O-LOGIC-ERROR exception condition is set to exist, and the execution of the REWRITE or
      *>     WRITE statement is unsuccessful."  cite.py: OK  §13.18.43.4 14)  (General rules)
      *>   §9.1.13.7 4) a) I-O status 44: "an attempt is made to write or rewrite a record that is
      *>     larger than the largest or smaller than the smallest record allowed by the RECORD IS VARYING
      *>     clause"   cite.py: OK  §9.1.13.7 4) a)  (Logic error condition with unsuccessful completion)
      *>   GR15 "after the successful execution of a READ or RETURN statement for the file, the contents
      *>     of the data item referenced by data-name-1 will indicate the number of bytes in the record
      *>     just read"   cite.py: OK  §13.18.43.4 15)  (General rules)
      *> Every file is VARYING IN SIZE FROM 5 TO 20.  Checking is off, so an EC only fails the statement.
      *> EXPECTED OUTPUT, derived (GR7 = the 44/00 outcomes at the bounds; GR12 = the L= value printed
      *> after each DELETE/RELEASE/REWRITE/START/WRITE and each unsuccessful READ/RETURN):
      *>   W4 44 L=04       4 < integer-2 (5): unsuccessful, 44; WRITE leaves L (GR12).
      *>   W5 00 L=05       5 = minimum: written; L unaltered.
      *>   W20 00 L=20      20 = maximum: written; L unaltered.
      *>   W21 44 L=21      21 > integer-3 (20): 44; L unaltered.
      *>   R 00 L=05 [ABCDE]                first record read back: 5 bytes (GR15 sets L).
      *>   R 00 L=20 [ABCDEFGHIJKLMNOPQRST] second record: 20 bytes.
      *>   E 10 L=99        unsuccessful READ (at end, status 10) leaves the 99 moved beforehand.
      *>   S 00 L=77        indexed START: L (77) unaltered.
      *>   RF 23 L=66       unsuccessful keyed READ (no record ZZZ, 23): L unaltered.
      *>   RK 00 L=05       successful keyed READ of AAA (written with L=5): GR15 L=05.
      *>   RW 00 L=15       REWRITE at 15 bytes (within 5..20): succeeds; L unaltered.
      *>   RW 44 L=25       REWRITE at 25 > 20: 44; L unaltered.
      *>   D 00 L=55        DELETE of BBB: L unaltered.
      *>   RK 00 L=15       AAA read back: the successful REWRITE made it 15 bytes.
      *>   RL 05            RELEASE with LS = 5: LS unaltered.
      *>   RL 07            RELEASE with LS = 7: LS unaltered.
      *>   RT 07 AAA        RETURN in key order: AAA first, released at 7 bytes (GR15).
      *>   RT 05 BBB        then BBB at 5 bytes.
      *>   RE 88            unsuccessful RETURN (at end) leaves the 88 moved beforehand.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C23O.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "L1C23F1.DAT"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS1.
           SELECT F2 ASSIGN TO "L1C23F2.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS K2
               FILE STATUS IS FS2.
           SELECT S1 ASSIGN TO "L1C23S1.TMP".
       DATA DIVISION.
       FILE SECTION.
       FD  F1
           RECORD IS VARYING IN SIZE FROM 5 TO 20 CHARACTERS
               DEPENDING ON L1.
       01  R1 PIC X(20).
       FD  F2
           RECORD IS VARYING IN SIZE FROM 5 TO 20 CHARACTERS
               DEPENDING ON L2.
       01  R2.
           05  K2 PIC X(3).
           05  D2 PIC X(17).
       SD  S1
           RECORD IS VARYING IN SIZE FROM 5 TO 20 CHARACTERS
               DEPENDING ON LS.
       01  SR.
           05  SK PIC X(3).
           05  SD1 PIC X(17).
       WORKING-STORAGE SECTION.
       01  FS1 PIC XX.
       01  FS2 PIC XX.
       01  L1 PIC 99.
       01  L2 PIC 99.
       01  LS PIC 99.
       01  EOF-S PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN SECTION.
       M1.
           OPEN OUTPUT F1
           MOVE "ABCDEFGHIJKLMNOPQRST" TO R1
           MOVE 4 TO L1
           WRITE R1
           DISPLAY "W4 " FS1 " L=" L1
           MOVE 5 TO L1
           WRITE R1
           DISPLAY "W5 " FS1 " L=" L1
           MOVE 20 TO L1
           WRITE R1
           DISPLAY "W20 " FS1 " L=" L1
           MOVE 21 TO L1
           WRITE R1
           DISPLAY "W21 " FS1 " L=" L1
           CLOSE F1
           OPEN INPUT F1
           READ F1
           DISPLAY "R " FS1 " L=" L1 " [" R1 (1:L1) "]"
           READ F1
           DISPLAY "R " FS1 " L=" L1 " [" R1 (1:L1) "]"
           MOVE 99 TO L1
           READ F1 AT END
               CONTINUE
           END-READ
           DISPLAY "E " FS1 " L=" L1
           CLOSE F1.
       M2.
           OPEN OUTPUT F2
           MOVE "AAAAAAAAAAAAAAAAAAAA" TO R2
           MOVE 5 TO L2
           WRITE R2
           MOVE "BBBBBBBBBBBBBBBBBBBB" TO R2
           MOVE 12 TO L2
           WRITE R2
           CLOSE F2
           OPEN I-O F2
           MOVE "AAA" TO K2
           MOVE 77 TO L2
           START F2 KEY IS EQUAL TO K2
           DISPLAY "S " FS2 " L=" L2
           MOVE "ZZZ" TO K2
           MOVE 66 TO L2
           READ F2 KEY IS K2 INVALID KEY
               CONTINUE
           END-READ
           DISPLAY "RF " FS2 " L=" L2
           MOVE "AAA" TO K2
           READ F2 KEY IS K2
           DISPLAY "RK " FS2 " L=" L2
           MOVE 15 TO L2
           REWRITE R2
           DISPLAY "RW " FS2 " L=" L2
           MOVE 25 TO L2
           REWRITE R2
           DISPLAY "RW " FS2 " L=" L2
           MOVE "BBB" TO K2
           MOVE 55 TO L2
           DELETE F2
           DISPLAY "D " FS2 " L=" L2
           MOVE "AAA" TO K2
           READ F2 KEY IS K2
           DISPLAY "RK " FS2 " L=" L2
           CLOSE F2.
       M3.
           SORT S1 ON ASCENDING KEY SK
               INPUT PROCEDURE IS IN-P
               OUTPUT PROCEDURE IS OUT-P
           STOP RUN.
       IN-P SECTION.
       I1.
           MOVE "BBBBBBBBBBBBBBBBBBBB" TO SR
           MOVE 5 TO LS
           RELEASE SR
           DISPLAY "RL " LS
           MOVE "AAAAAAAAAAAAAAAAAAAA" TO SR
           MOVE 7 TO LS
           RELEASE SR
           DISPLAY "RL " LS.
       OUT-P SECTION.
       O1.
           RETURN S1 AT END
               MOVE "Y" TO EOF-S
           END-RETURN
           DISPLAY "RT " LS " " SK
           RETURN S1 AT END
               MOVE "Y" TO EOF-S
           END-RETURN
           DISPLAY "RT " LS " " SK
           MOVE 88 TO LS
           RETURN S1 AT END
               MOVE "Y" TO EOF-S
           END-RETURN
           DISPLAY "RE " LS.
