      *> reject-at: 2002 2014 2023
      *> kb/Work PB3149 - EC-MCS-NO-REQUESTOR is Annex E.3.2 3)'s
      *>   spelling (informative); 14.6.13.1.6 Table 13 names the
      *>   condition EC-MCS-NO-REQUESTER (docs/CONFORMANCE.md
      *>   D-MCS-NR). 7.3.25.3 SR2: "Exception-name-1 shall be one of
      *>   the exception names listed in 14.6.13.1", so the -OR
      *>   spelling is an unknown exception-name (COBOLNET0711) at
      *>   every edition. The TURN directive is a 2002 introduction,
      *>   so COBOL-85 is not a cell of this negative.
       >>TURN EC-MCS-NO-REQUESTOR CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3149NG.
       PROCEDURE DIVISION.
       M1.
           DISPLAY "UNREACHABLE".
           STOP RUN.
