      *> reject-at: 2002 2014 2023
      *> ISO 13.18.58.3 SR2: 'The description of the subject of the entry,
      *>   including its subordinate items, shall not contain a TYPE clause
      *>   that directly or indirectly references this type definition.'
      *>   T1 -> T2 -> T1, and NO storage item references either: the rule is
      *>   a property of the declaration (kb/Work PB1302).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1302NRC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T1 TYPEDEF.
          05 A TYPE T2.
       01 T2 TYPEDEF.
          05 B TYPE T1.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "UNREACHABLE"
           STOP RUN.
