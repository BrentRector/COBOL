*>--- conformance-case
*> id: pb64t5_character_classification
*> rules: [GR-8.8.4.4.4-3, SR-12.3.6.3-3, GR-12.3.6.4-5, GR-12.3.6.4-7, GR-14.6.6-2, RV-15.57.4-3, RV-15.57.4-5, RV-15.57.4-6, RV-15.97.4-1, RV-15.97.4-3, RV-15.97.4-5, RV-15.97.4-6, FMT-12.3.6.2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 12.3.6 OBJECT-COMPUTER CHARACTER CLASSIFICATION (Annex A.4.9 item 7) with its two consumers (12.3.6.4 GR7):
      *> the UPPER-CASE / LOWER-CASE case mapping (15.97.4 r3 / 15.57.4 r3 — without a LOCALE phrase, the classification
      *> locale's LC_CTYPE) and the ALPHABETIC / ALPHABETIC-UPPER / ALPHABETIC-LOWER class tests (8.8.4.4.4 GR3 b1/c1/d1)
      *> — increment T5 of docs/rearchitecture/DESIGN-locale-facility.md (kb/Work PB64). The classification here is a
      *> NAMED locale, Turkish (12.3.6.4 GR5 a): in tr the uppercase of "i" is the DOTTED capital I (U+0130) and the
      *> lowercase of "I" is the DOTLESS small i (U+0131) — the one case mapping a locale visibly changes.
      *> Every outcome is witnessed by FUNCTION ORD (the code point + 1), never by the console echo (the harness
      *> normalizes nothing inside a literal, but a reader cannot tell U+0130 from U+0049 by eye).
      *>
      *> What each line proves:
      *>   UP-I  — UPPER-CASE("i") under the Turkish classification → U+0130 (ORD 305); LO-I — LOWER-CASE("I") → U+0131
      *>           (ORD 306). DETERMINATION L9: the mapping is simple (1:1), so the lengths are unchanged (LEN-UP/LEN-LO).
      *>   UP-A  — a letter whose mapping the locale does not tailor maps as always ("a" → "A", ORD 66).
      *>   ALPHA-TR — 8.8.4.4.4 GR3 b1: under a classification locale, ALPHABETIC is "characters identified as alphabetic
      *>           in LC_CTYPE" — the dotless "ı" (not in the Latin set A–Z/a–z) IS alphabetic there; and, as the rule
      *>           reads, SPACE is not (b2, the no-locale case, names space; b1 does not): "ab cd" is NOT alphabetic
      *>           under the classification (ALPHA-SP) — a documented reading (CONFORMANCE.md).
      *>   UPPER-TR / LOWER-TR — ALPHABETIC-UPPER / -LOWER over the tailored letters (d1 / c1).
      *> Non-ASCII appears only inside literals (UTF-8 source); every DISPLAY is ASCII.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB64T5CC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. X CHARACTER CLASSIFICATION IS TR.
       SPECIAL-NAMES.
           LOCALE TR IS "tr-TR".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  S               PIC X(4).
       01  N               PIC 9(5).
       01  DOTLESS         PIC X VALUE "ı".
       01  SPACED          PIC X(5) VALUE "ab cd".
       01  UPCASE          PIC X(2) VALUE "İI".
       01  LOCASE          PIC X(2) VALUE "ıi".
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION UPPER-CASE("i") TO S
           MOVE FUNCTION ORD(S(1:1)) TO N
           DISPLAY "UP-I=" N
           MOVE FUNCTION LENGTH(FUNCTION UPPER-CASE("i")) TO N
           DISPLAY "LEN-UP=" N
           MOVE FUNCTION LOWER-CASE("I") TO S
           MOVE FUNCTION ORD(S(1:1)) TO N
           DISPLAY "LO-I=" N
           MOVE FUNCTION LENGTH(FUNCTION LOWER-CASE("I")) TO N
           DISPLAY "LEN-LO=" N
           MOVE FUNCTION UPPER-CASE("a") TO S
           MOVE FUNCTION ORD(S(1:1)) TO N
           DISPLAY "UP-A=" N
           IF DOTLESS IS ALPHABETIC DISPLAY "ALPHA-TR=yes" ELSE DISPLAY "ALPHA-TR=no" END-IF
           IF SPACED IS ALPHABETIC DISPLAY "ALPHA-SP=yes" ELSE DISPLAY "ALPHA-SP=no" END-IF
           IF UPCASE IS ALPHABETIC-UPPER DISPLAY "UPPER-TR=yes" ELSE DISPLAY "UPPER-TR=no" END-IF
           IF LOCASE IS ALPHABETIC-LOWER DISPLAY "LOWER-TR=yes" ELSE DISPLAY "LOWER-TR=no" END-IF
           IF LOCASE IS ALPHABETIC-UPPER DISPLAY "UPPER-LO=yes" ELSE DISPLAY "UPPER-LO=no" END-IF
           STOP RUN.
