      *> PB1907 -- UNSTRING overlap: identifier-4 (INTO) or
      *> identifier-6 (COUNT IN) shares storage with identifier-8
      *> (TALLYING), and identifier-7 (POINTER) with identifier-8.
      *> ISO 14.9.48.4 GR18 / Annex A.2 item 61: the result is
      *> undefined; 4.4 2): a run unit that allows it is conforming.
      *> Documented implementor choice (docs/CONFORMANCE.md D-UNS3,
      *> GnuCOBOL 3.2 libcob strings.c cob_unstring_tallying then
      *> cob_unstring_finish, emitted in that order by cobc typeck.c
      *> cb_emit_unstring): after the last receiving area the number
      *> of areas acted upon is ADDED to the TALLYING item's content
      *> at that moment, and THEN the POINTER item is stored.
      *> C1: COUNT IN stores 3 into WS-T, then 2 areas are added: 05.
      *> C2: INTO stores 7 into WS-T, then 2 areas are added: 09.
      *> D1: WS-PT starts 1; tally 1 + 2 = 3 is stored, then the
      *>     pointer (1 + 6 examined) = 7 overwrites it: 07.
      *> (A start-value tally gives C1 02, C2 02, D1 03.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1907U3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-S   PIC X(6) VALUE "abc,de".
       01 WS-S2  PIC X(3) VALUE "7,x".
       01 WS-P1  PIC XXX  VALUE "zzz".
       01 WS-P2  PIC XXX  VALUE "zzz".
       01 WS-T   PIC 99   VALUE 0.
       01 WS-PT  PIC 99   VALUE 1.
       PROCEDURE DIVISION.
           MOVE 0 TO WS-T
           UNSTRING WS-S DELIMITED BY ","
               INTO WS-P1 COUNT IN WS-T WS-P2
               TALLYING IN WS-T
           END-UNSTRING
           DISPLAY "C1 P1=[" WS-P1 "] P2=[" WS-P2 "] T=" WS-T
           MOVE 0 TO WS-T
           UNSTRING WS-S2 DELIMITED BY ","
               INTO WS-T WS-P2
               TALLYING IN WS-T
           END-UNSTRING
           DISPLAY "C2 P2=[" WS-P2 "] T=" WS-T
           MOVE 1 TO WS-PT
           UNSTRING WS-S DELIMITED BY ","
               INTO WS-P1 WS-P2
               WITH POINTER WS-PT
               TALLYING IN WS-PT
           END-UNSTRING
           DISPLAY "D1 P1=[" WS-P1 "] P2=[" WS-P2 "] PT=" WS-PT
           STOP RUN.
