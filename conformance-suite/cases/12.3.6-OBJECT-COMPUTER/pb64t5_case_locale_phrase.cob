*>--- conformance-case
*> id: pb64t5_case_locale_phrase
*> rules: [GR-12.3.6.4-5, GR-12.3.6.4-6, GR-12.3.6.4-7, GR-12.3.6.4-8, GR-14.6.6-2, RV-15.57.4-2, RV-15.57.4-3, RV-15.57.4-4, AR-15.97.3-1, RV-15.97.4-1, RV-15.97.4-2, RV-15.97.4-3, RV-15.97.4-4, FMT-12.3.6.2, FMT-15.57.2, FMT-15.97.2, DOC-A.1-35]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 15.97 UPPER-CASE / 15.57 LOWER-CASE with the LOCALE phrase (15.97.4 r2 / 15.57.4 r2 — Annex A.4.9 items 13 /
      *> 6), the implementor's correspondence when no locale is in effect (r4), and CHARACTER CLASSIFICATION IS LOCALE —
      *> the classification resolved from the locale CURRENT AT THE MODULE'S ACTIVATION (12.3.6.4 GR5 b / GR8; 14.6.6 r2)
      *> — increment T5 of docs/rearchitecture/DESIGN-locale-facility.md (kb/Work PB64). Every outcome is witnessed by
      *> FUNCTION ORD (U+0130 dotted capital I = ORD 305; U+0131 dotless small i = ORD 306; "I" = 74; "i" = 106).
      *>
      *> What each line proves:
      *>   PHRASE-UP / PHRASE-LO — the LOCALE phrase names a SPECIAL-NAMES locale-name: UPPER-CASE("i" LOCALE TR) → 305,
      *>           LOWER-CASE("I" LOCALE TR) → 306 whatever the current locale (r2).
      *>   OUTER-1 — the containing program's CHARACTER CLASSIFICATION IS LOCALE was resolved when IT was activated,
      *>           under the harness's pinned root: UPPER-CASE("i") without a phrase → "I" (74) — the root's mapping.
      *>   INNER   — after SET LOCALE LC_CTYPE TO TR the CALLed contained program (which inherits the clause, 12.3.6.4
      *>           GR1) resolves ITS classification at ITS activation → Turkish: UPPER-CASE("i") → 305.
      *>   OUTER-2 — back in the container, its classification is unchanged (GR8: effective with the module's INITIAL
      *>           state — the SET moved the run unit's LC_CTYPE, not a module's established classification) → 74.
      *>   PLAIN   — a program (the second top-level unit) with NO classification and no phrase: r4, the implementor's
      *>           correspondence — the invariant map — "i" → "I" (74) even while LC_CTYPE is Turkish.
      *> Non-ASCII appears only inside literals (UTF-8 source); every DISPLAY is ASCII.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB64T5PH.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. X CHARACTER CLASSIFICATION IS LOCALE.
       SPECIAL-NAMES.
           LOCALE TR IS "tr-TR".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  S               PIC X(4).
       01  N               PIC 9(5).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION UPPER-CASE("i" LOCALE TR) TO S
           MOVE FUNCTION ORD(S(1:1)) TO N
           DISPLAY "PHRASE-UP=" N
           MOVE FUNCTION LOWER-CASE("I" LOCALE TR) TO S
           MOVE FUNCTION ORD(S(1:1)) TO N
           DISPLAY "PHRASE-LO=" N
           MOVE FUNCTION UPPER-CASE("i") TO S
           MOVE FUNCTION ORD(S(1:1)) TO N
           DISPLAY "OUTER-1=" N
           SET LOCALE LC_CTYPE TO TR
           CALL "PB64T5IN"
           MOVE FUNCTION UPPER-CASE("i") TO S
           MOVE FUNCTION ORD(S(1:1)) TO N
           DISPLAY "OUTER-2=" N
           CALL "PB64T5PL"
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB64T5IN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  S               PIC X(4).
       01  N               PIC 9(5).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION UPPER-CASE("i") TO S
           MOVE FUNCTION ORD(S(1:1)) TO N
           DISPLAY "INNER=" N
           EXIT PROGRAM.
       END PROGRAM PB64T5IN.
       END PROGRAM PB64T5PH.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB64T5PL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  S               PIC X(4).
       01  N               PIC 9(5).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION UPPER-CASE("i") TO S
           MOVE FUNCTION ORD(S(1:1)) TO N
           DISPLAY "PLAIN=" N
           EXIT PROGRAM.
       END PROGRAM PB64T5PL.
