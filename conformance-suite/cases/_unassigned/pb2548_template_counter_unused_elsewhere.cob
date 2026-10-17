      *>--- conformance-case
      *> id: pb2548_template_counter_unused_elsewhere
      *> rules: []
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB2548 (train 1055 review) - an UNUSED TYPEDEF template
      *> whose OCCURS DEPENDING ON counter the declaration does not
      *> define is legal source whatever else the program declares under
      *> that name: the counter is resolved where a use site places the
      *> type (ISO 13.18.57.4 GR1; ISO 8.4.2.2.1 rule 5), and there is
      *> no use site. T's CNT is declared twice elsewhere (G1, G2: not
      *> an ambiguity of T), and T2's CNTX elsewhere is alphanumeric
      *> (not an integer, ISO 13.18.38.3 SR17, but not T2's counter).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2548UE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G0.
          05 CNTX PIC X VALUE "Z".
       01 T TYPEDEF.
          05 E PIC X OCCURS 1 TO 5 DEPENDING ON CNT.
       01 T2 TYPEDEF.
          05 E2 PIC X OCCURS 1 TO 5 DEPENDING ON CNTX.
       01 G1.
          05 CNT PIC 99 VALUE 3.
          05 F1 PIC X.
       01 G2.
          05 CNT PIC 99 VALUE 2.
          05 F2 PIC X.
       PROCEDURE DIVISION.
           DISPLAY CNT OF G1 " " CNT OF G2 " " CNTX
           STOP RUN.
