*>--- conformance-case
*> id: pb331-read-key-before-lock
*> rules: [FMT-14.9.30.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 5.2.1: "The words, phrases, clauses, punctuation, and operands in each
      *> general format shall be written in the compilation group in the sequence given
      *> in the general format, unless otherwise specified by the rules of that format."
      *> 14.9.30.2 Format 2 (PDF page 722, RENDERED) prints the KEY phrase AFTER both
      *> lock brackets. The spelling below inverts them, and no rule of that format
      *> licenses the reversal - only 5.2.6.4 choice indicators free an order, and the
      *> lock brackets carry none (figure_geometry.py 722: plain stems at y=507.57 and
      *> y=547.48; contrast the INVALID KEY group at y=634.55, flagged CHOICE
      *> INDICATORS). It is the exact spelling the pre-PB331 grammar accepted while
      *> REJECTING the printed one, so this file is the guard on that swap.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB331KYF.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IXF ASSIGN TO "pb331kyf.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS IX-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD IXF.
       01 IX-REC.
          05 IX-KEY  PIC X(4).
          05 IX-DATA PIC X(5).
       PROCEDURE DIVISION.
       MAIN.
           OPEN I-O IXF.
           MOVE "K001" TO IX-KEY.
           READ IXF RECORD KEY IS IX-KEY WITH NO LOCK
               INVALID KEY CONTINUE
           END-READ.
           CLOSE IXF.
           STOP RUN.
