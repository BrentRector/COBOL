*>--- conformance-case
*> id: pb393-move-varlen-position-mismatch
*> rules: [SR-14.9.25.3-9]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB393. ISO 1989:2023 14.9.25.3 SR9 over the POSITIONAL half of the relation. 8.5.1.12.1 rule
      *> 3: "For each dynamic-length elementary item in either group there is a corresponding dynamic-length
      *> elementary item in the other group as specified in 8.5.1.12.2, Positional correspondence", and
      *> 8.5.1.12.2: "Two dynamic-length elementary items correspond if they start at the same relative byte
      *> positions within their groups." Under 8.5.1.12.3 ("all dynamic-length elementary items are considered
      *> to be of zero length") AD starts at relative byte 3 and BD at relative byte 4, so the two groups are
      *> NOT compatible and the move is refused - even though both are variable-length groups of the same
      *> collapsed length.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB393NEGVLP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 VA-G.
          05 AA PIC X(3) VALUE "ABC".
          05 AD PIC X DYNAMIC LENGTH.
          05 AB PIC X(2) VALUE "YZ".
       01 VB-G.
          05 BA PIC X(4).
          05 BD PIC X DYNAMIC LENGTH.
          05 BB PIC X(1).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "WXYZ" TO AD.
           MOVE VA-G TO VB-G.
           DISPLAY "BA=[" BA "]".
           STOP RUN.
