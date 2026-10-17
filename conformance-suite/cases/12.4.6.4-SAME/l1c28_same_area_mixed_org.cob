      *>--- conformance-case
      *> id: l1c28_same_area_mixed_org
      *> rules: [SR-12.4.6.4.3-4, GR-12.4.6.4.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §12.4.6.4.3 SR4 / §12.4.6.4.4 GR1 — one file-area SAME
      *> clause over SEQUENTIAL, RELATIVE and INDEXED files with three
      *> different access modes, each opened in turn.
      *> SR4: "The files specified in a given SAME clause need not all
      *>   have the same organization or access."
      *>   cite.py: OK  §12.4.6.4.3 4)  (Syntax rules)
      *> GR1: "A file-area format SAME clause specifies that two or more
      *>   files referenced by file-name-1, file-name-2 are to use the
      *>   same memory area during processing. ... No more than one of
      *>   these files may be in the open mode at a given time."
      *>   cite.py: OK  §12.4.6.4.4 1)  (General rules)
      *> Derivation. SR4 makes this clause legal: FS is SEQUENTIAL
      *> ACCESS SEQUENTIAL, FR is RELATIVE ACCESS RANDOM, FI is INDEXED
      *> ACCESS DYNAMIC, so the program compiles. GR1: the three files
      *> share one area, and the program keeps at most ONE of them open
      *> at any time (each OPEN is preceded by the CLOSE of the other),
      *> so the sharing is transparent: every file keeps exactly the
      *> records written to it, which were written while a DIFFERENT
      *> member had previously used the shared area.
      *>   FS: written SEQ-A, SEQ-B; read sequentially -> SEQ-A, SEQ-B.
      *>   FR: key 2 = REL-2, key 1 = REL-1; random read 1 then 2.
      *>   FI: keys K2 then K1 written (dynamic access, random WRITE);
      *>   read NEXT after OPEN INPUT returns ascending key order:
      *>   K1 IDX-1, then K2 IDX-2.
      *> Each file's status after its last I-O is 00 (successful).
      *> A wrong implementation (a file's records clobbered by another
      *> member's use of the shared area, or SR4 violated by rejecting
      *> the mixed clause) changes these lines.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C28A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FS ASSIGN TO "L1C28AS.DAT"
               ORGANIZATION IS SEQUENTIAL ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST-S.
           SELECT FR ASSIGN TO "L1C28AR.DAT"
               ORGANIZATION IS RELATIVE ACCESS MODE IS RANDOM
               RELATIVE KEY IS WS-RK
               FILE STATUS IS ST-R.
           SELECT FI ASSIGN TO "L1C28AI.DAT"
               ORGANIZATION IS INDEXED ACCESS MODE IS DYNAMIC
               RECORD KEY IS FI-K
               FILE STATUS IS ST-I.
       I-O-CONTROL.
           SAME AREA FOR FS FR FI.
       DATA DIVISION.
       FILE SECTION.
       FD FS.
       01 FS-REC PIC X(6).
       FD FR.
       01 FR-REC PIC X(8).
       FD FI.
       01 FI-REC.
          05 FI-K PIC XX.
          05 FI-D PIC X(6).
       WORKING-STORAGE SECTION.
       01 WS-RK PIC 9(4).
       01 ST-S PIC XX.
       01 ST-R PIC XX.
       01 ST-I PIC XX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT FS
           MOVE "SEQ-A" TO FS-REC
           WRITE FS-REC
           MOVE "SEQ-B" TO FS-REC
           WRITE FS-REC
           CLOSE FS
           OPEN OUTPUT FR
           MOVE 2 TO WS-RK
           MOVE "REL-2" TO FR-REC
           WRITE FR-REC
           MOVE 1 TO WS-RK
           MOVE "REL-1" TO FR-REC
           WRITE FR-REC
           CLOSE FR
           OPEN OUTPUT FI
           MOVE "K2" TO FI-K
           MOVE "IDX-2" TO FI-D
           WRITE FI-REC
           MOVE "K1" TO FI-K
           MOVE "IDX-1" TO FI-D
           WRITE FI-REC
           CLOSE FI
           OPEN INPUT FS
           READ FS
           DISPLAY "FS " FS-REC
           READ FS
           DISPLAY "FS " FS-REC " " ST-S
           CLOSE FS
           OPEN INPUT FR
           MOVE 1 TO WS-RK
           READ FR
           DISPLAY "FR " FR-REC
           MOVE 2 TO WS-RK
           READ FR
           DISPLAY "FR " FR-REC " " ST-R
           CLOSE FR
           OPEN INPUT FI
           READ FI NEXT RECORD
           DISPLAY "FI " FI-K " " FI-D
           READ FI NEXT RECORD
           DISPLAY "FI " FI-K " " FI-D " " ST-I
           CLOSE FI
           STOP RUN.
