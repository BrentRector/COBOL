      *> reject-at: 2023
      *> kb/Work PB3254 - THE MERGE ARM READS THE SAME LEXICAL SPAN (PcRange.Spans) AS THE SORT ARM.
      *> The inverted THRU range is the PERFORM statement's (§14.9.28.4 GR6), which a sort procedure shares:
      *>   cite.py --check 14.9.28.4 "There is no necessary relationship between procedure-name-1 and
      *>     procedure-name-2" -> OK §14.9.28.4 6
      *> OUTPUT PROCEDURE IS S-B THRU S-A names SECTIONS with S-A (A1, A2) BEFORE S-B (B1, B2, B3). The MERGE in B2
      *> lies inside procedure-name-1 and is COBOL-2023's removal (COBOLNET1572, the sibling of SORT's SR3 arm); the
      *> composed pair (B1, A2) forgot B2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3254N3.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SW ASSIGN TO "PB3254N3.tmp".
           SELECT SW2 ASSIGN TO "PB3254N3s2.tmp".
           SELECT F1 ASSIGN TO "PB3254N31.dat".
           SELECT F2 ASSIGN TO "PB3254N32.dat".
           SELECT F3 ASSIGN TO "PB3254N33.dat".
       DATA DIVISION.
       FILE SECTION.
       SD SW.
       01 SR.
          05 SK PIC X(4).
       SD SW2.
       01 SR2.
          05 SK2 PIC X(4).
       FD F1.
       01 R1 PIC X(4).
       FD F2.
       01 R2 PIC X(4).
       FD F3.
       01 R3 PIC X(4).
       PROCEDURE DIVISION.
       MAIN SECTION.
       M1.
           SORT SW ASCENDING KEY SK
               USING F1
               OUTPUT PROCEDURE IS S-B THRU S-A
           STOP RUN.
       S-A SECTION.
       A1.
           CONTINUE.
       A2.
           CONTINUE.
       S-B SECTION.
       B1.
           CONTINUE.
       B2.
           MERGE SW2 ASCENDING KEY SK2 USING F1 F2 GIVING F3.
       B3.
           CONTINUE.
