*>--- conformance-case
*> id: pb843-range-identifier-alphabet-numeric
*> rules: [SR-14.9.13.3-3]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB843. `WS-LO THRU WS-HI IN AL` - the trailing IN AL names an ALPHABET (8.3.2.2: a word is
      *> only one type of user-defined word, so it is not a qualifier), so it is 14.9.13.2's
      *> `[ IN alphabet-name-1 ]` phrase and 14.9.13.3 SR3 governs it: "Alphabet-name-1 may be specified only
      *> when the literals or identifiers specified in the THROUGH phrase are of class alphabetic,
      *> alphanumeric, or national." WS-LO and WS-HI are numeric. Before PB843 the phrase was read as a
      *> qualifier and the verdict was COBOLNET1639 on 'WS-HIINAL' - the phrase's own rule never ran.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB843NEG2.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET AL IS STANDARD-1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-N  PIC 9 VALUE 5.
       01 WS-LO PIC 9 VALUE 1.
       01 WS-HI PIC 9 VALUE 7.
       PROCEDURE DIVISION.
       MAIN-P.
           EVALUATE WS-N
               WHEN WS-LO THRU WS-HI IN AL DISPLAY "IN"
               WHEN OTHER                  DISPLAY "OUT"
           END-EVALUATE
           STOP RUN.
