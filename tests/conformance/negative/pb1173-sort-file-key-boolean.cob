      *> reject-at: 2002 2014 2023
      *> kb/Work PB1173 — A FILE SORT KEY OF CLASS BOOLEAN IS REFUSED BY THE KEY-CLASS RULE.
      *>   cite.py --check 14.9.40.3 "Key data items shall not be of the class boolean, message-tag, object, or
      *>     pointer." -> OK §14.9.40.3 6) c)
      *> KB is PIC 1 USAGE BIT, class boolean (USAGE BIT is a COBOL-2002 construct, so the rule can only fire from
      *> 2002). The statement used to compile clean.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1173FB.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SW ASSIGN TO "pb1173fb.tmp".
           SELECT IN1 ASSIGN TO "pb1173fbi.dat".
           SELECT OU1 ASSIGN TO "pb1173fbo.dat".
       DATA DIVISION.
       FILE SECTION.
       SD SW.
       01 SR.
          05 K1 PIC X.
          05 KB PIC 1 USAGE BIT.
       FD IN1.
       01 IR PIC X(5).
       FD OU1.
       01 OR1 PIC X(5).
       PROCEDURE DIVISION.
       MAIN.
           SORT SW ASCENDING KEY KB USING IN1 GIVING OU1
           STOP RUN.
