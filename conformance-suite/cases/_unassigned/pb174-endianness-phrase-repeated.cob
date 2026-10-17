      *>--- conformance-case
      *> id: pb174-endianness-phrase-repeated
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 5.2.6.4, Choice indicators: "When enclosed by brackets,
      *> zero or more of the alternatives contained within the choice
      *> indicators shall be specified, but any single alternative may
      *> be specified only once." The 13.18.60.2 FLOAT-BINARY-* tail is
      *> a single bracketed endianness-phrase, so at most one.
      *> kb/Work PB174.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB174N3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B USAGE FLOAT-BINARY-32 HIGH-ORDER-LEFT HIGH-ORDER-RIGHT.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
