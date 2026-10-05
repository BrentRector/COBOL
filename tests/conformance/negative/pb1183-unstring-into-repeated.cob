      *> reject-at: 85 2002 2014 2023
      *> ISO §14.9.48.2 general format — ONE INTO, then the repeated receiver group (kb/Work PB1183):
      *>   INTO { identifier-4 [ DELIMITER IN identifier-5 ] [ COUNT IN identifier-6 ] } …
      *>   cite.py --check 14.9.48.2 "INTO" -> OK §14.9.48.2 (General format)
      *> The ellipsis follows the BRACED group, so it repeats the receivers and never the INTO keyword.
      *> `INTO A INTO B` is a spelling no edition prints; the grammar's old `unstringIntoPhrase+` compiled
      *> it at every --std and ran A=[AB  ] B=[CD5E]. The second INTO is a syntax error at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1183N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S PIC X(12) VALUE "AB,CD5EF".
       01 A PIC X(4).
       01 B PIC X(4).
       PROCEDURE DIVISION.
           UNSTRING S DELIMITED BY "," INTO A INTO B.
           DISPLAY "[" A "][" B "]".
           STOP RUN.
