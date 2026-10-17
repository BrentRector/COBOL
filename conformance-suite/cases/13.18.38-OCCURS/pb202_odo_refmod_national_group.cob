*>--- conformance-case
*> id: pb202_odo_refmod_national_group
*> rules: [GR-13.18.38.4-8]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB202 - the NATIONAL-channel twin of 85/pb202_odo_refmod_group_direction. A national group
      *> reference-modified is operated on in national positions (8.4.3.3.4 GR5a), and ISO 1989:2023 13.18.38.4
      *> GR8b applies unchanged: as a SENDING operand a depending-inside group uses only its current extent, as a
      *> RECEIVING operand "the maximum length of the group will be used".
      *>   R1 = cddd   N(4:) at count 2: 1 + 2 x 3 = 7 national positions, from position 4 to the end.
      *> RECEIVING N(2:9) at count 2 writes positions 2-10 of the 16-position maximum; 11-16 (fff ggg) are outside
      *> the unique data item and unmodified:
      *>   R2 = 5XYZXYZXYZfffggg
      *> Before the fix R2 lost positions 11-16 (the splice base was read at the current extent and the
      *> maximum-length store wrote the short image back over the whole group).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB202ODORMNAT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N GROUP-USAGE NATIONAL.
          05 NM PIC 9 USAGE NATIONAL VALUE 5.
          05 NT OCCURS 1 TO 5 TIMES DEPENDING ON NM PIC N(3).
       PROCEDURE DIVISION.
       P1.
           MOVE N"ccc" TO NT (1)  MOVE N"ddd" TO NT (2)
           MOVE N"eee" TO NT (3)  MOVE N"fff" TO NT (4)
           MOVE N"ggg" TO NT (5)
           MOVE 2 TO NM
           DISPLAY "R1=" N (4:)
           MOVE N"XYZXYZXYZ" TO N (2:9)
           MOVE 5 TO NM
           DISPLAY "R2=" N
           STOP RUN.
