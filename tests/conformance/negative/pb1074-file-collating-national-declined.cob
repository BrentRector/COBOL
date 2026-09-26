      *> reject-at: 2002 2014 2023
      *> kb/Work PB1074 — ISO §12.4.5.7.4 GR3 applies a national alphabet-name-2 to the keys of class national.
      *> Collating an indexed file's keys by an alphabet defined with literals is a processor-dependent capability
      *> (Annex A.3 item 41) this implementation does not provide, so it is declined BY NAME (COBOLNET1584) and
      *> refused — never silently ignored, which is what the file-level FOR NATIONAL phrase used to be.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1074ND.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET NEQV FOR NATIONAL IS N"A" ALSO N"B".
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT G ASSIGN TO "pb1074nd.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS G-KEY
               COLLATING SEQUENCE FOR NATIONAL IS NEQV.
       DATA DIVISION.
       FILE SECTION.
       FD G.
       01 G-REC.
          05 G-KEY   PIC N(1).
          05 G-DAT   PIC X(2).
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT G CLOSE G.
           STOP RUN.
