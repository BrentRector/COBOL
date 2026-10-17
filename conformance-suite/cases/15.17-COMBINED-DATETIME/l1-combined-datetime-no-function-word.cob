*>--- conformance-case
*> id: l1-combined-datetime-no-function-word
*> rules: [FMT-15.17.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.17.2 prints "FUNCTION COMBINED-DATETIME ( argument-1 argument-2 )" with BOTH words UNDERLINED,
      *> and an underlined word in a general format is a REQUIRED word. §8.4.3.2.3 SR2 states the one exception
      *> and its boundary: "If intrinsic-function-name-1 or the ALL phrase is specified in the REPOSITORY
      *> paragraph … the word FUNCTION may be omitted from the function-identifier; otherwise the word FUNCTION
      *> is required." There is no REPOSITORY paragraph here, so the omission is not permitted and
      *> COMBINED-DATETIME (143951 45296) is not a function-identifier at all. Nothing else can resolve it
      *> either: no data item of that name is declared.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1NCDTKW.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-C PIC 9(7)V9(5).
       PROCEDURE DIVISION.
           COMPUTE W-C = COMBINED-DATETIME(143951 45296)
           STOP RUN.
       END PROGRAM L1NCDTKW.
