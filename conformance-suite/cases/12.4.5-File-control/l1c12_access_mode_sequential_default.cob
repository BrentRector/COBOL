      *>--- conformance-case
      *> id: l1c12_access_mode_sequential_default
      *> rules: [GR-12.4.5.5.3-1, GR-12.4.5.5.3-2, FMT-12.4.5.5.1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §12.4.5.5.3 GR1/GR2 — no ACCESS MODE clause assumes
      *>   sequential; sequential access order
      *> GR1: "If the ACCESS MODE clause is not specified, sequential
      *>   access is assumed."
      *>   cite.py --check 12.4.5.5.3 "If the ACCESS MODE clause is not
      *>     specified, sequential access is
      *>     assumed."  -> OK  §12.4.5.5.3 1)  (General rules)
      *> GR2 a): "For sequential files this sequence is specified by
      *>   predecessor-successor record
      *> relationships established by the execution of WRITE statements
      *>   when the physical file is created
      *> or extended."
      *>   cite.py -> OK  §12.4.5.5.3 2) a)  (General rules)
      *> GR2 b): "For relative files this sequence is the order of
      *>   ascending relative record numbers of
      *> existing records in the physical file."
      *>   cite.py -> OK  §12.4.5.5.3 2) b)  (General rules)
      *> GR2 c): "For indexed files this sequence is ascending within a
      *>   given key of reference according to
      *> the collating sequence for that key."
      *>   cite.py -> OK  §12.4.5.5.3 2) c)  (General rules)
      *> §14.9.51.4 GR42 a) (WRITE, indexed): "When the write file
      *>   connector is open for output or extend in
      *> the sequential access mode and the value of the prime record
      *>   key is not greater than the value of
      *> the prime record key of the last record written through that
      *>   file connector, the I-O status
      *> associated with the write file connector is set to '21'."
      *>   cite.py -> OK  §14.9.51.4 42)  (General rules)
      *> §14.9.30.4 GR25 (READ): "For a relative file, if the RELATIVE
      *>   KEY clause is specified for
      *> file-name-1, the execution of a READ statement moves the
      *>   relative record number of the record made
      *> available to the relative key data item"  -> cite.py OK
      *>   §14.9.30.4 25)
      *> SQ, RL, IX and IY have NO ACCESS MODE clause.  RLW and IXW
      *>   (ACCESS RANDOM / DYNAMIC) only BUILD
      *> the relative and indexed files out of order, so that sequential
      *>   reading must reorder them.
      *> DERIVATION OF THE EXPECTED OUTPUT:
      *>   SQ: OUTPUT writes S2,S1; EXTEND writes S0; READ in WRITE
      *>     order (GR2 a)   => SQ:S2 / SQ:S1 / SQ:S0
      *>   RL: records at RRN 5,2,9 (written in that order).  RK is set
      *>     to 9 BEFORE the first READ: under
      *>       random access that would select R9; under the assumed
      *>         sequential access (GR1) READ returns
      *>       ascending RRN (GR2 b) and moves each RRN into RK (READ
      *>         GR25)       => RL:R2 02 / RL:R5 05 /
      *>       RL:R9 09, then AT END
      *>         => RL:END
      *>   IX: records C1,A3,B2 (prime key letter, alternate key digit).
      *>     Plain READ on the prime key,
      *>       ascending (GR2 c)
      *>         => IX:A3 / IX:B2 / IX:C1
      *>       START on the alternate key IX-ALT >= LOW-VALUE makes it
      *>         the key of reference; READ ascending on
      *>       it (GR2 c)
      *>         => IX:C1 / IX:B2 / IX:A3
      *>   IY: no ACCESS clause -> sequential (GR1), so WRITE C3 then A1
      *>     (key not greater) -> 00 then 21
      *>       (WRITE GR42 a); random/dynamic access would have accepted
      *>         A1       => IY:00 21
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C12J.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SQ ASSIGN TO "L1C12J.SQ"
               FILE STATUS IS SQ-ST.
           SELECT RLW ASSIGN TO "L1C12J.RL"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RKW.
           SELECT RL ASSIGN TO "L1C12J.RL"
               ORGANIZATION IS RELATIVE
               RELATIVE KEY IS RK
               FILE STATUS IS RL-ST.
           SELECT IXW ASSIGN TO "L1C12J.IX"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IXW-KEY
               ALTERNATE RECORD KEY IS IXW-ALT.
           SELECT IX ASSIGN TO "L1C12J.IX"
               ORGANIZATION IS INDEXED
               RECORD KEY IS IX-KEY
               ALTERNATE RECORD KEY IS IX-ALT
               FILE STATUS IS IX-ST.
           SELECT IY ASSIGN TO "L1C12J.IY"
               ORGANIZATION IS INDEXED
               RECORD KEY IS IY-KEY
               FILE STATUS IS IY-ST.
       DATA DIVISION.
       FILE SECTION.
       FD  SQ.
       01  SQ-REC      PIC X(2).
       FD  RLW.
       01  RLW-REC     PIC X(2).
       FD  RL.
       01  RL-REC      PIC X(2).
       FD  IXW.
       01  IXW-REC.
           02  IXW-KEY PIC X.
           02  IXW-ALT PIC X.
       FD  IX.
       01  IX-REC.
           02  IX-KEY  PIC X.
           02  IX-ALT  PIC X.
       FD  IY.
       01  IY-REC.
           02  IY-KEY  PIC X.
           02  IY-VAL  PIC 9.
       WORKING-STORAGE SECTION.
       01  SQ-ST       PIC XX.
       01  RL-ST       PIC XX.
       01  IX-ST       PIC XX.
       01  IY-ST       PIC XX.
       01  IY-ST1      PIC XX.
       01  RKW         PIC 99.
       01  RK          PIC 99.
       01  WS-EOF      PIC X.
       PROCEDURE DIVISION.
       MAIN-P.
      *> GR2 a) — sequential file, created then extended
           OPEN OUTPUT SQ.
           MOVE "S2" TO SQ-REC. WRITE SQ-REC.
           MOVE "S1" TO SQ-REC. WRITE SQ-REC.
           CLOSE SQ.
           OPEN EXTEND SQ.
           MOVE "S0" TO SQ-REC. WRITE SQ-REC.
           CLOSE SQ.
           OPEN INPUT SQ.
           MOVE "N" TO WS-EOF.
           PERFORM UNTIL WS-EOF = "Y"
               READ SQ
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END DISPLAY "SQ:" SQ-REC
               END-READ
           END-PERFORM.
           CLOSE SQ.
      *> GR1 + GR2 b) — relative file built at RRN 5, 2, 9
           OPEN OUTPUT RLW.
           MOVE 5 TO RKW. MOVE "R5" TO RLW-REC. WRITE RLW-REC.
           MOVE 2 TO RKW. MOVE "R2" TO RLW-REC. WRITE RLW-REC.
           MOVE 9 TO RKW. MOVE "R9" TO RLW-REC. WRITE RLW-REC.
           CLOSE RLW.
           OPEN INPUT RL.
           MOVE 9 TO RK.
           MOVE "N" TO WS-EOF.
           PERFORM UNTIL WS-EOF = "Y"
               READ RL
                   AT END MOVE "Y" TO WS-EOF
                          DISPLAY "RL:END"
                   NOT AT END DISPLAY "RL:" RL-REC " " RK
               END-READ
           END-PERFORM.
           CLOSE RL.
      *> GR2 c) — indexed file built out of key order
           OPEN OUTPUT IXW.
           MOVE "C1" TO IXW-REC. WRITE IXW-REC.
           MOVE "A3" TO IXW-REC. WRITE IXW-REC.
           MOVE "B2" TO IXW-REC. WRITE IXW-REC.
           CLOSE IXW.
           OPEN INPUT IX.
           MOVE "N" TO WS-EOF.
           PERFORM UNTIL WS-EOF = "Y"
               READ IX
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END DISPLAY "IX:" IX-REC
               END-READ
           END-PERFORM.
           MOVE LOW-VALUE TO IX-ALT.
           START IX KEY IS NOT LESS THAN IX-ALT.
           MOVE "N" TO WS-EOF.
           PERFORM UNTIL WS-EOF = "Y"
               READ IX
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END DISPLAY "IX:" IX-REC
               END-READ
           END-PERFORM.
           CLOSE IX.
      *> GR1 — an indexed file with no ACCESS clause refuses an
      *>   out-of-order WRITE
           OPEN OUTPUT IY.
           MOVE "C3" TO IY-REC.
           WRITE IY-REC INVALID KEY CONTINUE END-WRITE.
           MOVE IY-ST TO IY-ST1.
           MOVE "A1" TO IY-REC.
           WRITE IY-REC INVALID KEY CONTINUE END-WRITE.
           DISPLAY "IY:" IY-ST1 " " IY-ST.
           CLOSE IY.
           STOP RUN.
