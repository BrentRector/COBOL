      *> reject-at: 2014 2023
      *> kb/Work PB1264 - ISO 13.18.38.3 SR30: "Data-name-3 shall not be
      *>   defined elsewhere in the source element" - and 8.3.2.2: a given
      *>   user-defined word "may be used as only one type of user-defined
      *>   word". F is a FILE-NAME below; the CAPACITY register written F
      *>   is a data-name (SR30 defines it as one), so the ONE user-word
      *>   declaration funnel refuses it COBOLNET2692 (the check used to
      *>   consult only the data-name index and the other registers, then
      *>   a hand-written file-name scan beside them).
      *> cite.py --check 13.18.38.3 "Data-name-3 shall not be defined
      *>   elsewhere in the source element" -> OK  13.18.38.3 30)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1264B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb1264.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 FR PIC X.
       WORKING-STORAGE SECTION.
       01 R.
          05 T PIC X OCCURS DYNAMIC CAPACITY IN F.
       PROCEDURE DIVISION.
           STOP RUN.
