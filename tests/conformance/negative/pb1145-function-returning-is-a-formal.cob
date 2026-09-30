      *> reject-at: 2002 2014 2023
      *> kb/Work PB1145 - ISO 14.2.2 SR6: "Data-name-2 shall not be the
      *>   same as data-name-1." A FUNCTION whose RETURNING item is also a
      *>   USING parameter was accepted clean (the program and function arm
      *>   had no SR6 check at all).
      *> cite.py --check 14.2.2 "Data-name-2 shall not be the same as
      *>   data-name-1" -> OK  14.2.2 6)
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB1145F.
       DATA DIVISION.
       LINKAGE SECTION.
       01 A PIC 9(3).
       PROCEDURE DIVISION USING A RETURNING A.
           GOBACK.
       END FUNCTION PB1145F.
