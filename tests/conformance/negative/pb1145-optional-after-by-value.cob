      *> reject-at: 2002 2014 2023
      *> kb/Work PB1145 - ISO 14.2.1 prints OPTIONAL only in the BY
      *>   REFERENCE alternative of the using-phrase: after BY VALUE, the
      *>   word `OPTIONAL LY` continues the BY VALUE alternative, which has
      *>   no OPTIONAL. The threaded BY VALUE mode used to be inherited by
      *>   the plain `OPTIONAL data-name` arm and accepted.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1145O.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LX PIC 9(3).
       01 LY PIC 9(3).
       PROCEDURE DIVISION USING BY VALUE LX OPTIONAL LY.
           GOBACK.
