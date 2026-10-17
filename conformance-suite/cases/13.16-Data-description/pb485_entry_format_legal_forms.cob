*>--- conformance-case
*> id: pb485_entry_format_legal_forms
*> rules: [SR-13.16.3-1, SR-13.16.3-2, GR-13.18.33.4-2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.16.2 formats 1-3 and 13.18.33.4 GR2 -- the OVER-REJECTION
      *> GUARD for the COBOLNET1747 format screen: every legal shape of
      *> each special level-number must still compile.
      *> A 66 RENAMES entry (format 2); the three condition-name spellings
      *> of format 3 -- a single literal, a VALUES ARE list with THRU, and
      *> the WHEN SET TO FALSE tail; and a NAMED 77 noncontiguous item,
      *> which 13.16.3 SR2 requires to carry the data-name format of the
      *> entry-name clause. The WHEN SET TO FALSE entry is DECLARED
      *> and TESTED, not SET: SET condition-name TO FALSE is a
      *> separately-deferred runtime feature, and this golden is about
      *> the ENTRY FORMAT the declaration is written in.
      *> The fourth shape -- a format-1 entry whose ONLY clause is a VALUE
      *> clause, legal under 13.16.3 SR9 -- is the probe that flips the
      *> axis these hold fixed, and it lives in the PENDING sibling
      *> pb485_value_only_format1.cob because SR9's implied PICTURE is not
      *> implemented yet. The screen is safe from it by CONSTRUCTION: the
      *> level-number selects which format check runs and the body only
      *> answers it, never the reverse, so a level-01 entry is never
      *> tested against the format-3 shape at all. kb/Work PB485.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB485P3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G.
           05  A PIC X(2) VALUE "AB".
           05  B PIC X(2) VALUE "CD".
       66  R RENAMES A THRU B.
       77  N PIC 9 VALUE 3.
       88  N-IS-THREE VALUE 3.
       88  N-IN-RANGE VALUES ARE 1 THRU 5.
       88  N-TOGGLE VALUE 3 WHEN SET TO FALSE 0.
       PROCEDURE DIVISION.
           DISPLAY R
           IF N-IS-THREE DISPLAY "THREE" END-IF
           IF N-IN-RANGE DISPLAY "RANGE" END-IF
           IF N-TOGGLE DISPLAY "TOGGLE" END-IF
           DISPLAY N
           STOP RUN.
