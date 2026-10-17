      *>--- conformance-case
      *> id: constant_record
      *> rules: [FMT-13.18.15.2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §13.18.15 CONSTANT RECORD clause (2002) — a STRUCTURED
      *> constant: the record's content is its normal initial content
      *> (§13.18.15.4 GR1 — as though the record had been the subject
      *> of INITIALIZE … WITH FILLER ALL TO VALUE THEN TO DEFAULT), and
      *> the content cannot be modified (§13.18.15.1; SR2's receiving
      *> rejection is the negative twin constant-record-store.cob).
      *> Reads are ordinary: elementary and group DISPLAY, a MOVE
      *> source, and a relation operand.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. KONSTRECP10CT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CFG CONSTANT RECORD.
          05 CFG-TAG  PIC X(4)  VALUE "COBL".
          05 CFG-VER  PIC 9(2)  VALUE 23.
          05 FILLER   PIC X(2)  VALUE "--".
          05 CFG-NAME PIC X(6)  VALUE "KONST".
       01 W-OUT PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "TAG=" CFG-TAG.
           DISPLAY "VER=" CFG-VER.
           DISPLAY "REC=" CFG.
           MOVE CFG-TAG TO W-OUT.
           DISPLAY "MOVED=" W-OUT.
           IF CFG-VER > 20 DISPLAY "REL-OK" END-IF.
           STOP RUN.
