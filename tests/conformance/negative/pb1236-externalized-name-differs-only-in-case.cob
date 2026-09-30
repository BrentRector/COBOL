      *> reject-at: 2002 2014 2023
      *> kb/Work PB1236 - ISO 13.18.22.3 SR2: "In the same source
      *>   element, the externalized name of the subject of the entry that
      *>   includes the EXTERNAL clause shall not be the same as the
      *>   externalized name of any other entry that includes the
      *>   EXTERNAL clause." The run-unit external store keys its cells
      *>   case-insensitively (a data-name's case never matters, 8.3.2),
      *>   so `01 A ... EXTERNAL` and `01 B ... EXTERNAL AS "a"` are ONE
      *>   externalized name: rejected COBOLNET2159 - they used to alias one
      *>   cell of the first-created width in silence.
      *> cite.py --check 13.18.22.3 "In the same source element, the
      *>   externalized name of the subject of the entry that" -> OK
      *>   13.18.22.3 2)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1236N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC X(3) EXTERNAL.
       01 B PIC X(5) EXTERNAL AS "a".
       PROCEDURE DIVISION.
           STOP RUN.
