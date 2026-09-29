      *> reject-at: 2002 2014 2023
      *> kb/Work PB1539 - an AS literal of spaces only. ISO 11.10.3 SR1 admits it (it is
      *> not a zero-length literal), but ISO 8.3.2.2 2) leaves the FORMATION of an
      *> externalized name to the implementor, and DOC-A.1-68 forms every externalized
      *> name without its leading and trailing spaces - so this literal forms the
      *> zero-length name, which no CALL, CANCEL or program-address target can name.
      *> Where the clause itself refuses a zero-length literal the formation rule
      *> refuses this one too (COBOLNET2643) instead of creating a program nothing can
      *> reach.  Below 2002 the AS phrase itself is the introduction gate
      *> (negative/pb303_as_phrase_below_2002).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1539NA AS "   ".
       PROCEDURE DIVISION.
       MAIN-P.
           STOP RUN.
       END PROGRAM PB1539NA.
