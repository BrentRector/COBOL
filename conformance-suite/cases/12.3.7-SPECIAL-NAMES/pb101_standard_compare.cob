*>--- conformance-case
*> id: pb101_standard_compare
*> rules: [SR-12.3.7.3-9, GR-12.3.7.4-17, AR-15.3-12, AR-15.85.3-1,
*>    AR-15.85.3-2, AR-15.85.3-3, AR-15.85.3-5, AR-15.85.3-6,
*>    RV-15.85.4-1, RV-15.85.4-3, RV-15.85.4-4, RV-15.85.4-5,
*>    RV-15.85.4-6, RV-15.85.4-7, FMT-15.85.2, DOC-A.1-42]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 15.85 STANDARD-COMPARE + the SPECIAL-NAMES ORDER TABLE clause (12.3.7.2) — increment T7 of
      *> docs/rearchitecture/DESIGN-locale-facility.md 4.9 (kb/Work PB101; owner decision Q4, 2026-08-18).
      *> COBOL.NET claims Annex A.3 item 25: "Implements collation behavior consistent with ISO/IEC 14651
      *> through derived tables and CLDR/UCA data."
      *>
      *> The general format (15.85.2) is
      *>     FUNCTION STANDARD-COMPARE ( argument-1 argument-2 [ ordering-name-1 ] [ argument-4 ] )
      *> ORDER TABLE is bracketed WITHOUT an ellipsis in 12.3.7.2, so ONE clause per paragraph; OT1 names the
      *> default table 15.85.3 r5 spells 'ISO 14651_2020_TABLE1' (12.3.7.4 NOTE 5 spells the same table
      *> 'ISO_14651_2020_TABLE1'; both resolve).
      *>
      *> What each line proves:
      *>   L1/L2/L3  — 15.85.4 r5's "ordering level being used": level 1 is base letters (case and accents
      *>               invisible), level 2 adds accents, level 3 adds case (CLDR root: lowercase first).
      *>   L3HY/L4HY — the ISO/IEC 14651 default treatment of punctuation: variable characters are ignored
      *>               through level 3 and weighted at level 4, so "a-b" = "ab" at 3 and "a-b" < "ab" at 4.
      *>   OMHY      — 15.85.4 r1: "If argument-4 is unspecified, the highest level defined in the ordering
      *>               table is used" — it must equal the level-4 answer, not the level-3 one.
      *>   RES       — 15.85.4's NOTE: "not necessarily a character-by-character comparison and not
      *>               necessarily a case-sensitive comparison". Ordinally 'R' (x52) < 'r' (x72), so a
      *>               code-unit comparison would answer ">" where the cultural ordering answers "<".
      *>   TRAIL/SPC — 15.85.4 r4: trailing spaces truncated, an all-space operand truncated to ONE space.
      *>   LEN       — 15.85.4 r7: "The length of the returned value is 1."
      *> Non-ASCII appears only inside literals (UTF-8 source); every DISPLAY is ASCII.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB101SCMP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ORDER TABLE OT1 IS "ISO 14651_2020_TABLE1".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A-ACC  PIC X   VALUE "á".
       01 R-ACC  PIC X(6) VALUE "Résumé".
       01 LEN-R  PIC 9.
       PROCEDURE DIVISION.
       MAIN.
      *> Level 1 — base letters only (15.85.4 r5).
           DISPLAY "L1AA=" FUNCTION STANDARD-COMPARE("a" "A" OT1 1).
           DISPLAY "L1AC=" FUNCTION STANDARD-COMPARE("a" A-ACC OT1 1).
      *> Level 2 — accents count, case does not.
           DISPLAY "L2AA=" FUNCTION STANDARD-COMPARE("a" "A" OT1 2).
           DISPLAY "L2AC=" FUNCTION STANDARD-COMPARE("a" A-ACC OT1 2).
      *> Level 3 — case counts.
           DISPLAY "L3AA=" FUNCTION STANDARD-COMPARE("a" "A" OT1 3).
      *> Levels 3 and 4 over a variable (punctuation) character.
           DISPLAY "L3HY=" FUNCTION STANDARD-COMPARE("a-b" "ab" OT1 3).
           DISPLAY "L4HY=" FUNCTION STANDARD-COMPARE("a-b" "ab" OT1 4).
      *> argument-4 omitted = the highest level defined (15.85.4 r1).
           DISPLAY "OMHY=" FUNCTION STANDARD-COMPARE("a-b" "ab" OT1).
      *> The three returned values (15.85.4 r6).
           DISPLAY "EQ=" FUNCTION STANDARD-COMPARE("abc" "abc" OT1).
           DISPLAY "GT=" FUNCTION STANDARD-COMPARE("b" "a" OT1).
           DISPLAY "RES=" FUNCTION STANDARD-COMPARE("resume" R-ACC OT1).
      *> The r4 operand rule.
           DISPLAY "TRAIL=" FUNCTION STANDARD-COMPARE("abc" "abc   " OT1).
           DISPLAY "SPCS=" FUNCTION STANDARD-COMPARE("    " " " OT1).
      *> r7 — the returned value is one character position long.
           MOVE FUNCTION LENGTH(FUNCTION STANDARD-COMPARE("a" "b" OT1)) TO LEN-R.
           DISPLAY "LEN=" LEN-R.
      *> The ordering-name is optional: the same comparison against the default table (15.85.3 r5).
           DISPLAY "DEF=" FUNCTION STANDARD-COMPARE("a" "A" 3).
           STOP RUN.
