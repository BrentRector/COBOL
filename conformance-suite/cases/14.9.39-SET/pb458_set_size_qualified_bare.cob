*>--- conformance-case
*> id: pb458_set_size_qualified_bare
*> rules: [SR-14.9.39.3-33, GR-14.9.39.4-39]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 2023 14.9.39.2 Format 16 - SET [ SIZE OF ] data-name-3 TO { integer-2 | arithmetic-expression-5 }.
      *> SIZE OF is a BRACKET, so the two arms are ONE format with one set of rules, and 14.9.39.3 SR33 says only
      *> "Data-name-3 shall reference a dynamic-length elementary data item" - it places no restriction on how
      *> the reference is written. 8.4.2.2.3 rule 2: "A name may be qualified even though it does not need
      *> qualification", so D OF G is a legal spelling of D. 13.16.3 SR18 leaves an OCCURS clause impossible on a
      *> DYNAMIC LENGTH entry ("If a DYNAMIC LENGTH clause is specified, the only other clauses permitted are
      *> level-number, entry-name, PICTURE, USAGE, and VALUE"), so a Format-16 receiver can never carry a
      *> subscript either - the guard that dropped this program had nothing legal left to exclude.
      *>
      *> The SIZE-OF-absent arm used to refuse any receiver carrying a dataReferenceSuffix - a guard aimed at
      *> subscripts, which a suffix ALSO carries qualification through - so SET D OF G TO 2 fell into the Format-1
      *> numeric store and terminated on a raw runtime exception. kb/Work PB458.
      *>
      *> EXPECTED VALUES, DERIVED FROM THE RULE, NOT MEASURED: 14.9.39.4 GR38 sets the current length to
      *> integer-2, so after SET ... TO 2 the item's content is its first 2 character positions ("AB") and
      *> FUNCTION LENGTH is 2 (15.50.4 - the current length of a dynamic-length item). GR39 initializes ADDED
      *> positions to spaces on a grow and never restores truncated content, so growing back to 4 gives "AB  ".
      *> Both arms are written over the SAME qualified receiver, so the two lines must agree.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB458-SET-SIZE-QUAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 D PIC X DYNAMIC LENGTH LIMIT IS 8.
       01 H.
          05 D PIC X DYNAMIC LENGTH LIMIT IS 8.
       01 WS-N PIC 9(2).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "ABCD" TO D OF G.
           SET D OF G TO 2.
           MOVE FUNCTION LENGTH(D OF G) TO WS-N.
           DISPLAY "BARE[" D OF G "]" " LEN=" WS-N.
           MOVE "ABCD" TO D OF H.
           SET SIZE OF D OF H TO 2.
           MOVE FUNCTION LENGTH(D OF H) TO WS-N.
           DISPLAY "EXPL[" D OF H "]" " LEN=" WS-N.
           SET D OF G TO 4.
           MOVE FUNCTION LENGTH(D OF G) TO WS-N.
           DISPLAY "GROW[" D OF G "]" " LEN=" WS-N.
           STOP RUN.
