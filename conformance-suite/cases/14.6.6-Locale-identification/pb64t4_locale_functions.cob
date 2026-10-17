*>--- conformance-case
*> id: pb64t4_locale_functions
*> rules: [GR-14.6.6-7, GR-14.6.6-8, AR-15.3-8, AR-15.51.3-1,
*>    RV-15.51.4-2, RV-15.51.4-3, RV-15.51.4-4, RV-15.51.4-5,
*>    AR-15.52.3-2, RV-15.52.4-1, RV-15.52.4-2, RV-15.52.4-3,
*>    AR-15.53.3-3, RV-15.53.4-2, RV-15.53.4-3, RV-15.54.4-2,
*>    RV-15.54.4-3, FMT-15.51.2, FMT-15.52.2, FMT-15.53.2, FMT-15.54.2]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 15.51 LOCALE-COMPARE, 15.52 LOCALE-DATE, 15.53 LOCALE-TIME, 15.54 LOCALE-TIME-FROM-SECONDS — increment T4 of
      *> docs/rearchitecture/DESIGN-locale-facility.md (kb/Work PB64; Annex A.4.9 items 2–5). A 2014 golden because
      *> LOCALE-TIME-FROM-SECONDS and TRIM are 2014 introductions; the 2002 edition of the other three is pinned by the
      *> construct rows. The harness pins the user default locale to the root; the program names its locales.
      *>
      *> What each line proves:
      *>   CMP-CUR  — 15.51.4 r3 "otherwise, the current locale is used": under the root, n-tilde is n + tilde, so
      *>              "nz" > "ñu" → ">".
      *>   CMP-ES   — locale-name-1: the Spanish ordering (CLDR es: n-tilde a primary after n) → "<".
      *>   CMP-SET  — the current locale after SET LOCALE LC_COLLATE TO ES — the same "<" (r3's "current locale" is
      *>              the run unit's LC_COLLATE, 14.6.6 r7).
      *>   CMP-EQ   — r2: trailing spaces are truncated, an all-space operand is one space → "ab" vs "ab   " is "=";
      *>              r5/r6 the one-character result.
      *>   CMP-FR   — 15.51.4 NOTE "not necessarily a character-by-character comparison": "côte" vs "coter" under
      *>              fr-FR — at the primary level cote = cote and r < e? no: "cote" vs "coter" — "côte" is the
      *>              shorter primary string, so it sorts FIRST: "<". (The root order agrees here, which is why the
      *>              Spanish pair above carries the locale-sensitivity proof; this line pins the accent being a
      *>              secondary difference only.)
      *>   DATE-ROOT / DATE-FR / DATE-JA — 15.52.4 r2: the date 2026-08-19 in the locale's d_fmt (DETERMINATION L10:
      *>              the culture's short date pattern) — invariant "08/19/2026", France "19/08/2026", Japan
      *>              "2026/08/19". r3: the length depends on the locale — LEN shows it.
      *>   TIME-ROOT / TIME-DE — 15.53.4 r2: 13:05:09 in the locale's t_fmt (L10: the long time pattern) —
      *>              invariant "13:05:09", Germany "13:05:09"; TIME-US — the US long time pattern carries the AM/PM
      *>              designator: "1:05:09 PM".
      *>   TIME-24 / TIME-99 — 15.53.3 r3: hours 00–24 and seconds 00–99 are THIS function's own ranges; "240000" and
      *>              "235999" format without error (a DateTime could not hold either).
      *>   SEC / SEC-FRAC / SEC-FR — 15.54: seconds past midnight in standard numeric time form per t_fmt: 47109 =
      *>              13:05:09; a fractional argument carries its fraction into the seconds (Annex D.31.4.5's
      *>              nanosecond note, a determination): 47109.25 → "13:05:09.25"; the named locale's pattern and
      *>              decimal separator (France: "13:05:09,25").
      *>   HANDLED/MISSING — 15.52.4 r1: a locale-name whose locale is unavailable sets EC-LOCALE-MISSING; with
      *>              checking on the declarative observes it and the statement is interrupted (R keeps "?").
      *>   ARG      — 15.52.3 r2: an invalid date is EC-ARGUMENT-FUNCTION (checking off here → the §15.3 default,
      *>              an empty result, shown as "[]").
      *> Non-ASCII appears only inside literals (UTF-8 source); every DISPLAY is ASCII.
       >>TURN EC-LOCALE-MISSING CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB64T4FN.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE ES IS "es-ES"
           LOCALE FR IS "fr-FR"
           LOCALE JA IS "ja-JP"
           LOCALE GER IS "de-DE"
           LOCALE USA IS "en-US"
           LOCALE XX IS "xx-NOWHERE".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A               PIC X(2) VALUE "nz".
       01  B               PIC X(2) VALUE "ñu".
       01  R               PIC X VALUE "?".
       01  D               PIC X(8) VALUE "20260819".
       01  T               PIC X(6) VALUE "130509".
       01  S               PIC X(20).
       01  N               PIC 99.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-LOCALE-MISSING.
       H-P.
           DISPLAY "HANDLED=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           DISPLAY "CMP-CUR=" FUNCTION LOCALE-COMPARE(A B)
           DISPLAY "CMP-ES=" FUNCTION LOCALE-COMPARE(A B ES)
           SET LOCALE LC_COLLATE TO ES
           DISPLAY "CMP-SET=" FUNCTION LOCALE-COMPARE(A B)
           SET LOCALE LC_COLLATE TO USER-DEFAULT
           DISPLAY "CMP-EQ=" FUNCTION LOCALE-COMPARE("ab" "ab   ")
           DISPLAY "CMP-FR=" FUNCTION LOCALE-COMPARE("côte" "coter" FR)
           MOVE FUNCTION LOCALE-DATE(D) TO S
           DISPLAY "DATE-ROOT=[" FUNCTION TRIM(S) "]"
           MOVE FUNCTION LOCALE-DATE(D FR) TO S
           DISPLAY "DATE-FR=[" FUNCTION TRIM(S) "]"
           MOVE FUNCTION LOCALE-DATE(D JA) TO S
           DISPLAY "DATE-JA=[" FUNCTION TRIM(S) "]"
           MOVE FUNCTION LENGTH(FUNCTION LOCALE-DATE(D FR)) TO N
           DISPLAY "LEN=" N
           MOVE FUNCTION LOCALE-TIME(T) TO S
           DISPLAY "TIME-ROOT=[" FUNCTION TRIM(S) "]"
           MOVE FUNCTION LOCALE-TIME(T GER) TO S
           DISPLAY "TIME-DE=[" FUNCTION TRIM(S) "]"
           MOVE FUNCTION LOCALE-TIME(T USA) TO S
           DISPLAY "TIME-US=[" FUNCTION TRIM(S) "]"
           MOVE FUNCTION LOCALE-TIME("240000") TO S
           DISPLAY "TIME-24=[" FUNCTION TRIM(S) "]"
           MOVE FUNCTION LOCALE-TIME("235999") TO S
           DISPLAY "TIME-99=[" FUNCTION TRIM(S) "]"
           MOVE FUNCTION LOCALE-TIME-FROM-SECONDS(47109) TO S
           DISPLAY "SEC=[" FUNCTION TRIM(S) "]"
           MOVE FUNCTION LOCALE-TIME-FROM-SECONDS(47109.25) TO S
           DISPLAY "SEC-FRAC=[" FUNCTION TRIM(S) "]"
           MOVE FUNCTION LOCALE-TIME-FROM-SECONDS(47109.25 FR) TO S
           DISPLAY "SEC-FR=[" FUNCTION TRIM(S) "]"
           MOVE FUNCTION LOCALE-DATE(D XX) TO R
           DISPLAY "MISSING=" R
           MOVE FUNCTION LOCALE-DATE("20261399") TO S
           DISPLAY "ARG=[" FUNCTION TRIM(S) "]"
           STOP RUN.
