*>--- conformance-case
*> id: pb334_read_previous_sequential_2014
*> rules: [GR-14.9.30.4-19, GR-14.9.30.4-21]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB334 — READ ... PREVIOUS on a file with SEQUENTIAL organization, the 2014 leg.
      *> Until PB334 this arm of the binder never called readDirection(): PREVIOUS bound as a forward
      *> read at every edition and the COBOLNET0900 gate never fired.
      *>
      *> THE RULES, and every expected value below derived from them (no observation):
      *>  14.9.30.4 GR19 - "An implicit or explicit NEXT phrase or a PREVIOUS phrase results in a
      *>    sequential read", so every READ here is Format 1 (12.4.5.5.2 SR2 bars ACCESS RANDOM/DYNAMIC
      *>    on a sequential file, and 14.9.30.3 SR8 then implies NEXT when no direction is written).
      *>  14.9.30.4 GR21 "When the file is a sequential file":
      *>    b) file position indicator established by a prior OPEN -> "the first existing record that is
      *>       selected is made available, regardless of whether NEXT or PREVIOUS is specified"  (R1, R9)
      *>    c) established by a prior successful READ -> the first existing record whose number is
      *>       "greater than the file position indicator if NEXT ... or is less than the file position
      *>       indicator if PREVIOUS"                                        (R2-R7)
      *>    e) no such record -> the at end condition                        (R8)
      *>    f) the indicator becomes the number of the record made available (R4 proves it: the forward
      *>       read after a backward one resumes from the REPOSITIONED indicator, not from record 3)
      *>  14.9.30.4 GR24 a)/c) - the at end condition sets '10' and transfers control to the AT END
      *>    imperative                                                       (R8)
      *>  14.9.35.4 GR5 - "the operating environment logically replaces the record that was accessed by
      *>    the READ statement", so the REWRITE after a backward READ replaces RECORD 1        (RW)
      *>
      *> EDITIONS: Annex E is INFORMATIVE and its only READ-positioning change, E.2 item 22, amends the
      *> INDEXED after-OPEN rule (GR21 d.3); the SEQUENTIAL block prints rule b) unamended in 2023, so
      *> this behaviour is identical at 2002, 2014 and 2023 -- which is what the three copies of this
      *> program assert. At --std 85 the whole program is rejected: negative/pb334-read-previous-85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB334P4.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SQF ASSIGN TO "pb334p4.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS WS-ST.
           SELECT VRF ASSIGN TO "pb334v4.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS WS-V.
           SELECT EMF ASSIGN TO "pb334e4.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS WS-E.
       DATA DIVISION.
       FILE SECTION.
       FD  SQF.
       01  SQ-REC        PIC X(4).
       FD  VRF RECORD IS VARYING IN SIZE FROM 3 TO 8
               DEPENDING ON WS-LEN.
       01  VR-REC        PIC X(8).
       FD  EMF.
       01  EM-REC        PIC X(4).
       WORKING-STORAGE SECTION.
       01  WS-ST         PIC XX.
       01  WS-V          PIC XX.
       01  WS-E          PIC XX.
       01  WS-LEN        PIC 9(4) COMP.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT SQF
           MOVE "AAAA" TO SQ-REC
           WRITE SQ-REC
           MOVE "BBBB" TO SQ-REC
           WRITE SQ-REC
           MOVE "CCCC" TO SQ-REC
           WRITE SQ-REC
           CLOSE SQF
      *> Phase 1 - the walk. GR21 b) for R1, then c) both ways.
           OPEN INPUT SQF
           READ SQF NEXT RECORD
           DISPLAY "R1=" SQ-REC "|" WS-ST
           READ SQF NEXT RECORD
           DISPLAY "R2=" SQ-REC "|" WS-ST
           READ SQF PREVIOUS RECORD
           DISPLAY "R3=" SQ-REC "|" WS-ST
           READ SQF NEXT RECORD
           DISPLAY "R4=" SQ-REC "|" WS-ST
           READ SQF NEXT RECORD
           DISPLAY "R5=" SQ-REC "|" WS-ST
           READ SQF PREVIOUS RECORD
           DISPLAY "R6=" SQ-REC "|" WS-ST
           READ SQF PREVIOUS RECORD
           DISPLAY "R7=" SQ-REC "|" WS-ST
           READ SQF PREVIOUS RECORD
               AT END DISPLAY "R8=ATEND|" WS-ST
           END-READ
           CLOSE SQF
      *> Phase 2 - GR21 b) on its own: after OPEN, PREVIOUS gives the FIRST record.
           OPEN INPUT SQF
           READ SQF PREVIOUS RECORD
           DISPLAY "R9=" SQ-REC "|" WS-ST
           CLOSE SQF
      *> Phase 3 - 14.9.35.4 GR5: the backward read establishes the REWRITE target.
           OPEN I-O SQF
           READ SQF NEXT RECORD
           READ SQF NEXT RECORD
           READ SQF PREVIOUS RECORD
           MOVE "AZZZ" TO SQ-REC
           REWRITE SQ-REC
           CLOSE SQF
           OPEN INPUT SQF
           READ SQF NEXT RECORD
           DISPLAY "RW=" SQ-REC "|" WS-ST
           CLOSE SQF
      *> Phase 4 - the same GR21 rules on a RECORD VARYING file, whose frames are NOT uniformly
      *> wide: the record number still selects the record, and 13.18.43 GR15 restores each record's
      *> length into the DEPENDING item on the way back.
           OPEN OUTPUT VRF
           MOVE 3 TO WS-LEN
           MOVE "AAA" TO VR-REC
           WRITE VR-REC
           MOVE 6 TO WS-LEN
           MOVE "BBBBBB" TO VR-REC
           WRITE VR-REC
           MOVE 4 TO WS-LEN
           MOVE "CCCC" TO VR-REC
           WRITE VR-REC
           CLOSE VRF
           OPEN INPUT VRF
           READ VRF NEXT RECORD
           READ VRF NEXT RECORD
           READ VRF NEXT RECORD
           DISPLAY "V3=" VR-REC "|" WS-LEN "|" WS-V
           READ VRF PREVIOUS RECORD
           DISPLAY "V4=" VR-REC "|" WS-LEN "|" WS-V
           READ VRF PREVIOUS RECORD
           DISPLAY "V5=" VR-REC "|" WS-LEN "|" WS-V
           READ VRF PREVIOUS RECORD
               AT END DISPLAY "V6=ATEND|" WS-V
           END-READ
           CLOSE VRF
      *> Phase 5 - GR21 e) on an EMPTY file. Rule b) targets the first record and there is none, so
      *> this is the at end condition ('10' + the AT END imperative), NOT a positioning failure.
           OPEN OUTPUT EMF
           CLOSE EMF
           OPEN INPUT EMF
           READ EMF PREVIOUS RECORD
               AT END DISPLAY "E1=ATEND|" WS-E
           END-READ
           CLOSE EMF
           STOP RUN.
