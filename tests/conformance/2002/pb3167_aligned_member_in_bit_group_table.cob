      *> kb/Work PB3167 - a TABLE whose element is a bit group holding an
      *> ALIGNED member: every occurrence's ALIGNED member is on a byte of the
      *> record, and a bit group whose first item is ALIGNED begins where that
      *> item does.  Every value is derived from the standard, none read off a
      *> run.
      *>
      *> 13.18.1.4 GR1: an ALIGNED clause "causes the subject of the entry to be
      *> aligned on the first bit of the first available byte boundary"; GR2:
      *> "An ALIGNED clause specified for a multiple-occurrence data item applies
      *> to each occurrence of that item" - TA below is subordinate to an OCCURS
      *> entry, so it has an occurrence per occurrence of SUB (13.18.63.4 9) says
      *> the same of a VALUE: "every occurrence of the associated data item").
      *> 13.18.38.4 7): the subject of an OCCURS entry has one length ("This does
      *> not imply that the length of the subject of the entry is variable").
      *> DETERMINATION (docs/CONFORMANCE.md, USAGE BIT): such an element's
      *> occurrences are a whole number of bytes apart, every one laid out as the
      *> first, with implicit filler between them (13.18.55.4 8) b) generates the
      *> FILLER of a synchronized table "for each occurrence"); the last one has
      *> none after it (8.5.1.6.3: filler only "for the next item").
      *> 8.5.1.6.3: "the alignment of the start of a group item and the alignment
      *> of the first item within that group, when the first item is a bit data
      *> item, are at the same bit position in storage" - both ways.
      *> 15.50.4 rule 1 gives a bit group's LENGTH in boolean positions and rule 5
      *> counts implicit filler; rule 9 rounds a partly used character up
      *> (BYTE-LENGTH, 15.14.4 rule 4, the same).
      *>
      *> LAYOUT 1 - BG: BA 0-1; SUB(1) begins at bit 2 (8.5.1.6.3, next bit):
      *>   SA 2, filler 3-7, TA 8-12: SUB is 11 positions, stride 16, so
      *>   SUB(2) is bits 18-28 (TA 24-28), SUB(3) 34-44 (TA 40-44); filler
      *>   13-17 and 29-33 between occurrences.  BG = 2 + 16 + 16 + 11 = 45,
      *>   BYTE-LENGTH 6.  BA=11 SA=1 TA=10101 01010 11111 give
      *>   11 10000010101 00000 10000001010 00000 10000011111.
      *> LAYOUT 2 - PG: the element's FIRST item is ALIGNED, so each occurrence
      *>   begins on a byte: TA(k) at 8(k-1), SA(k) right after it; 6 + 8 + 8 =
      *>   22 positions: 10101 1 00 01010 1 00 11111 1.
      *> LAYOUT 3 - QG: QSUB's first item QTA is ALIGNED, so QSUB begins at
      *>   bit 8 with it (the filler 2-7 is QG's): QSUB is 5 positions, QG 15
      *>   (QC follows at 13-14): 11 000000 10101 01.
      *> LAYOUT 4 - CG, the LAYOUT 1 members (2 occurrences) in an alphanumeric
      *>   group, then CZ PIC X on the next character: bytes 11100000 10101000
      *>   00100000 01010000 'Z' = 224 168 32 80 90 (ORD one more: 225 169 33 81
      *>   91), and a space (R is X(6)), 33; LENGTH(CG) = 5.  The VALUE of CSA
      *>   is given to every occurrence (13.18.63.4 9)).
      *> LAYOUT 5 - OG / AG, the same element as an OCCURS 1 TO 3 DEPENDING
      *>   table: at N=2 OG is 2 + 16 + 11 = 29 positions, BYTE-LENGTH 4;
      *>   AG (alphanumeric) is 4 characters, and 2 at N=1 (13 positions).
      *> LAYOUT 6 - ATA(2) is on a byte (bit 24), so it may be passed BY
      *>   REFERENCE (14.9.4.3 6): "aligned on a byte boundary").
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3167AL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BG GROUP-USAGE BIT.
          05 BA PIC 1(2) USAGE BIT.
          05 SUB GROUP-USAGE BIT OCCURS 3.
             10 SA PIC 1 USAGE BIT.
             10 TA PIC 1(5) USAGE BIT ALIGNED.
       01 PG GROUP-USAGE BIT.
          05 PSUB GROUP-USAGE BIT OCCURS 3.
             10 PTA PIC 1(5) USAGE BIT ALIGNED.
             10 PSA PIC 1 USAGE BIT.
       01 QG GROUP-USAGE BIT.
          05 QA PIC 1(2) USAGE BIT.
          05 QSUB GROUP-USAGE BIT.
             10 QTA PIC 1(5) USAGE BIT ALIGNED.
          05 QC PIC 1(2) USAGE BIT.
       01 CG.
          05 CBA PIC 1(2) USAGE BIT.
          05 CSUB GROUP-USAGE BIT OCCURS 2.
             10 CSA PIC 1 USAGE BIT VALUE B"1".
             10 CTA PIC 1(5) USAGE BIT ALIGNED.
          05 CZ PIC X VALUE "Z".
       01 N PIC 9 VALUE 2.
       01 OG GROUP-USAGE BIT.
          05 OBA PIC 1(2) USAGE BIT.
          05 OSUB GROUP-USAGE BIT OCCURS 1 TO 3 DEPENDING ON N.
             10 OSA PIC 1 USAGE BIT.
             10 OTA PIC 1(5) USAGE BIT ALIGNED.
       01 AG.
          05 ABA PIC 1(2) USAGE BIT.
          05 ASUB GROUP-USAGE BIT OCCURS 1 TO 3 DEPENDING ON N.
             10 ASA PIC 1 USAGE BIT.
             10 ATA PIC 1(5) USAGE BIT ALIGNED.
       01 R PIC X(6).
       01 T PIC 9(4).
       01 U PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           MOVE B"11" TO BA
           MOVE B"1" TO SA(1) SA(2) SA(3)
           MOVE B"10101" TO TA(1)
           MOVE B"01010" TO TA(2)
           MOVE B"11111" TO TA(3)
           DISPLAY "BG " BG
           MOVE FUNCTION LENGTH(BG) TO T
           MOVE FUNCTION BYTE-LENGTH(BG) TO U
           DISPLAY "BG " T " BYTES " U
           MOVE FUNCTION LENGTH(SUB(1)) TO T
           MOVE FUNCTION LENGTH(SUB(2)) TO U
           DISPLAY "SUB " T " " U
           DISPLAY "SUB2 " SUB(2) " TA " TA(2) " " TA(3)
           MOVE B"1" TO PSA(1) PSA(2) PSA(3)
           MOVE B"10101" TO PTA(1)
           MOVE B"01010" TO PTA(2)
           MOVE B"11111" TO PTA(3)
           MOVE FUNCTION LENGTH(PG) TO T
           DISPLAY "PG " PG " " T
           MOVE B"11" TO QA
           MOVE B"10101" TO QTA
           MOVE B"01" TO QC
           MOVE FUNCTION LENGTH(QG) TO T
           MOVE FUNCTION LENGTH(QSUB) TO U
           DISPLAY "QG " QG " " T " QSUB " QSUB " " U
           MOVE B"11" TO CBA
           MOVE B"10101" TO CTA(1)
           MOVE B"01010" TO CTA(2)
           MOVE CG TO R
           DISPLAY "CG " FUNCTION ORD(R(1:1)) " " FUNCTION ORD(R(2:1))
               " " FUNCTION ORD(R(3:1)) " " FUNCTION ORD(R(4:1))
               " " FUNCTION ORD(R(5:1)) " " FUNCTION ORD(R(6:1))
           MOVE FUNCTION LENGTH(CG) TO T
           DISPLAY "CG " T " " CSA(1) CSA(2) " " CZ
           MOVE B"11" TO OBA ABA
           MOVE B"1" TO OSA(1) OSA(2) ASA(1) ASA(2)
           MOVE B"10101" TO OTA(1) ATA(1)
           MOVE B"01010" TO OTA(2) ATA(2)
           MOVE FUNCTION LENGTH(OG) TO T
           MOVE FUNCTION BYTE-LENGTH(OG) TO U
           DISPLAY "OG " OG " " T " BYTES " U
           MOVE AG TO R
           DISPLAY "AG " FUNCTION ORD(R(1:1)) " " FUNCTION ORD(R(2:1))
               " " FUNCTION ORD(R(3:1)) " " FUNCTION ORD(R(4:1))
               " " FUNCTION ORD(R(5:1)) " " FUNCTION ORD(R(6:1))
           MOVE FUNCTION LENGTH(AG) TO T
           MOVE 1 TO N
           MOVE FUNCTION LENGTH(AG) TO U
           DISPLAY "AG " T " " U
           CALL "PB3167AS" USING BY REFERENCE ATA(2)
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3167AS.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC 1(5) USAGE BIT.
       PROCEDURE DIVISION USING L.
       ENTRY-POINT.
           DISPLAY "BYREF " L
           GOBACK.
       END PROGRAM PB3167AS.
       END PROGRAM PB3167AL.
