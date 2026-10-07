      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB2040 - SORT ... USING file-name-2 is a file-name, written alone (ISO 8.4.2.2.2 gives a
      *> file-name no qualified format; cite.py: OK 8.4.2.2.2). The USING list resolved each operand by
      *> its base word, so the qualifier OF G was dropped in silence and the sort ran over IN1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W1031GPB2040SORT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IN1 ASSIGN TO "w1031gpb2040a.dat".
           SELECT OUT1 ASSIGN TO "w1031gpb2040b.dat".
           SELECT S1 ASSIGN TO "w1031gpb2040s.dat".
       DATA DIVISION.
       FILE SECTION.
       FD IN1.
       01 R1 PIC X(4).
       FD OUT1.
       01 R2 PIC X(4).
       SD S1.
       01 SR PIC X(4).
       PROCEDURE DIVISION.
           SORT S1 ON ASCENDING KEY SR
               USING IN1 OF G GIVING OUT1.
           DISPLAY "DONE".
           STOP RUN.
