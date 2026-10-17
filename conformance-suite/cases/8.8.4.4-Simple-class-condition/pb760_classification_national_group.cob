*>--- conformance-case
*> id: pb760_classification_national_group
*> rules: [GR-8.8.4.4.4-3, GR-12.3.6.4-5, GR-12.3.6.4-7]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 12.3.6.4 GR5 establishes TWO character classifications - a)-e) the ALPHANUMERIC one,
      *> f)-j) the NATIONAL one - and GR7 names their consumers: a) "the uppercase and lowercase mappings of
      *> characters for the UPPER-CASE and LOWER-CASE intrinsic functions" and b) the ALPHABETIC class tests. The
      *> OPERAND's class chooses between them, and 13.18.29.4 GR2 b) makes a GROUP-USAGE NATIONAL group "an
      *> elementary data item of usage national and class and category national" (kb/Work PB760).
      *> Here the two classifications genuinely DIFFER - alphanumeric en-US, national tr-TR - so the wrong table
      *> gives a different ANSWER, not the same one: in tr the uppercase of "i" is U+0130 (ORD 305) and the
      *> lowercase of "I" is U+0131 (ORD 306); in en they are "I" (ORD 74) and "i" (ORD 106).
      *>   NG   (national group)          -> national table    UP 305 / LO 306
      *>   NE   (its elementary PIC N twin) -> national table  UP 305 / LO 306   (must AGREE with NG)
      *>   NG (1:1) (ref-mod slice, class national, 8.4.3.3.4 GR6) -> national table  UP 305
      *>   NATIONAL-OF(XE) (a national function result)       -> national table  UP 305
      *>   XE   (PIC X control)            -> alphanumeric table UP 74 / LO 106
      *> Every outcome is witnessed by FUNCTION ORD, never by the console echo.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB760CLASSIFYNG.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. X CHARACTER CLASSIFICATION
           FOR ALPHANUMERIC IS EN FOR NATIONAL IS TR.
       SPECIAL-NAMES.
           LOCALE EN IS "en-US"
           LOCALE TR IS "tr-TR".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NG GROUP-USAGE NATIONAL.
          05 NG-A PIC N(2) VALUE N"ii".
       01 NGU GROUP-USAGE NATIONAL.
          05 NGU-A PIC N(2) VALUE N"II".
       01 NE  PIC N(2) VALUE N"ii".
       01 NEU PIC N(2) VALUE N"II".
       01 XE  PIC X(2) VALUE "ii".
       01 XEU PIC X(2) VALUE "II".
       01 NR  PIC N(2).
       01 XR  PIC X(2).
       01 K   PIC 9(5).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE FUNCTION UPPER-CASE(NG) TO NR
           MOVE FUNCTION ORD(NR(1:1)) TO K
           DISPLAY "UP-NG=" K
           MOVE FUNCTION UPPER-CASE(NE) TO NR
           MOVE FUNCTION ORD(NR(1:1)) TO K
           DISPLAY "UP-NE=" K
           MOVE FUNCTION UPPER-CASE(NG(1:1)) TO NR
           MOVE FUNCTION ORD(NR(1:1)) TO K
           DISPLAY "UP-NG-SLICE=" K
           MOVE FUNCTION UPPER-CASE(FUNCTION NATIONAL-OF(XE)) TO NR
           MOVE FUNCTION ORD(NR(1:1)) TO K
           DISPLAY "UP-NATIONAL-OF=" K
           MOVE FUNCTION UPPER-CASE(XE) TO XR
           MOVE FUNCTION ORD(XR(1:1)) TO K
           DISPLAY "UP-XE=" K
           MOVE FUNCTION LOWER-CASE(NGU) TO NR
           MOVE FUNCTION ORD(NR(1:1)) TO K
           DISPLAY "LO-NG=" K
           MOVE FUNCTION LOWER-CASE(NEU) TO NR
           MOVE FUNCTION ORD(NR(1:1)) TO K
           DISPLAY "LO-NE=" K
           MOVE FUNCTION LOWER-CASE(XEU) TO XR
           MOVE FUNCTION ORD(XR(1:1)) TO K
           DISPLAY "LO-XE=" K
           STOP RUN.
