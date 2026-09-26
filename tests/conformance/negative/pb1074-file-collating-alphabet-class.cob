      *> reject-at: 2002 2014 2023
      *> kb/Work PB1074 — ISO §12.4.5.7.3 SR1 "Alphabet-name-1 shall reference an alphabet that defines an
      *> alphanumeric collating sequence", SR2 "Alphabet-name-2 shall reference an alphabet that defines a
      *> national collating sequence", SR7 "When the class of data-name-1 or record-key-name-1 is national,
      *> alphabet-name-3 shall reference an alphabet that defines a national collating sequence". They are rules
      *> about the clause AS WRITTEN: NOSUCH is refused although the key-level clause shadows it for every key,
      *> REV (alphanumeric) is refused as alphabet-name-2, and REV is refused on the national key IX-KEY.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1074NG.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET REV IS "ZYXWVUTSRQPONMLKJIHGFEDCBA".
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IXF ASSIGN TO "pb1074ng.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IX-KEY
               COLLATING SEQUENCE IS NOSUCH REV
               COLLATING SEQUENCE OF IX-KEY IS REV.
       DATA DIVISION.
       FILE SECTION.
       FD IXF.
       01 IX-REC.
          05 IX-KEY  PIC N(1).
          05 IX-DAT  PIC X(1).
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT IXF CLOSE IXF.
           STOP RUN.
