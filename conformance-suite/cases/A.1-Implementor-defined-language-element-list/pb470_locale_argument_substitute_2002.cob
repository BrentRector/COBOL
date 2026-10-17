*>--- conformance-case
*> id: pb470_locale_argument_substitute_2002
*> rules: [DOC-A.1-90]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> The COBOL-2002 half of kb/Work PB470's witness. LOCALE-DATE and
      *> LOCALE-TIME are 2002 additions (Annex A.4.9 items 3 and 4); their
      *> 2014 sibling LOCALE-TIME-FROM-SECONDS and the FULL derivation of the
      *> expected values live in 2014/pb470_locale_argument_substitute.
      *>
      *> WHY A SECOND EDITION AT ALL. The substituted result of a rejected
      *> function reference is 15.3 rule 14's implementor-defined value, and
      *> docs/CONFORMANCE.md row DOC-A.1-90 states ONE determination for
      *> every edition this compiler targets. Nothing in the runtime guard is
      *> edition-conditional, and this file is what keeps that true: an
      *> edition gate added to the LOCALE family later cannot quietly hand
      *> COBOL-2002 a different answer than COBOL-2014 without turning red.
      *>
      *> IN BRIEF: 15.52.4 r3 and 15.53.4 r3 both say "The length of the
      *> returned value depends on the format indicated in the locale", so
      *> the returned length derives from the LOCALE and survives the
      *> rejection of argument-1 intact. Row DOC-A.1-90's zero-length class
      *> is only for a length "derived from the rejected argument", so its
      *> GENERAL clause applies and the answer for an alphanumeric result
      *> (15.52.1 / 15.53.1) is SPACES - one, on 15.30.3 r1's own answer for
      *> an alphanumeric function whose contents are absent.
      *> Each leg is read twice, since either kind alone is blind: a DISPLAY
      *> between delimiters (14.9.11.4 GR1 transfers nothing for a
      *> zero-length operand, so the defective answer prints []) and a
      *> FUNCTION LENGTH read-out (15.50.4 r3; 00000 defective, 00001
      *> correct). Nothing is MOVEd - 14.6.8.5 space-fills a receiver from a
      *> zero-length sender, which is what hid the defect for a year.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB470LOC02.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE FR IS "fr-FR".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-DOK    PIC X(8) VALUE "20260819".
       01 W-DBAD   PIC X(8) VALUE "20261399".
       01 W-TOK    PIC X(6) VALUE "130509".
       01 W-TFORM  PIC X(6) VALUE "13X509".
       01 W-TRANGE PIC X(6) VALUE "250000".
       01 W-LEN    PIC 9(5).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "DOK=[" FUNCTION LOCALE-DATE(W-DOK FR) "]"
           MOVE FUNCTION LENGTH(
               FUNCTION LOCALE-DATE(W-DOK FR)) TO W-LEN
           DISPLAY "DOKLEN=" W-LEN
           DISPLAY "DBAD=[" FUNCTION LOCALE-DATE(W-DBAD FR) "]"
           MOVE FUNCTION LENGTH(
               FUNCTION LOCALE-DATE(W-DBAD FR)) TO W-LEN
           DISPLAY "DBADLEN=" W-LEN
           DISPLAY "TOK=[" FUNCTION LOCALE-TIME(W-TOK FR) "]"
           MOVE FUNCTION LENGTH(
               FUNCTION LOCALE-TIME(W-TOK FR)) TO W-LEN
           DISPLAY "TOKLEN=" W-LEN
           DISPLAY "TFORM=[" FUNCTION LOCALE-TIME(W-TFORM FR) "]"
           MOVE FUNCTION LENGTH(
               FUNCTION LOCALE-TIME(W-TFORM FR)) TO W-LEN
           DISPLAY "TFORMLEN=" W-LEN
           DISPLAY "TRANGE=[" FUNCTION LOCALE-TIME(W-TRANGE FR) "]"
           MOVE FUNCTION LENGTH(
               FUNCTION LOCALE-TIME(W-TRANGE FR)) TO W-LEN
           DISPLAY "TRANGELEN=" W-LEN
           STOP RUN.
