      *> kb/Work PB1277 -- a record's size is counted in BYTES, never in character
      *> positions. Until PB1277 the minimum and maximum record sizes were summed
      *> from the carrier's character positions, so a national position (two bytes,
      *> determination D-N1) counted once, and the minimum re-summed each USAGE BIT
      *> leaf as a byte of its own: legal source was refused (FN, SN below), a
      *> national file accepted records half its size (FV), and the bit record had
      *> minimum 2 over maximum 1, so no WRITE of it could succeed (FB).
      *>
      *> RULES (each run through scripts/spec/cite.py --check) --
      *>   13.18.43.4 GR3  "the number of bytes required to store the logical record,
      *>                   regardless of the types of characters used".
      *>   13.18.43.4 GR4  "the record size includes the entire byte in which that
      *>                   data item ends".
      *>   13.18.43.4 GR8, GR9, GR10 -- the unstated minimum and maximum are "the
      *>                   least" / "the greatest number of bytes described for a
      *>                   record in that file".
      *>   13.18.43.4 GR14 a) a record to be written outside [integer-2, integer-3]
      *>                   makes the WRITE unsuccessful: I-O status 44 (9.1.13.7 4)).
      *>   13.4.6.4 GR1    "The number of characters is specified in terms of bytes"
      *>                   -- the same rule for a sort-merge file description entry.
      *>
      *> DERIVATION --
      *>   FN   RN = PIC X(10) + PIC N(5) = 10 + 10 = 20 bytes, inside FROM 16 TO 40
      *>        (13.18.43.3 SR4), so the program compiles; WRITE RN: FN=00.
      *>   FV   RV = PIC N(10) = 20 bytes; no FROM/TO, so min = max = 20 (GR9/GR10).
      *>        L=12 is below 20: FV12=44. L=20: FV20=00.
      *>   FB   RB = two PIC 1(3) USAGE BIT items = 6 bits, so 1 byte (GR4); min =
      *>        max = 1. LB=1: FB1=00. LB=2 is above 1: FB2=44.
      *>   FA   FA-X (15 bytes) and FA-N (PIC N(10) = 20 bytes) are implicit
      *>        redefinitions of the same area (13.18.33.4 GR3), whose implied
      *>        Format 1 size is the LARGEST record, 20 bytes (13.18.43.4 GR5 a).
      *>        FA-N written, then read back over N"ZZZZZZZZZZ": the whole record
      *>        is made available, FA=00 FA-N=[ABCDEFGHIJ]. (Measured in character
      *>        positions FA-X looked larger, 15 > 10, and the READ went through
      *>        its 15-byte view: FA-N=[ABCDEFGZZZ].)
      *>   SN   SNR = PIC N(3) + PIC N(1) = 8 bytes, inside FROM 6 TO 8, and the
      *>        key NK (6 bytes) lies within the first 6 bytes (14.9.40.3 SR6 g),
      *>        so the program compiles; two records released, both returned:
      *>        SORTED=2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1277RB.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FN ASSIGN TO "pb1277rb1.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS.
           SELECT FV ASSIGN TO "pb1277rb2.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS.
           SELECT FB ASSIGN TO "pb1277rb3.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS.
           SELECT FA ASSIGN TO "pb1277rb5.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS.
           SELECT SN ASSIGN TO "pb1277rb4.tmp".
       DATA DIVISION.
       FILE SECTION.
       FD  FN RECORD IS VARYING IN SIZE FROM 16 TO 40 CHARACTERS.
       01  RN.
           05 RN-A PIC X(10).
           05 RN-C PIC N(5).
       FD  FV RECORD IS VARYING IN SIZE DEPENDING ON L.
       01  RV PIC N(10).
       FD  FB RECORD IS VARYING IN SIZE DEPENDING ON LB.
       01  RB.
           05 RB-1 PIC 1(3) USAGE BIT.
           05 RB-2 PIC 1(3) USAGE BIT.
       FD  FA.
       01  FA-X PIC X(15).
       01  FA-N PIC N(10).
       SD  SN RECORD IS VARYING IN SIZE FROM 6 TO 8 CHARACTERS.
       01  SNR.
           05 NK PIC N(3).
           05 NT PIC N(1).
       WORKING-STORAGE SECTION.
       01  FS PIC XX.
       01  L PIC 99.
       01  LB PIC 99.
       01  N PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT FN
           MOVE "ABCDEFGHIJ" TO RN-A
           MOVE N"KLMNO" TO RN-C
           WRITE RN
           DISPLAY "FN=" FS
           CLOSE FN
           OPEN OUTPUT FV
           MOVE N"ABCDEFGHIJ" TO RV
           MOVE 12 TO L
           WRITE RV
           DISPLAY "FV12=" FS
           MOVE 20 TO L
           WRITE RV
           DISPLAY "FV20=" FS
           CLOSE FV
           OPEN OUTPUT FB
           MOVE B"101" TO RB-1
           MOVE B"010" TO RB-2
           MOVE 1 TO LB
           WRITE RB
           DISPLAY "FB1=" FS
           MOVE 2 TO LB
           WRITE RB
           DISPLAY "FB2=" FS
           CLOSE FB
           OPEN OUTPUT FA
           MOVE N"ABCDEFGHIJ" TO FA-N
           WRITE FA-N
           CLOSE FA
           OPEN INPUT FA
           MOVE N"ZZZZZZZZZZ" TO FA-N
           READ FA
           DISPLAY "FA=" FS " FA-N=[" FA-N "]"
           CLOSE FA
           SORT SN ON ASCENDING KEY NK
               INPUT PROCEDURE IS REL-SN
               OUTPUT PROCEDURE IS RET-SN
           DISPLAY "SORTED=" N
           STOP RUN.
       REL-SN.
           MOVE N"B" TO NT
           MOVE N"BBB" TO NK
           RELEASE SNR
           MOVE N"AAA" TO NK
           RELEASE SNR.
       RET-SN.
           RETURN SN AT END GO TO RET-SN-EXIT END-RETURN
           ADD 1 TO N
           GO TO RET-SN.
       RET-SN-EXIT.
           EXIT.
