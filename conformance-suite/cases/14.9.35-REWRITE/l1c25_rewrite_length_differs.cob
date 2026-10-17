      *>--- conformance-case
      *> id: l1c25_rewrite_length_differs
      *> rules: [GR-14.9.35.4-18]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.35.4 GR18 — a REWRITE of a relative or indexed file
      *> may replace a record with one of a different length.
      *> "The number of bytes in the record referenced by identifier-1,
      *> the runtime representation of literal-1, or the record
      *> referenced by record-name-1 may differ from the number of
      *> bytes in the record being replaced."
      *> cite.py: OK  §14.9.35.4 18)  (General rules)
      *> cite.py: OK  §13.18.43.4 13) a)  (General rules) - the REWRITE
      *>   record length is "the content of the data item referenced by
      *>   data-name-1" (DEPENDING ON)
      *> cite.py: OK  §13.18.43.4 15)  (General rules) - after a
      *>   successful READ data-name-1 holds "the number of bytes in the
      *>   record just read"
      *> Both lengths stay within FROM 5 TO 20, so GR20 ('44') does not
      *> apply; with GR18 the REWRITE succeeds and the new length is
      *> what a later READ reports. (Record sequential files are GR16,
      *> where a length change is unsuccessful; not used here.)
      *> Derivation (each line; LN is zeroed before every READ):
      *>  REL1: record 1 written 10 bytes, rewritten 15 bytes -> 00;
      *>        READ -> LN=0015, "BBBBBBBBBBBBBBB".
      *>  REL2: record 2 written 15 bytes, rewritten 5 bytes -> 00;
      *>        READ -> LN=0005, "DDDDD".
      *>  IDX1: K001 written 10 bytes, rewritten 18 -> 00; READ ->
      *>        LN=0018, "K001EEEEEEEEEEEEEE".
      *>  IDX2: K002 written 16 bytes, rewritten 6 -> 00; READ ->
      *>        LN=0006, "K002FF".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C25E.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT R-FILE ASSIGN TO "L1C25E1.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS R-KEY
               FILE STATUS IS R-ST.
           SELECT X-FILE ASSIGN TO "L1C25E2.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS X-KEY
               FILE STATUS IS X-ST.
       DATA DIVISION.
       FILE SECTION.
       FD R-FILE
           RECORD IS VARYING IN SIZE FROM 5 TO 20 CHARACTERS
               DEPENDING ON R-LN.
       01 R-REC PIC X(20).
       FD X-FILE
           RECORD IS VARYING IN SIZE FROM 5 TO 20 CHARACTERS
               DEPENDING ON X-LN.
       01 X-REC.
          05 X-KEY  PIC X(4).
          05 X-DATA PIC X(16).
       WORKING-STORAGE SECTION.
       01 R-ST  PIC XX.
       01 X-ST  PIC XX.
       01 R-KEY PIC 9(4).
       01 R-LN  PIC 9(4).
       01 X-LN  PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT R-FILE X-FILE.
           MOVE 1 TO R-KEY. MOVE 10 TO R-LN.
           MOVE ALL "A" TO R-REC. WRITE R-REC.
           MOVE 2 TO R-KEY. MOVE 15 TO R-LN.
           MOVE ALL "C" TO R-REC. WRITE R-REC.
           MOVE 10 TO X-LN. MOVE "K001" TO X-KEY.
           MOVE ALL "A" TO X-DATA. WRITE X-REC.
           MOVE 16 TO X-LN. MOVE "K002" TO X-KEY.
           MOVE ALL "C" TO X-DATA. WRITE X-REC.
           CLOSE R-FILE X-FILE.
           OPEN I-O R-FILE X-FILE.
           MOVE 1 TO R-KEY. MOVE 15 TO R-LN.
           MOVE ALL "B" TO R-REC. REWRITE R-REC.
           DISPLAY "REL1 REW=" R-ST.
           MOVE 2 TO R-KEY. MOVE 5 TO R-LN.
           MOVE ALL "D" TO R-REC. REWRITE R-REC.
           DISPLAY "REL2 REW=" R-ST.
           MOVE 18 TO X-LN. MOVE "K001" TO X-KEY.
           MOVE ALL "E" TO X-DATA. REWRITE X-REC.
           DISPLAY "IDX1 REW=" X-ST.
           MOVE 6 TO X-LN. MOVE "K002" TO X-KEY.
           MOVE ALL "F" TO X-DATA. REWRITE X-REC.
           DISPLAY "IDX2 REW=" X-ST.
           CLOSE R-FILE X-FILE.
           OPEN INPUT R-FILE X-FILE.
           MOVE 1 TO R-KEY. MOVE 0 TO R-LN. READ R-FILE.
           DISPLAY "REL1 LN=" R-LN " " R-REC(1:R-LN).
           MOVE 2 TO R-KEY. MOVE 0 TO R-LN. READ R-FILE.
           DISPLAY "REL2 LN=" R-LN " " R-REC(1:R-LN).
           MOVE "K001" TO X-KEY. MOVE 0 TO X-LN. READ X-FILE.
           DISPLAY "IDX1 LN=" X-LN " " X-REC(1:X-LN).
           MOVE "K002" TO X-KEY. MOVE 0 TO X-LN. READ X-FILE.
           DISPLAY "IDX2 LN=" X-LN " " X-REC(1:X-LN).
           CLOSE R-FILE X-FILE.
           STOP RUN.
