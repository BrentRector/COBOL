*>--- conformance-case
*> id: pb544-usage-message-tag-group
*> rules: [SR-13.18.60.3-14]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB544 — §13.18.60.3 syntax rule 14's MESSAGE-TAG arm: "A USAGE clause with the MESSAGE-TAG,
      *> OBJECT REFERENCE, POINTER, FUNCTION-POINTER, or PROGRAM-POINTER phrase may be specified only for an
      *> elementary data item at level 1 or an elementary data item subordinate to a type declaration that
      *> includes the STRONG phrase." The screen's arm A reports at the GROUP entry, because §13.18.60.4 GR1
      *> pushes the group's usage onto every elementary item in it and the entry the programmer must change
      *> is this one. Like SR4's arm, it had no reachable subject until MESSAGE-TAG joined `usageKeyword`
      *> (kb/Work PB487) — before that `01 G USAGE MESSAGE-TAG.` was a bare parse error and the row was
      *> PARTIAL on this phrase alone.
      *> Pinned at 2023: MESSAGE-TAG is an Annex E.2 item-25 COBOL-2023 addition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB544S14.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G USAGE MESSAGE-TAG.
           05  M PIC X(3).
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
