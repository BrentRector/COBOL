      *> PB1907 -- UNSTRING overlap through CALL BY REFERENCE: the
      *> sending and receiving LINKAGE items are one storage area.
      *> ISO 14.9.48.4 GR18 / Annex A.2 item 61: the result is
      *> undefined; 4.4 2): a run unit that allows it is conforming.
      *> Documented implementor choice (docs/CONFORMANCE.md D-UNS1;
      *> GnuCOBOL 3.2 libcob strings.c cob_unstring_into runs in
      *> place): the compiler cannot tell where a LINKAGE item
      *> lives, so every UNSTRING that names one re-reads the
      *> sender before each receiving area.
      *> K1: "AAA" -> LK-R (X11, the whole of WS-REC) space-fills it;
      *>     the second scan finds no "," in the spaces, so WS-B gets
      *>     seven spaces and WS-C is not acted upon, T = 2.
      *>     (A snapshot would give B=BBB, C=CCC, T=03.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1907U5.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-REC PIC X(11) VALUE "AAA,BBB,CCC".
       01 WS-B   PIC X(7)  VALUE "zzzzzzz".
       01 WS-C   PIC X(3)  VALUE "zzz".
       01 WS-T   PIC 99    VALUE 0.
       PROCEDURE DIVISION.
           CALL "PB1907U5S" USING WS-REC WS-REC WS-B WS-C WS-T
           DISPLAY "K1 REC=[" WS-REC "] B=[" WS-B "] C=[" WS-C
               "] T=" WS-T
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1907U5S.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-S   PIC X(11).
       01 LK-R   PIC X(11).
       01 LK-B   PIC X(7).
       01 LK-C   PIC X(3).
       01 LK-T   PIC 99.
       PROCEDURE DIVISION USING LK-S LK-R LK-B LK-C LK-T.
           UNSTRING LK-S DELIMITED BY ","
               INTO LK-R LK-B LK-C TALLYING IN LK-T
           END-UNSTRING
           GOBACK.
       END PROGRAM PB1907U5S.
       END PROGRAM PB1907U5.
