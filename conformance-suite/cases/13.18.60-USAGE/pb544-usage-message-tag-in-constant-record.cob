*>--- conformance-case
*> id: pb544-usage-message-tag-in-constant-record
*> rules: [SR-13.18.60.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB544 — §13.18.60.3 syntax rule 4's MESSAGE-TAG arm, which had no reachable subject until the
      *> phrase joined `usageKeyword` (kb/Work PB487): `05 Q USAGE MESSAGE-TAG.` used to be
      *> `COBOL0001: no viable alternative at input 'MESSAGE-TAG'`, so nothing could distinguish SR4's
      *> violation from the general format's gap, and the row stayed PARTIAL on exactly this one phrase.
      *> SR4 lists SIX phrases where the neighbouring SR14 lists five — INDEX is the difference — and the
      *> screen is written as SR14's set UNION INDEX so the difference stays structural.
      *> ⚠ TWO diagnostics are correct here and both are wanted: COBOLNET1726 is the SYNTAX RULE, and
      *> COBOLNET1943 is the Annex A.3 item-4 non-support decline. A declined usage still has syntax rules
      *> over it; that is the whole reason this witness exists.
      *> Pinned at 2023: MESSAGE-TAG is an Annex E.2 item-25 COBOL-2023 addition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB544SR4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  CFG CONSTANT RECORD.
           05  Q USAGE MESSAGE-TAG.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
