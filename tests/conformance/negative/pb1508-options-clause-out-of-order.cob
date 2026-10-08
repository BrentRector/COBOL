      *> reject-at: 2014 2023
      *> ISO 11.9.2 prints ARITHMETIC before DEFAULT ROUNDED, and 5.2.1 binds that sequence ("shall be
      *> written ... in the sequence given in the general format, unless otherwise specified by the rules
      *> of that format" - 11.9.3 specifies nothing of the kind). kb/Work PB1508: this compiled clean.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1508NA.
       OPTIONS.
           DEFAULT ROUNDED MODE IS TRUNCATION
           ARITHMETIC IS NATIVE.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
