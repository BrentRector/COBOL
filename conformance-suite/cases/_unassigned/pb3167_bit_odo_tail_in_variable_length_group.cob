*>--- conformance-case
*> id: pb3167_bit_odo_tail_in_variable_length_group
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3167 review R1-1 - a bit OCCURS DEPENDING table whose
      *> occurrences stride whole bytes but which begins in the middle of a
      *> byte, held by a member of a VARIABLE-LENGTH group (8.5.1.12: DL is a
      *> dynamic-length item).  The member's current image and the receiving
      *> splice measure the table from its real end, never its stride count.
      *> Every value is derived from the standard, none read off a run.
      *>
      *> 13.18.1.4 GR2: "An ALIGNED clause specified for a multiple-occurrence
      *> data item applies to each occurrence of that item"; DETERMINATION
      *> (docs/CONFORMANCE.md, USAGE BIT): the occurrences are a whole number
      *> of bytes apart with filler between them and none after the last
      *> (8.5.1.6.3: filler only "for the next item").
      *> 13.18.38.4 GR8 a): a receiving table uses "only that part of the
      *> table area that is specified by the value of the data item
      *> referenced by data-name-1 at the start of the operation".
      *>
      *> LAYOUT - AG: ABA bits 0-1; ASUB(1) begins at bit 2: ASA 2, filler
      *>   3-7, ATA 8-12, so ASUB is 11 positions and strides 16: ASUB(2) is
      *>   bits 18-28, ASUB(3) 34-44.  AG at its maximum is 45 bits, 6
      *>   characters; at N = 0 it is ABA's 2 bits, 1 character; at N = 2 it
      *>   ends at bit 29 (2 + 16 + 11), 4 characters.  So LENGTH(R) is
      *>   2 + 1 = 3 at N = 0 and 2 + 4 = 6 at N = 2 (15.50.4 rule 9).
      *> (A) N = 0, M = 0: R's image is "ab" and AG's one character, so R2
      *>   receives ABA2 = 11.
      *> (B) N = 2, M = 2 at the start of the MOVE: R2 takes occurrences 1
      *>   and 2 (ASA 1, ATA 10101 and 01010) and keeps its own occurrence 3
      *>   (ASA2 0, ATA2 11111), which begins at bit 34.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3167VT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 0.
       01 M PIC 9 VALUE 0.
       01 T PIC 9(4).
       01 R.
          05 DL PIC X DYNAMIC LENGTH.
          05 AG.
             10 ABA PIC 1(2) USAGE BIT.
             10 ASUB GROUP-USAGE BIT OCCURS 0 TO 3 DEPENDING ON N.
                15 ASA PIC 1 USAGE BIT.
                15 ATA PIC 1(5) USAGE BIT ALIGNED.
       01 R2.
          05 DL2 PIC X DYNAMIC LENGTH.
          05 AG2.
             10 ABA2 PIC 1(2) USAGE BIT.
             10 ASUB2 GROUP-USAGE BIT OCCURS 0 TO 3 DEPENDING ON M.
                15 ASA2 PIC 1 USAGE BIT.
                15 ATA2 PIC 1(5) USAGE BIT ALIGNED.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "ab" TO DL
           MOVE B"11" TO ABA
           MOVE FUNCTION LENGTH(R) TO T
           DISPLAY "A LEN " T
           MOVE R TO R2
           DISPLAY "A " DL2 " " ABA2
           MOVE 2 TO N
           MOVE B"1" TO ASA(1) ASA(2)
           MOVE B"10101" TO ATA(1)
           MOVE B"01010" TO ATA(2)
           MOVE FUNCTION LENGTH(R) TO T
           DISPLAY "B LEN " T
           MOVE 3 TO M
           MOVE B"0" TO ASA2(1) ASA2(2) ASA2(3)
           MOVE B"11111" TO ATA2(1) ATA2(2) ATA2(3)
           MOVE 2 TO M
           MOVE R TO R2
           MOVE 3 TO M
           DISPLAY "B " ABA2 " " ASA2(1) " " ATA2(1) " " ASA2(2) " "
               ATA2(2) " " ASA2(3) " " ATA2(3)
           STOP RUN.
