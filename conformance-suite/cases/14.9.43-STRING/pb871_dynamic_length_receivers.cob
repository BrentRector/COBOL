*>--- conformance-case
*> id: pb871_dynamic_length_receivers
*> rules: [GR-14.9.43.4-7, GR-14.9.48.4-11]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB871 - a DYNAMIC-LENGTH elementary item as the RECEIVING operand of every verb
      *> that transfers "according to the rules for the MOVE statement", not only MOVE.
      *> 8.5.1.10.4: "If a dynamic-length elementary item is a receiving operand and is not
      *> reference-modified, the new value becomes the content of the item. The new length of the
      *> dynamic-length elementary item is determined by the length of new content." ... "If the
      *> maximum length is reached, the value is truncated on the right as necessary." 14.9.25.4 GR8
      *> routes a MOVE there; UNSTRING (14.9.48.4 GR11c/d) and ACCEPT (14.9.1.4 GR6) move "according
      *> to the rules for the MOVE statement". Every expected value below is derived from those rules:
      *>
      *> UNS  UNSTRING "AB,CDEFGHIJ" DELIMITED BY "," INTO D1 DELIMITER IN D2 -> D1 "AB" LEN 2,
      *>      D2 "," LEN 1 (the delimiter moved as an elementary alphanumeric item, GR11d).
      *> UNS2 without DELIMITED BY, GR11b examines "the size of the current receiving area": D3's
      *>      MAXIMUM size, 8 (DETERMINATION D-DL2, docs/CONFORMANCE.md 3) -> "ABCDEFGH" LEN 8.
      *> ACC  ACCEPT D4 FROM DAY-OF-WEEK: GR12's one-digit conceptual item -> LEN 1; FROM DATE:
      *>      GR7's six digits (YYMMDD) -> LEN 6 (the digits themselves are the clock's, not asserted).
      *> STRING - 14.9.43.4 GR7 changes "only the portion of the data item referenced by identifier-3
      *>      that was referenced", so STRING is NOT a replacement: D-DL2 makes the receiver's size
      *>      its maximum, and the new length (8.5.1.10.4) is the old content's, extended through the
      *>      last position written:
      *> STR1 "XY" into the 8-character "ABCDEFGH" -> "XYCDEFGH" LEN 8 (positions 3-8 untouched).
      *> STR2 "XYZ" into an EMPTY item -> "XYZ" LEN 3 (it overflowed at the first character before).
      *> STR3 "Q" WITH POINTER 5 into the 3-character "XYZ" -> position 4 is a grown position, a
      *>      space (the character 14.9.39.4 GR39 gives any position a dynamic item grows by) ->
      *>      "XYZ Q" LEN 5, POINTER 6.
      *> STR4 12 characters into the 5-character item: positions 1-8 (the maximum) are written and
      *>      the ninth character overflows (GR8) -> "ABCDEFGH" LEN 8, OVERFLOW.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB871DYNLENRECEIVERS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-S PIC X(11) VALUE "AB,CDEFGHIJ".
       01 D1 PIC X DYNAMIC LENGTH LIMIT IS 8.
       01 D2 PIC X DYNAMIC LENGTH LIMIT IS 8.
       01 D3 PIC X DYNAMIC LENGTH LIMIT IS 8.
       01 D4 PIC X DYNAMIC LENGTH LIMIT IS 8.
       01 D5 PIC X DYNAMIC LENGTH LIMIT IS 8.
       01 D6 PIC X DYNAMIC LENGTH LIMIT IS 8.
       01 L  PIC 99.
       01 L2 PIC 99.
       01 P  PIC 99.
       PROCEDURE DIVISION.
           UNSTRING WS-S DELIMITED BY "," INTO D1 DELIMITER IN D2
           MOVE FUNCTION LENGTH(D1) TO L
           MOVE FUNCTION LENGTH(D2) TO L2
           DISPLAY "UNS  [" D1 "] " L " [" D2 "] " L2
           MOVE "ABCDEFGHIJ" TO WS-S
           UNSTRING WS-S INTO D3
           MOVE FUNCTION LENGTH(D3) TO L
           DISPLAY "UNS2 [" D3 "] " L
           ACCEPT D4 FROM DAY-OF-WEEK
           MOVE FUNCTION LENGTH(D4) TO L
           DISPLAY "ACC  " L
           ACCEPT D4 FROM DATE
           MOVE FUNCTION LENGTH(D4) TO L
           DISPLAY "ACC  " L
           MOVE "ABCDEFGH" TO D5
           STRING "XY" DELIMITED BY SIZE INTO D5
           MOVE FUNCTION LENGTH(D5) TO L
           DISPLAY "STR1 [" D5 "] " L
           STRING "XYZ" DELIMITED BY SIZE INTO D6
             ON OVERFLOW DISPLAY "STR2 OVERFLOW"
           END-STRING
           MOVE FUNCTION LENGTH(D6) TO L
           DISPLAY "STR2 [" D6 "] " L
           MOVE 5 TO P
           STRING "Q" DELIMITED BY SIZE INTO D6 WITH POINTER P
           MOVE FUNCTION LENGTH(D6) TO L
           DISPLAY "STR3 [" D6 "] " L " " P
           MOVE "12345" TO D6
           STRING "ABCDEFGHIJKL" DELIMITED BY SIZE INTO D6
             ON OVERFLOW DISPLAY "STR4 OVERFLOW"
           END-STRING
           MOVE FUNCTION LENGTH(D6) TO L
           DISPLAY "STR4 [" D6 "] " L
           STOP RUN.
