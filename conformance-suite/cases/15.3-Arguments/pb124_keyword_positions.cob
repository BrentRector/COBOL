*>--- conformance-case
*> id: pb124_keyword_positions
*> rules: [AR-15.3-7, FMT-15.50.2, FMT-15.96.2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB124 (AR-15.3-7 / FMT-15.96.2) — the intrinsic phrase keywords bind POSITIONALLY, and a
      *> word in a non-keyword position is the user's own name. ISO 15.50.2: LENGTH(argument-1 [PHYSICAL]) —
      *> PHYSICAL is not a reserved word (8.10 has no row), so a data item NAMED PHYSICAL is legal and
      *> LENGTH(PHYSICAL) measures IT (3 here); the old unordered swallow consumed the name as the keyword
      *> and degraded the call to zero arguments. LENGTH(WS-G PHYSICAL) is the keyword form (10 — this
      *> implementation's PHYSICAL is transparent, 15.50.4 r8 determination). ISO 15.96.2 places TRIM's
      *> [LEADING|TRAILING] AFTER argument-1 (the figure notes: a plain bracket between argument-1 and the
      *> repeated argument-2). ISO 15.87.2 places [ANYCASE] [FIRST|LAST] before each SUBSTITUTE pair in that
      *> order: "aAa" with ANYCASE FIRST "a"->"z" replaces only the FIRST case-insensitive match: "zAa".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB124KP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PHYSICAL PIC X(3) VALUE "abc".
       01 WS-G.
          05 A PIC X(4).
          05 B PIC X(6).
       01 R PIC 9(3).
       01 RS PIC X(8).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = FUNCTION LENGTH(PHYSICAL)
           IF R = 3 DISPLAY "NAME OK" ELSE DISPLAY "NAME BAD " R END-IF
           COMPUTE R = FUNCTION LENGTH(WS-G PHYSICAL)
           IF R = 10 DISPLAY "KEYW OK" ELSE DISPLAY "KEYW BAD " R END-IF
           MOVE FUNCTION TRIM("  x " LEADING) TO RS
           IF RS = "x " DISPLAY "TRIM OK" ELSE DISPLAY "TRIM BAD [" RS "]"
           END-IF
           MOVE FUNCTION SUBSTITUTE("aAa" ANYCASE FIRST "a" "z") TO RS
           IF RS = "zAa" DISPLAY "SUBS OK" ELSE DISPLAY "SUBS BAD [" RS "]"
           END-IF
           STOP RUN.
