*>--- conformance-case
*> id: pb481-record-key-refmod
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB481 - ISO 12.4.5.12.2 prints RECORD KEY IS data-name-1, and 8.4.3.3.3's NOTE: "where data-name-n
*> is used in a general format or syntax rule, then reference-modification is not permitted." Refused as
*> COBOLNET2024, and ONLY that: the refused operand is recorded as written, so the key rules' SR2 row
*> ("references nothing described") no longer adds a second, false verdict. The screen used to call this
*> captured ref-mod "a subscript" - it now reads the suffixes through the one lexical reader.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB481N1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IXF ASSIGN TO "pb481n1.dat"
               ORGANIZATION IS INDEXED ACCESS MODE IS DYNAMIC
               RECORD KEY IS IX-KEY(1:3).
       DATA DIVISION.
       FILE SECTION.
       FD IXF.
       01 IX-REC.
          05 IX-KEY  PIC X(6).
          05 IX-DATA PIC X(8).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT IXF.
           CLOSE IXF.
           STOP RUN.
