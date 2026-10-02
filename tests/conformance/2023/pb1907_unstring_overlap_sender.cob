      *> PB1907 -- UNSTRING overlap: identifier-1 (sending) shares
      *> storage with identifier-4 (INTO) or identifier-6 (COUNT IN).
      *> ISO 14.9.48.4 GR18 / Annex A.2 item 61: the result is
      *> undefined; 4.4 2): a run unit that allows it is conforming.
      *> Documented implementor choice (docs/CONFORMANCE.md D-UNS1,
      *> CLAUDE.md rule 1 -> GnuCOBOL 3.2 libcob strings.c
      *> cob_unstring_into): UNSTRING runs IN PLACE -- each receiving
      *> area is examined in the sending item's storage AS IT IS
      *> after the previous receiver's INTO, DELIMITER IN and
      *> COUNT IN stores; nothing is snapshotted at initiation.
      *> A1: "AAA" -> WS-F2 (X4) makes WS-REC "AAA,AAA CCC"; the
      *>     second scan starts at position 5 and finds no ",", so
      *>     WS-B gets "AAA CCC", WS-C is not acted upon, T = 2.
      *>     (A snapshot would give B=BBB, C=CCC, T=03.)
      *> A2: COUNT IN WS-CNT stores 2 into position 4 of WS-G before
      *>     the second scan, so WS-P2 gets "2" (snapshot: "5").
      *> A3: the receiver WS-V2 is a REDEFINES view of the sender
      *>     WS-A: "AAA" -> WS-V2 makes WS-A "AAA,AAA    ", and the
      *>     second scan finds no "," (snapshot: B=BBB, C=CCC, T=03).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1907U1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-REC.
          05 WS-F1 PIC X(4) VALUE "AAA,".
          05 WS-F2 PIC X(4) VALUE "BBB,".
          05 WS-F3 PIC X(3) VALUE "CCC".
       01 WS-B   PIC X(7) VALUE "zzzzzzz".
       01 WS-C   PIC X(3) VALUE "zzz".
       01 WS-T   PIC 99   VALUE 0.
       01 WS-G.
          05 WS-G1  PIC X(3) VALUE "ab,".
          05 WS-CNT PIC 9    VALUE 5.
          05 WS-G2  PIC X(3) VALUE ",cd".
       01 WS-P1  PIC XX   VALUE "zz".
       01 WS-P2  PIC XX   VALUE "zz".
       01 WS-P3  PIC XX   VALUE "zz".
       01 WS-A   PIC X(11) VALUE "AAA,BBB,CCC".
       01 WS-V REDEFINES WS-A.
          05 WS-V1 PIC X(4).
          05 WS-V2 PIC X(7).
       01 WS-B3  PIC X(7) VALUE "zzzzzzz".
       01 WS-C3  PIC X(3) VALUE "zzz".
       01 WS-T3  PIC 99   VALUE 0.
       PROCEDURE DIVISION.
           UNSTRING WS-REC DELIMITED BY ","
               INTO WS-F2 WS-B WS-C TALLYING IN WS-T
           END-UNSTRING
           DISPLAY "A1 REC=[" WS-REC "] B=[" WS-B "] C=[" WS-C
               "] T=" WS-T
           UNSTRING WS-G DELIMITED BY ","
               INTO WS-P1 COUNT IN WS-CNT WS-P2 WS-P3
           END-UNSTRING
           DISPLAY "A2 G=[" WS-G "] P1=[" WS-P1 "] P2=[" WS-P2
               "] P3=[" WS-P3 "]"
           UNSTRING WS-A DELIMITED BY ","
               INTO WS-V2 WS-B3 WS-C3 TALLYING IN WS-T3
           END-UNSTRING
           DISPLAY "A3 A=[" WS-A "] B=[" WS-B3 "] C=[" WS-C3
               "] T=" WS-T3
           STOP RUN.
