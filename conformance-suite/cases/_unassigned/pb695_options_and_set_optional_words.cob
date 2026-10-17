*>--- conformance-case
*> id: pb695_options_and_set_optional_words
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> !! THE OPTIONS INITIALIZE CLAUSE AND SET FORMATS 7 AND 16 WITH THEIR OPTIONAL WORDS OMITTED
      *> (kb/Work PB695). Three printed formats, three words the grammar demanded that the standard
      *> leaves plain (5.2.3; 8.3.2.4.3 - "may be specified at the user's option with no effect on the
      *> semantics of the format"):
      *>   . 11.9.10.2, the OPTIONS paragraph's INITIALIZE clause. Printed folio 277's underline roster
      *>     for the whole format is exactly ALL, BINARY, HIGH-VALUES, INITIALIZE, LOCAL-STORAGE,
      *>     LOW-VALUES, SCREEN, SPACES, WORKING-STORAGE and ZEROES; SECTION and TO carry no rule.
      *>     SECTION was already optional here and TO was not, so `INITIALIZE WORKING-STORAGE SECTION
      *>     HIGH-VALUES.` was rejected.
      *>   . 14.9.39.2 Format 16, on folio 732: `SET [ SIZE OF ] data-name-3 TO { integer-2 | ... }`
      *>     with rules under SET, SIZE and TO and none under OF.
      *>   . 14.9.39.2 Format 7, on folio 730: `SET { ADDRESS OF data-name-1 | identifier-5 } ... TO ...`
      *>     with a rule under ADDRESS and none under OF. Its sender arm takes the same phrase from the
      *>     8.4.3.11.2 data-address-identifier, printed on folio 140, whose entire underline roster is
      *>     the single word ADDRESS.
      *> DERIVATION of the expected lines:
      *>  . 11.9.10.4 4): "If WORKING-STORAGE is specified, all data items in the working-storage
      *>    section are initialized as indicated in the rules for initial state", and 5) b): "If
      *>    HIGH-VALUES is specified, the alphanumeric high value character is the specified-fill-
      *>    character." 11.9.10.4 6) confines that to items without a predefined initialization value,
      *>    so W-FILL (no VALUE clause) is HIGH-VALUES while W-ITEM keeps the "ABCD" its VALUE gave it.
      *>  . 14.9.39.4 (Format 7): the sender form sets the pointer to the address of W-ITEM, and the
      *>    receiver form makes the BASED item W-BASED refer to that address; reading W-BASED therefore
      *>    reads W-ITEM's four characters, `ABCD`.
      *>  . 14.9.39.4 (Format 16) sets the current length of the dynamic-length item to 3, which
      *>    the 15.50 LENGTH function returns for such an item, so LEN=3.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB695OPTSET.
       OPTIONS.
           INITIALIZE WORKING-STORAGE SECTION HIGH-VALUES.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-FILL PIC X(4).
       01 W-ITEM PIC X(4) VALUE "ABCD".
       01 W-PTR  USAGE POINTER.
       01 W-DYN  PIC X DYNAMIC LENGTH.
       LINKAGE SECTION.
       01 W-BASED PIC X(4) BASED.
       PROCEDURE DIVISION.
       MAIN.
           IF W-FILL = HIGH-VALUES
               DISPLAY "FILL=high"
           ELSE
               DISPLAY "FILL=other"
           END-IF
           DISPLAY "ITEM=" W-ITEM
           SET W-PTR TO ADDRESS W-ITEM
           SET ADDRESS W-BASED TO W-PTR
           DISPLAY "BASED=" W-BASED
           SET SIZE W-DYN TO 3
           DISPLAY "LEN=" FUNCTION LENGTH (W-DYN)
           DISPLAY "DONE"
           STOP RUN.
