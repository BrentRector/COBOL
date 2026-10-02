      *> kb/Work PB1907 - ISO 14.9.51.4 GR13 (Annex A.2 item 64): when the
      *> number of bytes to be written to the file is greater than the
      *> number of bytes in record-name-1, "the content of the bytes that
      *> extend outside the end of record-name-1 are undefined", and 4.4 2)
      *> makes a run unit that allows it a conforming one.
      *>
      *> The NUMBER of bytes is the standard's, not a choice: 13.18.43.4
      *> GR13 a) takes it from data-name-1 of RECORD VARYING ... DEPENDING
      *> ON, and GR6 makes every record of a RECORD CONTAINS integer-1 file
      *> integer-1 bytes long. Each READ below reports that length.
      *>
      *> The CONTENT past record-name-1 is WiseOwl COBOL's documented
      *> determination (docs/CONFORMANCE.md 3, D-WRT1): the bytes come from
      *> the record area at those positions - the storage every record
      *> description of the file shares (13.18.33.4 GR3) - and a position
      *> that no record description of the file occupies is a space. It is
      *> pinned here so the determination cannot drift silently. Each WRITE
      *> refills the long record first, so no line depends on what a WRITE
      *> leaves behind in the area (14.9.51.4 GR4).
      *>
      *> EXPECTED VALUES (derived from the rule, not from a run):
      *>   V1  12 bytes (GR13 a): ABCDE, then positions 6-12 of the area,
      *>       which VF-LONG filled with Z.
      *>   V2  WRITE FROM: GR5 a) MOVEs WS-SRC into VF-SHORT (5 bytes,
      *>       01234), then GR5 b) the same WRITE: 9 bytes, 01234 then
      *>       positions 6-9 of the area, which VF-LONG filled with Y.
      *>   F1  20 bytes (GR6): ABCDE then positions 6-20, Z.
      *>   F2  20 bytes: 01234 then Y.
      *>   G1  20 bytes, but no record description of GF covers positions
      *>       6-20: they are spaces.
      *>   I1  no RECORD clause: 13.18.43.4 GR5 a) makes the implied Format 1
      *>       integer-1 the largest record description, 20, so the record
      *>       is 20 bytes: ABCDE then Z.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1907WP.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT VF ASSIGN TO "pb1907wp-v.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS VF-ST.
           SELECT FF ASSIGN TO "pb1907wp-f.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FF-ST.
           SELECT GF ASSIGN TO "pb1907wp-g.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS GF-ST.
           SELECT IF1 ASSIGN TO "pb1907wp-i.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS IF-ST.
       DATA DIVISION.
       FILE SECTION.
       FD  VF
           RECORD IS VARYING IN SIZE FROM 5 TO 20 CHARACTERS
               DEPENDING ON VF-LEN.
       01  VF-SHORT          PIC X(5).
       01  VF-LONG           PIC X(20).
       FD  FF
           RECORD CONTAINS 20 CHARACTERS.
       01  FF-SHORT          PIC X(5).
       01  FF-LONG           PIC X(20).
       FD  GF
           RECORD CONTAINS 20 CHARACTERS.
       01  GF-ONLY           PIC X(5).
       FD  IF1.
       01  IF-SHORT          PIC X(5).
       01  IF-LONG           PIC X(20).
       WORKING-STORAGE SECTION.
       01  VF-ST             PIC XX.
       01  FF-ST             PIC XX.
       01  GF-ST             PIC XX.
       01  IF-ST             PIC XX.
       01  VF-LEN            PIC 99.
       01  WS-SRC            PIC X(20) VALUE "0123456789ABCDEFGHIJ".
       01  WS-GF             PIC X(20).
       PROCEDURE DIVISION.
           OPEN OUTPUT VF FF GF IF1.
      *> V1 - variable: DEPENDING ON asks for 12 bytes, VF-SHORT has 5.
           MOVE ALL "Z" TO VF-LONG
           MOVE "ABCDE" TO VF-SHORT
           MOVE 12 TO VF-LEN
           WRITE VF-SHORT
           DISPLAY "V1 WRITE=" VF-ST " VF-LEN=" VF-LEN
      *> V2 - WRITE FROM: GR5 a) MOVEs WS-SRC into VF-SHORT (5 bytes).
           MOVE ALL "Y" TO VF-LONG
           MOVE 9 TO VF-LEN
           WRITE VF-SHORT FROM WS-SRC
           DISPLAY "V2 WRITE=" VF-ST " VF-LEN=" VF-LEN
      *> F1 - fixed: every record is 20 bytes, FF-SHORT has 5.
           MOVE ALL "Z" TO FF-LONG
           MOVE "ABCDE" TO FF-SHORT
           WRITE FF-SHORT
           DISPLAY "F1 WRITE=" FF-ST
      *> F2 - fixed, WRITE FROM.
           MOVE ALL "Y" TO FF-LONG
           WRITE FF-SHORT FROM WS-SRC
           DISPLAY "F2 WRITE=" FF-ST
      *> G1 - fixed, and no record description covers bytes 6-20.
           MOVE "QRSTU" TO GF-ONLY
           WRITE GF-ONLY
           DISPLAY "G1 WRITE=" GF-ST
      *> I1 - no RECORD clause: the implied Format 1 is 20 bytes.
           MOVE ALL "Z" TO IF-LONG
           MOVE "ABCDE" TO IF-SHORT
           WRITE IF-SHORT
           DISPLAY "I1 WRITE=" IF-ST
           CLOSE VF FF GF IF1
           OPEN INPUT VF FF GF IF1
           READ VF
           DISPLAY "V1 READ=" VF-ST " LEN=" VF-LEN
                   " [" VF-LONG (1:VF-LEN) "]"
           READ VF
           DISPLAY "V2 READ=" VF-ST " LEN=" VF-LEN
                   " [" VF-LONG (1:VF-LEN) "]"
           READ FF
           DISPLAY "F1 READ=" FF-ST " [" FF-LONG "]"
           READ FF
           DISPLAY "F2 READ=" FF-ST " [" FF-LONG "]"
           READ GF INTO WS-GF
           DISPLAY "G1 READ=" GF-ST " [" WS-GF "]"
           READ IF1
           DISPLAY "I1 READ=" IF-ST " [" IF-LONG "]"
           CLOSE VF FF GF IF1
           STOP RUN.
