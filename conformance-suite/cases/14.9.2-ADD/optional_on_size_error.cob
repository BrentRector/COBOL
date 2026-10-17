      *>--- conformance-case
      *> id: optional_on_size_error
      *> rules: [GR-14.9.2.4-6, GR-14.9.8.4-2, GR-14.9.44.4-6,
      *>    FMT-14.9.2.2, FMT-14.9.8.2, FMT-14.9.12.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 5.2.3 — an uppercase word printed WITHOUT an underline is an OPTIONAL WORD: it may be written or
      *> omitted, and omitting it does not change the meaning. In every arithmetic statement's printed general
      *> format the word ON is NOT underlined (SIZE, ERROR and NOT are), so `SIZE ERROR` and `NOT SIZE ERROR`
      *> are legal spellings of the same phrases. Measured rather than assumed: the underline rectangles were
      *> read per word off the PDF on pages 632 (COMPUTE), 644 (DIVIDE) and 703 (MULTIPLY).
      *> Regression guard for the defect fixed 2026-07-27: arithmeticOnSizeError, computeOnSizeError and
      *> mcsExceptionPhrases all required the token ON, so every line below was rejected with COBOL0001
      *> "no viable alternative at input 'SIZE'" — while callOnExceptionPhrase next door already had `ON?`,
      *> so the grammar contradicted itself. This golden pins ON as omittable for all five arithmetic
      *> statements, and proves each phrase still binds to its own role.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OPTONSIZEERR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SMALL   PIC 9(3) VALUE 1.
       01 BIG     PIC 9(3) VALUE 999.
       01 RES     PIC 9(3).
       PROCEDURE DIVISION.
       MAIN.
           ADD 1 TO SMALL
               SIZE ERROR DISPLAY "ADD-ON"
               NOT SIZE ERROR DISPLAY "ADD-NOT"
           END-ADD.
           ADD 500 TO BIG
               SIZE ERROR DISPLAY "ADD2-ON"
               NOT SIZE ERROR DISPLAY "ADD2-NOT"
           END-ADD.

           MOVE 5 TO SMALL.
           SUBTRACT 1 FROM SMALL
               SIZE ERROR DISPLAY "SUB-ON"
               NOT SIZE ERROR DISPLAY "SUB-NOT"
           END-SUBTRACT.

           MOVE 900 TO BIG.
           MULTIPLY 5 BY BIG
               SIZE ERROR DISPLAY "MUL-ON"
               NOT SIZE ERROR DISPLAY "MUL-NOT"
           END-MULTIPLY.

           MOVE 10 TO SMALL.
           DIVIDE 2 INTO SMALL
               SIZE ERROR DISPLAY "DIV-ON"
               NOT SIZE ERROR DISPLAY "DIV-NOT"
           END-DIVIDE.

           COMPUTE RES = 999 * 99
               SIZE ERROR DISPLAY "CMP-ON"
               NOT SIZE ERROR DISPLAY "CMP-NOT"
           END-COMPUTE.

      *> The omission is independent per phrase: ON may be written on one and omitted on the other, and the
      *> reversed order established by phrase_order_arithmetic must still hold with ON absent.
           COMPUTE RES = 1 + 1
               NOT SIZE ERROR DISPLAY "MIX-NOT"
               ON SIZE ERROR DISPLAY "MIX-ON"
           END-COMPUTE.

           STOP RUN.
