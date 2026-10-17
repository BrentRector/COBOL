*>--- conformance-case
*> id: pb300_padding_user_word
*> rules: []
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.9 - the word PADDING as a USER-DEFINED word (kb/Work PB300).
      *> PADDING is a lexer token because the ANSI X3.23-1985 file-control PADDING
      *> CHARACTER clause spells it, but 8.9 reserves it at 85 and 2002 ONLY: it is
      *> absent from the 2023 reserved-word list (which runs PACKED-DECIMAL -> PAGE)
      *> and from the whole 2023 text, and reserved-words.json records r2014 false.
      *> So every one of these declarations and references is legal COBOL at 2014 and
      *> 2023, and each was a raw COBOL0001 parse error until PB300 made PADDING a
      *> reservation-gated cobolWord row - declining or deleting a clause may not cost
      *> the user the WORD.
      *>
      *> Three legs, because the fix has three surfaces:
      *>   W=   the plain name slot (cobolWord),
      *>   E1=/E2= a SUBSCRIPTED reference, which needs PADDING in the lexer's
      *>        _dataNameTokens trigger set or `PADDING (` never enters SUBSCRIPT mode,
      *>   Q=   qualification, so the word survives a two-name reference too.
      *> Expected output is read straight off the source: W= is the VALUE clause "ABC";
      *> E1= is the "PQ" moved into occurrence 1; E2= is occurrence 2, subscripted by a
      *> data item rather than a literal, holding the "RS" moved into it; Q= is the same
      *> scalar reached as PADDING OF WS-SCA.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB300-PADDING-WORD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-SCA.
           05  PADDING       PIC X(3) VALUE "ABC".
       01  WS-TAB.
           05  PADDING       PIC X(2) OCCURS 2.
       01  WS-N              PIC 9 VALUE 2.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "PQ" TO PADDING OF WS-TAB (1)
           MOVE "RS" TO PADDING OF WS-TAB (2)
           DISPLAY "W=" PADDING OF WS-SCA
           DISPLAY "E1=" PADDING OF WS-TAB (1)
           DISPLAY "E2=" PADDING OF WS-TAB (WS-N)
           DISPLAY "Q=" PADDING IN WS-SCA
           STOP RUN.
