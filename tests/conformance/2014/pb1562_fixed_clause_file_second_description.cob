      *> ISO 1989:2014 section 13.18.43 (RECORD clause Format 1) and section 9.1.6 - the train-review finding on
      *> kb/Work PB1562. Format 1 "is used to specify fixed-length records" (GR6: "Integer-1 specifies the number of
      *> bytes contained in each record in the file") and section 9.1.6 makes the record TYPE and SIZE fixed file
      *> attributes: every program that uses the file describes the same ones. A second program that describes the
      *> same file as RECORD CONTAINS 20 over PIC X(20) has the SAME attributes as the first, so it must read the
      *> same bytes - the file cannot be framed as variable-length records, nor carry an extent table, just because
      *> the first program's record holds DYNAMIC LENGTH members. (GR2 lets the physical record differ "due to
      *> control information required by the operating environment", but that is information the file's TYPE
      *> and SIZE determine, not one program's description of its records: the second program's record has none.)
      *>
      *> The standard leaves the physical form of a dynamic-length member to the implementor (section 8.5.1.10.2,
      *> 8.5.1.10.3; DOC-A.1-63), and docs/CONFORMANCE.md D-FRA (vi) states what this implementation does for a file of
      *> FIXED-length records: each member sits at the position it has when it holds its MAXIMUM size (the LIMIT,
      *> section 13.18.43.4 GR8 b)), padded with spaces - here A at bytes 1-9, KY at 10-12, C at 13-20 - and a
      *> READ takes each member back at that width and drops the padding (a space in a fixed-size field is padding,
      *> as for a line sequential record, section 14.9.51.4 GR21). Every organization carries it the same way.
      *>
      *> EXPECTED OUTPUT, DERIVED from that layout:
      *>  "AAA" "KEY" "CCCC"   -> "AAA" + 6 spaces + "KEY" + "CCCC" + 4 spaces  (20 bytes)
      *>  "B" "K2K" "DDDDDD"   -> "B" + 8 spaces + "K2K" + "DDDDDD" + 2 spaces  (20 bytes)
      *> and the writing program reads every member back at the length it wrote. A REWRITE of the first record
      *> with "WXYZ" "NEW" "Q" replaces the same 20 bytes. The relative and indexed files open under the plain
      *> description with I-O status 00 - the record key of the writing description (KY) is the bytes 10-12 the
      *> plain description names (PK) - and a dynamic-length ELEMENTARY record in a fixed file is its content
      *> padded to integer-1, read back without the padding.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1562SD.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SW ASSIGN TO "pb1562sd-seq.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS ST.
           SELECT SP ASSIGN TO "pb1562sd-seq.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS ST.
           SELECT RW ASSIGN TO "pb1562sd-rel.dat"
               ORGANIZATION IS RELATIVE ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST.
           SELECT RP ASSIGN TO "pb1562sd-rel.dat"
               ORGANIZATION IS RELATIVE ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST.
           SELECT XW ASSIGN TO "pb1562sd-idx.dat"
               ORGANIZATION IS INDEXED ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS KY FILE STATUS IS ST.
           SELECT XP ASSIGN TO "pb1562sd-idx.dat"
               ORGANIZATION IS INDEXED ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS PK FILE STATUS IS ST.
           SELECT DW ASSIGN TO "pb1562sd-dyn.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS ST.
           SELECT DP ASSIGN TO "pb1562sd-dyn.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS ST.
       DATA DIVISION.
       FILE SECTION.
       FD  SW RECORD CONTAINS 20 CHARACTERS.
       01  SWR.
           05 SA  PIC X DYNAMIC LENGTH LIMIT 9.
           05 SK  PIC X(3).
           05 SC  PIC X DYNAMIC LENGTH LIMIT 8.
       FD  SP RECORD CONTAINS 20 CHARACTERS.
       01  SPR PIC X(20).
       FD  RW RECORD CONTAINS 20 CHARACTERS.
       01  RWR.
           05 RA  PIC X DYNAMIC LENGTH LIMIT 9.
           05 RK  PIC X(3).
           05 RC  PIC X DYNAMIC LENGTH LIMIT 8.
       FD  RP RECORD CONTAINS 20 CHARACTERS.
       01  RPR PIC X(20).
       FD  XW RECORD CONTAINS 20 CHARACTERS.
       01  XWR.
           05 XA  PIC X DYNAMIC LENGTH LIMIT 9.
           05 KY  PIC X(3).
           05 XC  PIC X DYNAMIC LENGTH LIMIT 8.
       FD  XP RECORD CONTAINS 20 CHARACTERS.
       01  XPR.
           05 FILLER PIC X(9).
           05 PK  PIC X(3).
           05 FILLER PIC X(8).
       FD  DW RECORD CONTAINS 12 CHARACTERS.
       01  DWR PIC X DYNAMIC LENGTH LIMIT 12.
       FD  DP RECORD CONTAINS 12 CHARACTERS.
       01  DPR PIC X(12).
       WORKING-STORAGE SECTION.
       01  ST PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT SW.
           MOVE "AAA" TO SA. MOVE "KEY" TO SK. MOVE "CCCC" TO SC.
           WRITE SWR.
           MOVE "B" TO SA. MOVE "K2K" TO SK. MOVE "DDDDDD" TO SC.
           WRITE SWR.
           CLOSE SW.
           OPEN INPUT SW.
           MOVE "ZZZ" TO SA. MOVE "???" TO SK. MOVE "Y" TO SC.
           READ SW.
           DISPLAY "S1 A=[" SA "] K=[" SK "] C=[" SC "] " ST.
           READ SW.
           DISPLAY "S2 A=[" SA "] K=[" SK "] C=[" SC "] " ST.
           CLOSE SW.
           OPEN INPUT SP.
           READ SP.
           DISPLAY "SP1 [" SPR "] " ST.
           READ SP.
           DISPLAY "SP2 [" SPR "] " ST.
           CLOSE SP.
           OPEN I-O SW.
           READ SW.
           MOVE "WXYZ" TO SA. MOVE "NEW" TO SK. MOVE "Q" TO SC.
           REWRITE SWR.
           DISPLAY "SRW " ST.
           CLOSE SW.
           OPEN INPUT SP.
           READ SP.
           DISPLAY "SP1 [" SPR "] " ST.
           CLOSE SP.
           OPEN OUTPUT RW.
           MOVE "AAA" TO RA. MOVE "KEY" TO RK. MOVE "CCCC" TO RC.
           WRITE RWR.
           CLOSE RW.
           OPEN INPUT RW.
           MOVE "ZZZ" TO RA. MOVE "???" TO RK. MOVE "Y" TO RC.
           READ RW.
           DISPLAY "R1 A=[" RA "] K=[" RK "] C=[" RC "] " ST.
           CLOSE RW.
           OPEN INPUT RP.
           DISPLAY "RP-OPEN " ST.
           READ RP.
           DISPLAY "RP1 [" RPR "] " ST.
           CLOSE RP.
           OPEN OUTPUT XW.
           MOVE "AAA" TO XA. MOVE "KEY" TO KY. MOVE "CCCC" TO XC.
           WRITE XWR.
           MOVE "B" TO XA. MOVE "LOW" TO KY. MOVE "DDDDDD" TO XC.
           WRITE XWR.
           CLOSE XW.
           OPEN INPUT XW.
           MOVE "ZZZ" TO XA. MOVE "???" TO KY. MOVE "Y" TO XC.
           READ XW.
           DISPLAY "X1 A=[" XA "] K=[" KY "] C=[" XC "] " ST.
           READ XW.
           DISPLAY "X2 A=[" XA "] K=[" KY "] C=[" XC "] " ST.
           CLOSE XW.
           OPEN INPUT XP.
           DISPLAY "XP-OPEN " ST.
           READ XP.
           DISPLAY "XP1 [" XPR "] KEY=[" PK "] " ST.
           CLOSE XP.
           OPEN OUTPUT DW.
           MOVE "HELLO" TO DWR.
           WRITE DWR.
           MOVE "" TO DWR.
           WRITE DWR.
           CLOSE DW.
           OPEN INPUT DW.
           MOVE "JUNK" TO DWR.
           READ DW.
           DISPLAY "D1 [" DWR "] LEN=" FUNCTION LENGTH(DWR) " " ST.
           READ DW.
           DISPLAY "D2 [" DWR "] LEN=" FUNCTION LENGTH(DWR) " " ST.
           CLOSE DW.
           OPEN INPUT DP.
           READ DP.
           DISPLAY "DP1 [" DPR "] " ST.
           CLOSE DP.
           STOP RUN.
