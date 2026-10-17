*>--- conformance-case
*> id: file_collating_seq
*> rules: [GR-12.4.5.7.4-2, GR-12.4.5.12.4-1, GR-14.9.30.4-13,
*>    GR-14.9.41.4-16, GR-14.9.41.4-17]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §12.4.5.7 file-control COLLATING SEQUENCE clause (INDEXED record-key
      *> collating). The prime key collates under ALPHABET REV (Z..A), so the
      *> ascending key order is the REVERSE of native: records WRITTEN A, M, Z
      *> read back (READ NEXT) as Z, M, A (§12.4.5.5.3 GR2c — ascending within a
      *> key of reference according to that key's collating sequence). A START
      *> KEY >= "M" under REV positions at M and walks M, A (weights: Z<M<A).
      *> Then an equivalence-class alphabet (EQV maps 'A' and 'B' to one weight)
      *> makes two byte-different keys EQUAL: the second WRITE of a weight-equal
      *> prime key is duplicate-key '22' (§12.4.5.12.4 GR1 — equality per the
      *> collating sequence). Native ordinal ordering would print A,M,Z and admit
      *> both keys; this golden is greenfield-only (the legacy has no clause).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FILECOLSEQ.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET REV IS "ZYXWVUTSRQPONMLKJIHGFEDCBA"
           ALPHABET EQV IS "A" ALSO "B".
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IXF ASSIGN TO "file-collating-seq.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IX-KEY
               FILE STATUS IS WS-FS
               COLLATING SEQUENCE IS REV.
           SELECT EQF ASSIGN TO "file-collating-eqv.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS EQ-KEY
               FILE STATUS IS WS-FS
               COLLATING SEQUENCE IS EQV.
       DATA DIVISION.
       FILE SECTION.
       FD IXF.
       01 IX-REC.
          05 IX-KEY  PIC X(1).
          05 IX-DATA PIC X(9).
       FD EQF.
       01 EQ-REC.
          05 EQ-KEY  PIC X(1).
          05 EQ-DATA PIC X(9).
       WORKING-STORAGE SECTION.
       01 WS-EOF PIC 9 VALUE 0.
       01 WS-FS  PIC X(2) VALUE "00".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT IXF.
           MOVE "A" TO IX-KEY. WRITE IX-REC.
           MOVE "M" TO IX-KEY. WRITE IX-REC.
           MOVE "Z" TO IX-KEY. WRITE IX-REC.
           CLOSE IXF.
           OPEN INPUT IXF.
           DISPLAY "READ-NEXT UNDER REV:".
           PERFORM UNTIL WS-EOF = 1
               READ IXF NEXT
                   AT END MOVE 1 TO WS-EOF
                   NOT AT END DISPLAY "  K=" IX-KEY
               END-READ
           END-PERFORM.
           CLOSE IXF.
           OPEN INPUT IXF.
           MOVE "M" TO IX-KEY.
           START IXF KEY IS >= IX-KEY
               INVALID KEY DISPLAY "  START-INVALID"
               NOT INVALID KEY DISPLAY "START >= M UNDER REV:"
           END-START.
           MOVE 0 TO WS-EOF.
           PERFORM UNTIL WS-EOF = 1
               READ IXF NEXT
                   AT END MOVE 1 TO WS-EOF
                   NOT AT END DISPLAY "  K=" IX-KEY
               END-READ
           END-PERFORM.
           CLOSE IXF.
           OPEN OUTPUT EQF.
           MOVE "A" TO EQ-KEY. WRITE EQ-REC.
           DISPLAY "WRITE A EQV FS=" WS-FS.
           MOVE "B" TO EQ-KEY. WRITE EQ-REC.
           DISPLAY "WRITE B EQV FS=" WS-FS.
           CLOSE EQF.
           STOP RUN.
