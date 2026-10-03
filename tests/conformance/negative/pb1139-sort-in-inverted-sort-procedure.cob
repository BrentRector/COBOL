      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1139 (and PB812) - THE PROCEDURE RANGE IS ASKED OF THE TEXT, NOT OF A NUMERIC INTERVAL.
      *>   cite.py --check 14.9.40.3 "A SORT statement shall not appear in imperative-statement-1 of an
      *>     exception-checking PERFORM statement, in an input or output procedure, or in a declarative procedure."
      *>     -> OK §14.9.40.3 3)
      *> INPUT PROCEDURE IS B1 THRU A1 names an INVERTED range (A1 stands before B1; §14.9.28.4 GR6 permits it). The
      *> file SORT written in A1 is in the input procedure; a Start..End interval over the pcs is empty for this pair,
      *> which is how the statement escaped the ban. PcRange.Spans is the one lexical membership test.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1139D.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SW ASSIGN TO "PB1139D.tmp".
           SELECT SW2 ASSIGN TO "PB1139Ds2.tmp".
           SELECT F1 ASSIGN TO "PB1139D1.dat".
           SELECT F2 ASSIGN TO "PB1139D2.dat".
           SELECT F3 ASSIGN TO "PB1139D3.dat".
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
               INPUT PROCEDURE IS B1 THRU A1
               GIVING F3
           STOP RUN.
       A1.
           SORT SW2 ASCENDING KEY SK2 USING F1 GIVING F2.
       B1.
           CONTINUE.
