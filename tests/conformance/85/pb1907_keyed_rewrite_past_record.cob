      *> kb/Work PB1907 - ISO 14.9.51.4 GR13 and 14.9.35.4 GR15 (Annex A.2
      *> items 64 and 49): when the number of bytes to be written is greater
      *> than the number of bytes in record-name-1, "the content of the bytes
      *> that extend outside the end of record-name-1 are undefined". This is
      *> the RELATIVE, INDEXED and REWRITE half of the determination that
      *> pb1907_write_past_record pins for WRITE on a sequential file
      *> (docs/CONFORMANCE.md 3, D-WRT1): a byte past record-name-1 is the
      *> record area's content at its position (13.18.33.4 GR3: every level 1
      *> entry under the FD redefines the same area), and a position no record
      *> description occupies is a space. WRITE FROM and REWRITE FROM are the
      *> same case, because GR5 a) / 14.9.35.4 GR7 a) MOVE into record-name-1
      *> first and then run the statement without FROM.
      *>
      *> EXPECTED VALUES (derived from the rule, not from a run):
      *>   R1  RECORD CONTAINS 20 (13.18.43.4 GR6): every record is 20 bytes.
      *>       WRITE RL-SHORT sends ABCDE, then area positions 6-20, which
      *>       RL-LONG filled with Z.
      *>   R2  WRITE FROM: 01234 (GR5 a) MOVE of WS-SRC into the 5-byte
      *>       RL-SHORT), then the Y that RL-LONG held.
      *>   R1' REWRITE of record 1 after the area is refilled: klmno, then X.
      *>   R2' REWRITE FROM: 01234, then the W that RL-LONG held.
      *>   X1  indexed, RECORD CONTAINS 20: KEY and ab are XF-SHORT's five
      *>       bytes; positions 6-20 are the Z that XF-LONG held.
      *>   X1' REWRITE: KEY (the record key, positions 1-3) and the W that
      *>       XF-REST put at positions 4-20 - XF-SHORT covers positions 1-5
      *>       only, and the rest of the record is the area's content.
      *>   V1  RECORD VARYING 5 TO 20 DEPENDING ON: 12 bytes, ABCDE then
      *>       area positions 6-12 (Z). A record sequential REWRITE must be
      *>       the length of the record it replaces (14.9.35.4 GR16): 12.
      *>   V1' REWRITE of those 12 bytes: vwxyz then area positions 6-12 (Q).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1907KR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RL ASSIGN TO "pb1907kr-rel.dat"
               ORGANIZATION IS RELATIVE ACCESS MODE IS RANDOM
               RELATIVE KEY IS RL-KEY FILE STATUS IS RL-ST.
           SELECT XF ASSIGN TO "pb1907kr-idx.dat"
               ORGANIZATION IS INDEXED ACCESS MODE IS DYNAMIC
               RECORD KEY IS XF-K FILE STATUS IS XF-ST.
           SELECT VF ASSIGN TO "pb1907kr-var.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS VF-ST.
       DATA DIVISION.
       FILE SECTION.
       FD  RL RECORD CONTAINS 20 CHARACTERS.
       01  RL-SHORT          PIC X(5).
       01  RL-LONG           PIC X(20).
       FD  XF RECORD CONTAINS 20 CHARACTERS.
       01  XF-SHORT.
           05 XF-SK          PIC X(3).
           05 XF-SJ          PIC X(2).
       01  XF-LONG.
           05 XF-K           PIC X(3).
           05 XF-REST        PIC X(17).
       FD  VF
           RECORD IS VARYING IN SIZE FROM 5 TO 20 CHARACTERS
               DEPENDING ON VF-LEN.
       01  VF-SHORT          PIC X(5).
       01  VF-LONG           PIC X(20).
       WORKING-STORAGE SECTION.
       01  RL-ST             PIC XX.
       01  XF-ST             PIC XX.
       01  VF-ST             PIC XX.
       01  RL-KEY            PIC 9(4).
       01  VF-LEN            PIC 99.
       01  WS-SRC            PIC X(20) VALUE "0123456789ABCDEFGHIJ".
       PROCEDURE DIVISION.
      *> Relative file: WRITE, WRITE FROM, REWRITE, REWRITE FROM.
           OPEN OUTPUT RL
           MOVE ALL "Z" TO RL-LONG
           MOVE "ABCDE" TO RL-SHORT
           MOVE 1 TO RL-KEY
           WRITE RL-SHORT
           DISPLAY "R1 WRITE=" RL-ST
           MOVE ALL "Y" TO RL-LONG
           MOVE 2 TO RL-KEY
           WRITE RL-SHORT FROM WS-SRC
           DISPLAY "R2 WRITE=" RL-ST
           CLOSE RL
           OPEN I-O RL
           MOVE 1 TO RL-KEY
           READ RL
           MOVE ALL "X" TO RL-LONG
           MOVE "klmno" TO RL-SHORT
           REWRITE RL-SHORT
           DISPLAY "R1 REWRITE=" RL-ST
           MOVE ALL "W" TO RL-LONG
           MOVE 2 TO RL-KEY
           REWRITE RL-SHORT FROM WS-SRC
           DISPLAY "R2 REWRITE=" RL-ST
           CLOSE RL
           OPEN INPUT RL
           MOVE 1 TO RL-KEY
           READ RL
           DISPLAY "R1 READ=" RL-ST " [" RL-LONG "]"
           MOVE 2 TO RL-KEY
           READ RL
           DISPLAY "R2 READ=" RL-ST " [" RL-LONG "]"
           CLOSE RL
      *> Indexed file: WRITE and REWRITE with a key shorter than the record.
           OPEN OUTPUT XF
           MOVE ALL "Z" TO XF-LONG
           MOVE "KEY" TO XF-K
           MOVE "ab" TO XF-SJ
           WRITE XF-SHORT
           DISPLAY "X1 WRITE=" XF-ST
           CLOSE XF
           OPEN I-O XF
           MOVE "KEY" TO XF-K
           READ XF
           DISPLAY "X1 READ=" XF-ST " [" XF-LONG "]"
           MOVE ALL "W" TO XF-REST
           MOVE "KEY" TO XF-K
           REWRITE XF-SHORT
           DISPLAY "X1 REWRITE=" XF-ST
           CLOSE XF
           OPEN INPUT XF
           MOVE "KEY" TO XF-K
           READ XF
           DISPLAY "X1 READ2=" XF-ST " [" XF-LONG "]"
           CLOSE XF
      *> Record sequential, DEPENDING ON: WRITE, then REWRITE of the record.
           OPEN OUTPUT VF
           MOVE ALL "Z" TO VF-LONG
           MOVE "ABCDE" TO VF-SHORT
           MOVE 12 TO VF-LEN
           WRITE VF-SHORT
           CLOSE VF
           OPEN I-O VF
           READ VF
           DISPLAY "V1 READ=" VF-ST " LEN=" VF-LEN
                   " [" VF-LONG (1:VF-LEN) "]"
           MOVE ALL "Q" TO VF-LONG
           MOVE "vwxyz" TO VF-SHORT
           MOVE 12 TO VF-LEN
           REWRITE VF-SHORT
           DISPLAY "V1 REWRITE=" VF-ST
           CLOSE VF
           OPEN INPUT VF
           READ VF
           DISPLAY "V1 READ2=" VF-ST " LEN=" VF-LEN
                   " [" VF-LONG (1:VF-LEN) "]"
           CLOSE VF
           STOP RUN.
