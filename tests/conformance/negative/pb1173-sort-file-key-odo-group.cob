      *> reject-at: 2002 2014 2023
      *> kb/Work PB1173 — A FILE SORT KEY THAT IS A GROUP HOLDING AN OCCURS DEPENDING TABLE IS REFUSED.
      *>   cite.py --check 14.9.40.3 "A key data item shall not be a variable-length group, an occurs-depending-on
      *>     data item, a dynamic-length elementary item or an item subordinate to a dynamic-capacity table."
      *>     -> OK §14.9.40.3 6) d)
      *>   cite.py --check 13.18.38.4 "is an occurs-depending group item" -> OK §13.18.38.4 8)
      *> KODO has an entry subordinate to it that specifies DEPENDING, so it is an occurs-depending group item and its
      *> length varies with CNT; SR6 e) takes "the same byte positions" as the key in every record, which no varying
      *> window can be. The statement used to compile clean.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1173FO.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SW ASSIGN TO "pb1173fo.tmp".
           SELECT IN1 ASSIGN TO "pb1173foi.dat".
           SELECT OU1 ASSIGN TO "pb1173foo.dat".
       DATA DIVISION.
       FILE SECTION.
       SD SW.
       01 SR.
          05 CNT PIC 9.
          05 KODO.
             10 KODE PIC X OCCURS 1 TO 3 DEPENDING ON CNT.
       FD IN1.
       01 IR PIC X(5).
       FD OU1.
       01 OR1 PIC X(5).
       PROCEDURE DIVISION.
       MAIN.
           SORT SW ASCENDING KEY KODO USING IN1 GIVING OU1
           STOP RUN.
