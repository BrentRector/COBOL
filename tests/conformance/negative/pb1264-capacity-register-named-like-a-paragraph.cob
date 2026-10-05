      *> reject-at: 2014 2023
      *> kb/Work PB1264 - ISO 13.18.38.3 SR30: "Data-name-3 shall not be
      *>   defined elsewhere in the source element" - and 8.3.2.2: a given
      *>   user-defined word "may be used as only one type of user-defined
      *>   word". CAPS is a PARAGRAPH-NAME in the procedure division, which
      *>   the procedure-division binder declares AFTER the data division:
      *>   the second declaration, the paragraph's, is the one refused,
      *>   COBOLNET2692.
      *> cite.py --check 13.18.38.3 "Data-name-3 shall not be defined
      *>   elsewhere in the source element" -> OK  13.18.38.3 30)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1264P.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R.
          05 T PIC X OCCURS DYNAMIC CAPACITY IN CAPS.
       PROCEDURE DIVISION.
       CAPS.
           STOP RUN.
