*>--- conformance-case
*> id: pb64t1_set_locale_categories
*> rules: [GR-14.6.6-1, GR-14.6.6-3, GR-14.9.39.4-22, GR-14.9.39.4-23,
*>    GR-14.9.39.4-25]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.39 Format 11 (set-locale) — `SET LOCALE {category… | USER-DEFAULT} TO {locale-name | USER-DEFAULT |
      *> SYSTEM-DEFAULT}` over the run unit's ONE locale state (8.2.1; 14.6.6) — increment T1 of
      *> docs/rearchitecture/DESIGN-locale-facility.md (kb/Work PB64; Annex A.4.9 item 9).
      *>
      *> The printed format's category brace carries CHOICE INDICATORS (5.2.6.4): one or more categories, each at most
      *> once, in ANY order — so `SET LOCALE LC_NUMERIC LC_TIME TO ES` is one statement naming two categories (a scalar
      *> category model would reject it). The harness pins the user AND system defaults to the root.
      *>
      *> What each line proves:
      *>   START   — at run-unit activation every category is the user default (14.6.6 r1): root order, "nz" > "ñu".
      *>   OTHERS  — 14.6.6 r3 / 14.9.39.4 GR23a: switching LC_NUMERIC and LC_TIME (two categories, order-free) leaves
      *>             LC_COLLATE UNCHANGED — the relation still answers the root order.
      *>   COLLATE — switching LC_COLLATE to ES makes n-tilde a primary after n: "nz" < "ñu".
      *>   REORDER — the same statement with the categories in the other order and LC_COLLATE among them; LC_ALL
      *>             beside a named category is two different alternatives (redundant, legal).
      *>   SYSDEF  — GR23c: LC_ALL TO SYSTEM-DEFAULT → the root order again.
      *>   USERDEF — GR22: `SET LOCALE USER-DEFAULT TO ES` sets the USER DEFAULT; the categories already current do
      *>             NOT move (r1 made them a copy at activation) — still root …
      *>   FROMUD  — … until GR23b `SET LOCALE LC_COLLATE TO USER-DEFAULT` takes LC_COLLATE from the (now Spanish)
      *>             user default: "nz" < "ñu".
      *>   GR25    — each category stays until another SET names it: LC_ALL TO SYSTEM-DEFAULT then LC_TIME TO ES
      *>             leaves LC_COLLATE at the root.
      *> Non-ASCII appears only inside literals (UTF-8 source); every DISPLAY is ASCII.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB64T1CATS.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. X PROGRAM COLLATING SEQUENCE IS CUR.
       SPECIAL-NAMES.
           ALPHABET CUR IS LOCALE
           LOCALE ES IS "es-ES".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A               PIC X(2) VALUE "nz".
       01  B               PIC X(2) VALUE "ñu".
       01  VERDICT         PIC X.
       PROCEDURE DIVISION.
       MAIN.
           PERFORM CMP
           DISPLAY "START=" VERDICT
           SET LOCALE LC_NUMERIC LC_TIME TO ES
           PERFORM CMP
           DISPLAY "OTHERS=" VERDICT
           SET LOCALE LC_COLLATE TO ES
           PERFORM CMP
           DISPLAY "COLLATE=" VERDICT
           SET LOCALE LC_ALL TO SYSTEM-DEFAULT
           SET LOCALE LC_TIME LC_COLLATE LC_ALL TO ES
           PERFORM CMP
           DISPLAY "REORDER=" VERDICT
           SET LOCALE LC_ALL TO SYSTEM-DEFAULT
           PERFORM CMP
           DISPLAY "SYSDEF=" VERDICT
           SET LOCALE USER-DEFAULT TO ES
           PERFORM CMP
           DISPLAY "USERDEF=" VERDICT
           SET LOCALE LC_COLLATE TO USER-DEFAULT
           PERFORM CMP
           DISPLAY "FROMUD=" VERDICT
           SET LOCALE LC_ALL TO SYSTEM-DEFAULT
           SET LOCALE LC_TIME TO ES
           PERFORM CMP
           DISPLAY "GR25=" VERDICT
           STOP RUN.
       CMP.
           IF A < B MOVE "<" TO VERDICT ELSE MOVE ">" TO VERDICT END-IF.
