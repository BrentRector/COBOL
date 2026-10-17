      *>--- conformance-case
      *> id: pb374-ec-mcs-name-below-2023
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB374 - the EC-MCS exception-names are the asynchronous
      *>   messaging facility's, a COBOL-2023 addition (Annex E.3.2 item
      *>   3 lists EC-MCS and its level-3 names among the new exception
      *>   conditions). Below --std 2023 the name draws the introduction
      *>   gate (COBOLNET0878) before the facility's accept-inert
      *>   warning is ever asked, so 2023's COBOLNET1578 is not the
      *>   answer here. The TURN directive is a 2002 introduction, so
      *>   COBOL-85 is not a cell of this negative.
       >>TURN EC-MCS-INVALID-TAG CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB374NG.
       PROCEDURE DIVISION.
       M1.
           DISPLAY "UNREACHABLE".
           STOP RUN.
