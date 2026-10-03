      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1173 — A FILE SORT KEY UNDER OCCURS IS REFUSED BY SR6 b) AND f), NOT BY THE SUBSCRIPT RULE.
      *>   cite.py --check 14.9.40.3 "Key data names shall not be subject to any OCCURS clauses" -> OK §14.9.40.3 6) b)
      *>   cite.py --check 14.9.40.3 "None of the data items identified by key data-names may be described by an
      *>     entry that either contains an OCCURS clause or is subordinate to an entry that contains an OCCURS
      *>     clause." -> OK §14.9.40.3 6) f)
      *> KOCC is a table; writing it unsubscripted used to draw COBOLNET2270 ("a table element needs a subscript"),
      *> true and beside the point: no subscript would make it a legal key. The key-admissibility predicate names the
      *> rule the statement breaks, for the key whether or not it is subscripted.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1173FC.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SW ASSIGN TO "pb1173fc.tmp".
           SELECT IN1 ASSIGN TO "pb1173fci.dat".
           SELECT OU1 ASSIGN TO "pb1173fco.dat".
       DATA DIVISION.
       FILE SECTION.
       SD SW.
       01 SR.
          05 K1 PIC X.
          05 KOCC PIC X OCCURS 2.
       FD IN1.
       01 IR PIC X(5).
       FD OU1.
       01 OR1 PIC X(5).
       PROCEDURE DIVISION.
       MAIN.
           SORT SW ASCENDING KEY KOCC USING IN1 GIVING OU1
           STOP RUN.
