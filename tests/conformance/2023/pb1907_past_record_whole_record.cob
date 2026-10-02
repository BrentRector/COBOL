      *> kb/Work PB1907 - the cases where record-name-1 is the WHOLE record,
      *> so 14.9.51.4 GR13 / 14.9.35.4 GR15 (Annex A.2 items 64 and 49) have
      *> no bytes outside it to leave undefined. They bound determination
      *> D-WRT1 (docs/CONFORMANCE.md 3): the record area's content goes past
      *> record-name-1 only where the standard takes the number of bytes from
      *> somewhere other than record-name-1 (DEPENDING ON, or the fixed
      *> length integer-1 of a Format 1 file), and never into a LINE
      *> SEQUENTIAL file, whose transfer the standard defines outright.
      *>
      *> Every case writes a 5-byte record-name-1 with the Z's of a 20-byte
      *> record sharing its area, then reads the record back INTO WS-OUT,
      *> which carries exactly the bytes of the record read (14.9.30.4 GR4 b),
      *> 13.18.43.4 GR16): a Z in WS-OUT would be a byte of the area that the
      *> record must not hold.
      *>
      *> EXPECTED VALUES (derived from the rule, not from a run):
      *>   L1  LINE SEQUENTIAL, no DEPENDING: 14.9.51.4 GR21 drops spaces to
      *>       the right of the rightmost non-space character, and GR13 b)
      *>       makes the record the 5 bytes of record-name-1: ABCDE.
      *>   L2  LINE SEQUENTIAL with DEPENDING ON 12: GR22 fills the record
      *>       "to the right of the rightmost non-space character with one
      *>       or more space characters": ABCDE then spaces, never Z.
      *>   L3  LINE SEQUENTIAL REWRITE of a longer record by a shorter
      *>       record-name-1: 14.9.35.4 GR17 c) appends spaces to reach the
      *>       length of the record being replaced: klmno then spaces.
      *>   N1  RECORD VARYING 5 TO 20 without DEPENDING ON: GR13 b) the
      *>       record is the 5 bytes of record-name-1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1907NP.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT LF ASSIGN TO "pb1907np-l1.dat"
               ORGANIZATION IS LINE SEQUENTIAL FILE STATUS IS LF-ST.
           SELECT LD ASSIGN TO "pb1907np-l2.dat"
               ORGANIZATION IS LINE SEQUENTIAL FILE STATUS IS LD-ST.
           SELECT LR ASSIGN TO "pb1907np-l3.dat"
               ORGANIZATION IS LINE SEQUENTIAL FILE STATUS IS LR-ST.
           SELECT NF ASSIGN TO "pb1907np-n1.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS NF-ST.
       DATA DIVISION.
       FILE SECTION.
       FD  LF.
       01  LF-SHORT          PIC X(5).
       01  LF-LONG           PIC X(20).
       FD  LD
           RECORD IS VARYING IN SIZE FROM 5 TO 20 CHARACTERS
               DEPENDING ON LD-LEN.
       01  LD-SHORT          PIC X(5).
       01  LD-LONG           PIC X(20).
       FD  LR.
       01  LR-SHORT          PIC X(5).
       01  LR-LONG           PIC X(20).
       FD  NF
           RECORD IS VARYING IN SIZE FROM 5 TO 20 CHARACTERS.
       01  NF-SHORT          PIC X(5).
       01  NF-LONG           PIC X(20).
       WORKING-STORAGE SECTION.
       01  LF-ST             PIC XX.
       01  LD-ST             PIC XX.
       01  LR-ST             PIC XX.
       01  NF-ST             PIC XX.
       01  LD-LEN            PIC 99.
       01  WS-OUT            PIC X(20).
       PROCEDURE DIVISION.
           OPEN OUTPUT LF LD LR NF.
      *> L1
           MOVE ALL "Z" TO LF-LONG
           MOVE "ABCDE" TO LF-SHORT
           WRITE LF-SHORT
           DISPLAY "L1 WRITE=" LF-ST
      *> L2
           MOVE ALL "Z" TO LD-LONG
           MOVE "ABCDE" TO LD-SHORT
           MOVE 12 TO LD-LEN
           WRITE LD-SHORT
           DISPLAY "L2 WRITE=" LD-ST
      *> L3 - a 15-character record, replaced below by a 5-byte one.
           MOVE "ABCDEFGHIJKLMNO" TO LR-LONG
           WRITE LR-LONG
           DISPLAY "L3 WRITE=" LR-ST
      *> N1
           MOVE ALL "Z" TO NF-LONG
           MOVE "ABCDE" TO NF-SHORT
           WRITE NF-SHORT
           DISPLAY "N1 WRITE=" NF-ST
           CLOSE LF LD LR NF
           OPEN INPUT LF LD NF
           OPEN I-O LR
           READ LF INTO WS-OUT
           DISPLAY "L1 READ=" LF-ST " [" WS-OUT "]"
           READ LD INTO WS-OUT
           DISPLAY "L2 READ=" LD-ST " [" WS-OUT "]"
           READ NF INTO WS-OUT
           DISPLAY "N1 READ=" NF-ST " [" WS-OUT "]"
           READ LR
           MOVE ALL "Z" TO LR-LONG
           MOVE "klmno" TO LR-SHORT
           REWRITE LR-SHORT
           DISPLAY "L3 REWRITE=" LR-ST
           CLOSE LF LD NF LR
           OPEN INPUT LR
           READ LR INTO WS-OUT
           DISPLAY "L3 READ=" LR-ST " [" WS-OUT "]"
           CLOSE LR
           STOP RUN.
