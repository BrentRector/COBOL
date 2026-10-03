      *> reject-at: 2002 2014 2023
      *> kb/Work PB1173 — A FILE SORT KEY OF CLASS POINTER IS THE SOURCE'S ERROR, NOT THE COMPILER'S GAP.
      *>   cite.py --check 14.9.40.3 "Key data items shall not be of the class boolean, message-tag, object, or
      *>     pointer." -> OK §14.9.40.3 6) c)
      *> SPR is a level-1 USAGE POINTER record (the only place a pointer may be declared in an SD, §13.18.60.3 SR14).
      *> A record with a pointer leaf has no image the sort store can hold, which the binder used to answer first
      *> (a COBOLNET1756 deferral that aborted the run unit) — hiding that the KEY is illegal at every edition.
      *> The key rules are asked of the key phrase before the record's image is.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1173FP.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SW ASSIGN TO "pb1173fp.tmp".
           SELECT IN1 ASSIGN TO "pb1173fpi.dat".
           SELECT OU1 ASSIGN TO "pb1173fpo.dat".
       DATA DIVISION.
       FILE SECTION.
       SD SW.
       01 SPR USAGE POINTER.
       FD IN1.
       01 IR PIC X(5).
       FD OU1.
       01 OR1 PIC X(5).
       PROCEDURE DIVISION.
       MAIN.
           SORT SW ASCENDING KEY SPR USING IN1 GIVING OU1
           STOP RUN.
