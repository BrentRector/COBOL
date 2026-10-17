*>--- conformance-case
*> id: pb64t1_locale_declare
*> rules: [GR-12.3.6.4-12, SR-12.3.7.3-24, GR-12.3.7.4-5, GR-14.6.6-4, GR-14.6.6-5, DOC-A.1-181]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 12.3.7.2 — the SPECIAL-NAMES LOCALE clause, `LOCALE locale-name-1 IS {external-locale-name-1 | literal-4}`,
      *> and the NAMED form of the ALPHABET LOCALE phrase, `ALPHABET a IS LOCALE locale-name-2` (12.3.7.3 SR24) —
      *> increment T1 of docs/rearchitecture/DESIGN-locale-facility.md (kb/Work PB64; Annex A.4.9 item 10, the clause half).
      *>
      *> What each line proves:
      *>   ROOT   — the program collating sequence (CUR — IS LOCALE without a name, 12.3.7.4 GR7e) under the
      *>            harness's pinned user default (the root / INVARIANT order): n-tilde is n + tilde at the
      *>            secondary level, so "nz" > "nu-tilde" (z > u at level 1 decides).
      *>   ES1/2/3 — DETERMINATION L1: the external identification is NORMALIZED before lookup — the literal branch
      *>            "es_ES.UTF-8" (the .codeset suffix ignored), the external-locale-name word es_ES (a legal word,
      *>            8.3.2.1 admits the underscore), and the BCP-47 literal "es-ES" all identify ONE locale
      *>            (8.5.3.1 rule 2's "same external identification"): under each, SET LOCALE LC_COLLATE makes
      *>            n-tilde a PRIMARY after n (CLDR es: &N<ñ<<<Ñ), so "nz" < "nu-tilde".
      *>   NAMED  — an alphabet bound to a NAMED locale (SWE IS LOCALE SV) collates by THAT locale whatever the
      *>            current locale is: the SORT below names SWE in its COLLATING SEQUENCE while LC_COLLATE is
      *>            Spanish, and the Swedish order puts o-umlaut AFTER z (CLDR sv: &[before 1]ǀ<…<ö) — a
      *>            root/Spanish order would put "öre" before "zebra" (o + umlaut).
      *>   BACK   — SET LOCALE LC_COLLATE TO USER-DEFAULT restores the root order (14.9.39.4 GR23b).
      *> Non-ASCII appears only inside literals (UTF-8 source); every DISPLAY is ASCII.
      *> CLAUSE ORDER: the 12.3.7.2 format prints alphabet-name-clause BEFORE the LOCALE clause, and 5.2.1
      *> requires that sequence, so SWE's locale-name-2 is a FORWARD reference. This program used to write the
      *> LOCALE clauses first — non-conforming source, and the only order the compiler accepted (kb/Work PB1558).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB64T1DECL.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. X PROGRAM COLLATING SEQUENCE IS CUR.
       SPECIAL-NAMES.
           ALPHABET CUR IS LOCALE
           ALPHABET SWE IS LOCALE SV
           LOCALE ES1 IS "es_ES.UTF-8"
           LOCALE ES2 IS es_ES
           LOCALE ES3 IS "es-ES"
           LOCALE SV IS sv_SE.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SORT-FILE ASSIGN TO "PB64T1DECL.SRT".
       DATA DIVISION.
       FILE SECTION.
       SD  SORT-FILE.
       01  SORT-REC         PIC X(5).
       WORKING-STORAGE SECTION.
       01  A               PIC X(2) VALUE "nz".
       01  B               PIC X(2) VALUE "ñu".
       01  VERDICT         PIC X.
       01  EOF-FLAG        PIC X.
       PROCEDURE DIVISION.
       MAIN.
           PERFORM CMP
           DISPLAY "ROOT=" VERDICT
           SET LOCALE LC_COLLATE TO ES1
           PERFORM CMP
           DISPLAY "ES1=" VERDICT
           SET LOCALE LC_COLLATE TO ES2
           PERFORM CMP
           DISPLAY "ES2=" VERDICT
           SET LOCALE LC_COLLATE TO ES3
           PERFORM CMP
           DISPLAY "ES3=" VERDICT
           SORT SORT-FILE ON ASCENDING KEY SORT-REC
               COLLATING SEQUENCE IS SWE
               INPUT PROCEDURE IS FEED
               OUTPUT PROCEDURE IS DRAIN
           SET LOCALE LC_COLLATE TO USER-DEFAULT
           PERFORM CMP
           DISPLAY "BACK=" VERDICT
           STOP RUN.
       CMP.
           IF A < B MOVE "<" TO VERDICT ELSE MOVE ">" TO VERDICT END-IF.
       FEED.
           MOVE "zebra" TO SORT-REC  RELEASE SORT-REC
           MOVE "öre  " TO SORT-REC  RELEASE SORT-REC
           MOVE "apple" TO SORT-REC  RELEASE SORT-REC.
       DRAIN.
           MOVE "N" TO EOF-FLAG
           PERFORM UNTIL EOF-FLAG = "Y"
               RETURN SORT-FILE AT END MOVE "Y" TO EOF-FLAG
                   NOT AT END PERFORM SHOW
               END-RETURN
           END-PERFORM.
       SHOW.
           IF SORT-REC = "zebra" DISPLAY "NAMED=zebra"
           ELSE IF SORT-REC = "apple" DISPLAY "NAMED=apple"
           ELSE DISPLAY "NAMED=o-umlaut-re" END-IF END-IF.
