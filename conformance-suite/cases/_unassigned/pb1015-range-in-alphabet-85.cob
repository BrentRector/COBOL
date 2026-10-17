*>--- conformance-case
*> id: pb1015-range-in-alphabet-85
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1015 - the THROUGH range's [ IN alphabet-name-1 ] phrase is a COBOL-2002 addition
      *> (constructs.json range-in-alphabet-2002; the edge is DERIVED - the 2023 Annex E covers 2014->2023 only,
      *> and COBOL-85's EVALUATE and VALUE formats carry a bare THROUGH pair). 14.7.8 rule 2: "When the IN
      *> alphabet-name phrase is specified, the collating sequence used for range evaluation is the collating
      *> sequence defined by that alphabet." Every spelling is gated at the ONE resolver, because the phrase is
      *> decided by symbol, not position:
      *>   MID  - the VALUE clause's explicit IN spelling (13.18.63.2 format 3)
      *>   LOW  - the VALUE clause with IN omitted (IN is an optional word; AL names an alphabet)
      *>   WHEN - the EVALUATE range-expression (14.9.13.2), explicit IN
      *> Before PB1015 this compiled at --std 85 and printed IN / MID. The same source is legal at 2002+
      *> (tests/conformance/2002/pb398_range_in_alphabet and pb502_value_range_in_alphabet pin the behaviour).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1015NEG.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET AL IS NATIVE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-C PIC X VALUE "C".
          88 MID VALUE "A" THRU "M" IN AL.
          88 LOW VALUE "A" THRU "F" AL.
       PROCEDURE DIVISION.
           EVALUATE WS-C
               WHEN "A" THRU "M" IN AL DISPLAY "IN"
               WHEN OTHER DISPLAY "OUT"
           END-EVALUATE
           IF MID DISPLAY "MID" END-IF
           STOP RUN.
