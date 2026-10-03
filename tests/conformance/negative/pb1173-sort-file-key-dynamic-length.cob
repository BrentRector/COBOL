      *> reject-at: 2014 2023
      *> kb/Work PB1173 + PB1052 — A FILE SORT KEY THAT IS A DYNAMIC-LENGTH ITEM IS REFUSED BY SR6 d), NOT BY A CAPABILITY.
      *>   cite.py --check 14.9.40.3 "A key data item shall not be a variable-length group, an occurs-depending-on
      *>     data item, a dynamic-length elementary item or an item subordinate to a dynamic-capacity table."
      *>     -> OK §14.9.40.3 6) d)
      *> SK is PIC X DYNAMIC LENGTH (a COBOL-2014 construct). The statement was refused all along, but as "key 'SK' has
      *> no character image" — a statement about the compiler — where the standard names the case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1173FD.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SW ASSIGN TO "pb1173fd.tmp".
           SELECT IN1 ASSIGN TO "pb1173fdi.dat".
           SELECT OU1 ASSIGN TO "pb1173fdo.dat".
       DATA DIVISION.
       FILE SECTION.
       SD SW.
       01 SR.
          05 SK PIC X DYNAMIC LENGTH.
       FD IN1.
       01 IR PIC X(5).
       FD OU1.
       01 OR1 PIC X(5).
       PROCEDURE DIVISION.
       MAIN.
           SORT SW ASCENDING KEY SK USING IN1 GIVING OU1
           STOP RUN.
