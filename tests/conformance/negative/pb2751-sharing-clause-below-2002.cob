      *> reject-at: 85
      *> kb/Work PB2751 - the 2002 golden pb2751_keyed_write_rule_order
      *> shares one relative file between two connectors through the
      *> file control entry's SHARING clause, a COBOL-2002
      *> introduction (ISO 12.4.5.15), so below 2002 the entry is
      *> refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2751GATE85.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT Q ASSIGN TO "pb2751g.dat"
               ORGANIZATION IS RELATIVE ACCESS MODE IS SEQUENTIAL
               SHARING WITH READ ONLY
               FILE STATUS IS QS.
       DATA DIVISION.
       FILE SECTION.
       FD Q.
       01 Q-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 QS PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN INPUT Q
           CLOSE Q
           STOP RUN.
