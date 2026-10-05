      *> reject-at: 2023
      *> kb/Work PB819 - 13.18.60.3 SR14's ELEMENTARY arm for a usage the compiler REFUSES BY NAME. SR14: "A USAGE
      *> clause with the MESSAGE-TAG, OBJECT REFERENCE, POINTER, FUNCTION-POINTER, or PROGRAM-POINTER phrase may be
      *> specified only for an elementary data item at level 1 or an elementary data item subordinate to a type
      *> declaration that includes the STRONG phrase." `05 A USAGE MESSAGE-TAG.` inside the ordinary group G is an
      *> elementary item at level 05 under no strong type declaration, so the rule is broken - exactly as it is
      *> for its group twin `01 G USAGE MESSAGE-TAG. 05 M PIC X(3).` (pb544-usage-message-tag-group). MESSAGE-TAG
      *> is declined non-support (Annex A.3 item 4, COBOLNET1943), so the item binds a recovery profile with no
      *> class, and the elementary arm - which asked the resolved CLASS - never saw it while the group arm, which
      *> reads the written clause, did. Both arms now read the one phrase, so the two spellings of ONE rule agree:
      *> each is COBOLNET1943 (the declined usage) AND COBOLNET1724 (SR14).
      *> Pinned at 2023: MESSAGE-TAG is an Annex E.2 item-25 COBOL-2023 addition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB819S14.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G.
           05  M USAGE MESSAGE-TAG.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
