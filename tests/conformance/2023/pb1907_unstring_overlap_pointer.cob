      *> PB1907 -- UNSTRING overlap: identifier-4 (INTO) or
      *> identifier-6 (COUNT IN) shares storage with identifier-7
      *> (POINTER); identifier-1 (sending) shares storage with
      *> identifier-7 (POINTER) or identifier-8 (TALLYING).
      *> ISO 14.9.48.4 GR18 / Annex A.2 item 61: the result is
      *> undefined; 4.4 2): a run unit that allows it is conforming.
      *> Documented implementor choice (docs/CONFORMANCE.md D-UNS4,
      *> GnuCOBOL 3.2 libcob strings.c: cob_unstring_init reads the
      *> pointer ONCE; cob_unstring_finish stores it LAST): the
      *> pointer's initial value governs the whole scan, stores into
      *> the pointer item during the statement do not move the scan,
      *> and the final pointer value overwrites them. The sending
      *> item sees the POINTER and TALLYING stores only after the
      *> statement, because both are stored at its end.
      *> P1: "9" -> WS-PT (09); the scan continues at 3; PT = 05.
      *> P2: COUNT IN stores 03 into WS-PT; final PT = 07.
      *> E1: WS-GP (3) is WS-G's first byte; scan "b,cd,"; at the end
      *>     the pointer 8 lands in byte 1: G = "8ab,cd,".
      *> E2: WS-KT (1) is WS-K's first byte; 3 areas; KT = 4.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1907U4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-S   PIC X(6) VALUE "abc,de".
       01 WS-S2  PIC X(4) VALUE "9,ab".
       01 WS-P1  PIC XXX  VALUE "zzz".
       01 WS-P2  PIC XXX  VALUE "zzz".
       01 WS-P3  PIC XXX  VALUE "zzz".
       01 WS-PT  PIC 99   VALUE 1.
       01 WS-G.
          05 WS-GP  PIC 9    VALUE 3.
          05 WS-G2  PIC X(6) VALUE "ab,cd,".
       01 WS-K.
          05 WS-KT  PIC 9    VALUE 1.
          05 WS-K2  PIC X(5) VALUE ",a,bc".
       01 WS-T   PIC 9    VALUE 0.
       PROCEDURE DIVISION.
           MOVE 1 TO WS-PT
           UNSTRING WS-S2 DELIMITED BY ","
               INTO WS-PT WS-P2
               WITH POINTER WS-PT
           END-UNSTRING
           DISPLAY "P1 P2=[" WS-P2 "] PT=" WS-PT
           MOVE 1 TO WS-PT
           UNSTRING WS-S DELIMITED BY ","
               INTO WS-P1 COUNT IN WS-PT WS-P2
               WITH POINTER WS-PT
           END-UNSTRING
           DISPLAY "P2 P1=[" WS-P1 "] P2=[" WS-P2 "] PT=" WS-PT
           UNSTRING WS-G DELIMITED BY ","
               INTO WS-P1 WS-P2
               WITH POINTER WS-GP
               TALLYING IN WS-T
           END-UNSTRING
           DISPLAY "E1 G=[" WS-G "] P1=[" WS-P1 "] P2=[" WS-P2
               "] T=" WS-T
           UNSTRING WS-K DELIMITED BY ","
               INTO WS-P1 WS-P2 WS-P3
               TALLYING IN WS-KT
           END-UNSTRING
           DISPLAY "E2 K=[" WS-K "] P1=[" WS-P1 "] P2=[" WS-P2
               "] P3=[" WS-P3 "]"
           STOP RUN.
