*>--- conformance-case
*> id: l1-formatted-current-date-no-function-word
*> rules: [FMT-15.38.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.38.2 prints "FUNCTION FORMATTED-CURRENT-DATE ( argument-1 )" with BOTH words UNDERLINED, and an
      *> underlined word in a general format is a REQUIRED word. §8.4.3.2.3 SR2 states the one exception and its
      *> boundary: "If intrinsic-function-name-1 or the ALL phrase is specified in the REPOSITORY paragraph …
      *> the word FUNCTION may be omitted from the function-identifier; otherwise the word FUNCTION is required."
      *> No REPOSITORY paragraph is written here and no data item of that name is declared, so the bare name is
      *> not a function-identifier and there is no other reading to fall back on.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1NFCDKW.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-S PIC X(15).
       PROCEDURE DIVISION.
           MOVE FORMATTED-CURRENT-DATE("YYYYMMDDThhmmss") TO W-S
           STOP RUN.
       END PROGRAM L1NFCDKW.
