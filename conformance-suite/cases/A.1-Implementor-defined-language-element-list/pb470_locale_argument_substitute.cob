*>--- conformance-case
*> id: pb470_locale_argument_substitute
*> rules: [DOC-A.1-90]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 15.3 rule 14 - the result when a function's ARGUMENT rules are
      *> violated and checking for EC-ARGUMENT-FUNCTION is not enabled - at
      *> the LOCALE-DATE / LOCALE-TIME / LOCALE-TIME-FROM-SECONDS sites of
      *> docs/CONFORMANCE.md row DOC-A.1-90. Landed with the kb/Work PB470
      *> fix, which moved all four guards out of the row's zero-length class
      *> and into its general "spaces" clause.
      *>
      *> WHY THIS GOLDEN HAD TO BE WRITTEN AT ALL. The path was already
      *> exercised - 2014/pb64t4_locale_functions has
      *>     MOVE FUNCTION LOCALE-DATE("20261399") TO S
      *>     DISPLAY "ARG=[" FUNCTION TRIM(S) "]"
      *> expecting ARG=[] - and that leg is GREEN under either answer:
      *> 14.6.8.5 says "If the sending data item or literal is zero-length,
      *> the entire receiving data item is space filled", so a zero-length
      *> sender and a one-space sender leave S identical and TRIM erases the
      *> difference. Nothing here MOVEs a result; every leg reads the
      *> function reference DIRECTLY.
      *>
      *> THE RULE, and which class each site is in.
      *> 15.3 rule 14: "If the EC-ARGUMENT-FUNCTION exception condition is
      *> set to exist and checking for EC-ARGUMENT-FUNCTION is not enabled,
      *> the implementor defines the result of the function reference."
      *> Row DOC-A.1-90 settles the class by asking what determines the
      *> returned item's LENGTH. Its zero-length class is the functions
      *> "where the returned LENGTH is itself derived from the rejected
      *> argument" - nothing survives to size a result. For these three,
      *> 15.52.4 r3, 15.53.4 r3 and 15.54.4 r3 all say the same thing: "The
      *> length of the returned value depends on the format indicated in the
      *> locale." The length therefore derives from the LOCALE, which
      *> rejecting argument-1 leaves untouched, so the zero-length class does
      *> not reach these and the row's GENERAL clause does - and 15.52.1 /
      *> 15.53.1 / 15.54.1 each say "The function type is alphanumeric", so
      *> the answer is SPACES.
      *> ONE space, because DETERMINATION L10 renders d_fmt / t_fmt as the
      *> culture's own patterns, whose width varies with the VALUE as well as
      *> with the format (under en-US's M/d/yyyy a date is 8 or 10 positions
      *> wide), so once the value is rejected the format alone fixes no width
      *> to fill. The standard answers that exact shape itself at 15.30.3
      *> r1 - an alphanumeric function whose length is "based on its
      *> contents" (r2) returns "one alphanumeric space character" when the
      *> contents are absent - and the row adopts it.
      *>
      *> WHAT EACH LEG MEASURES. Two observation kinds per site, because
      *> either alone is worthless:
      *>   [...] a DISPLAY of the reference between delimiters. 14.9.11.4
      *>         GR1 - "If an operand is a zero-length data item or a
      *>         zero-length literal, no data is transferred for that
      *>         operand" - so the defective zero-length answer prints [] and
      *>         the correct answer prints [ ].
      *>   ...LEN a FUNCTION LENGTH read-out. 15.50.4 r3 - "the returned
      *>         value is an integer equal to the length of argument-1 in
      *>         alphanumeric character positions" - 00000 defective, 00001
      *>         correct. 15.50.3 r1 admits a data item of any class as the
      *>         argument, 8.4.3.2.1 makes a function-identifier a reference
      *>         to "the unique data item that results from the evaluation of
      *>         a function", and 8.4.3.2.4 r2 permits one as an argument.
      *> Each of the four rejected sites sits beside a legal CONTROL leg
      *> through the same locale, so a leg reporting one space cannot be
      *> mistaken for "the function returns one space".
      *>
      *> THE FOUR REJECTED SITES, one per guard:
      *>   DBAD   15.52.3 r2 - argument-1 shall be "valid according to the
      *>          definition of a returned value from" CURRENT-DATE. Month 13
      *>          is not.
      *>   TFORM  15.53.3 r1/r2 - argument-1 shall be in CURRENT-DATE
      *>          positions 9 through 14 form. "13X509" is not.
      *>   TRANGE 15.53.3 r3 a) - "The hours past midnight shall be 00
      *>          through 24." 25 is not. A SECOND guard, deliberately
      *>          measured apart from TFORM: the two were separate arms
      *>          before PB470 and each wrote its own substitute.
      *>   SBAD   15.54.3 r1 - "Argument-1 shall be a numeric value in
      *>          standard numeric time form", whose range 7.3.17.4 GR5 sets
      *>          at "greater than or equal to zero and less than 86,400"
      *>          with LEAP-SECOND OFF, the implied default. 90000 is not.
      *>
      *> RAISED / ON are the other arm of the same rule, measured because
      *> row DOC-A.1-90 asserts it: "The exception condition is always SET,
      *> so a declarative sees it whenever checking is on; only the
      *> substituted result is defined here." With CHECKING ON the same
      *> rejected argument reaches the declarative instead of substituting,
      *> and because EC-ARGUMENT-FUNCTION is FATAL (Table 13) the MOVE is
      *> interrupted - W-ON keeps its "???" sentinel, so NO substituted value
      *> is stored at all. This is the one leg that uses a MOVE, and it uses
      *> it precisely to show that nothing arrived.
      *>
      *> Every argument is a DATA ITEM, so each value reaches the runtime
      *> guard rather than a bind-time literal screen (LOCALE-DATE's
      *> argument-1 width is screened at bind, 15.52.3 r1).
      *> A 2014 golden because LOCALE-TIME-FROM-SECONDS is a 2014 addition;
      *> 2002/pb470_locale_argument_substitute_2002 measures the same
      *> determination at the two functions COBOL-2002 already had.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB470LOCSB.
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
       01 W-SOK    PIC 9(6) VALUE 47109.
       01 W-SBAD   PIC 9(6) VALUE 90000.
       01 W-LEN    PIC 9(5).
       01 W-ON     PIC X(3) VALUE "???".
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-ARGUMENT-FUNCTION.
       H-P.
           DISPLAY "RAISED=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
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
           DISPLAY "SOK=["
               FUNCTION LOCALE-TIME-FROM-SECONDS(W-SOK FR) "]"
           MOVE FUNCTION LENGTH(
               FUNCTION LOCALE-TIME-FROM-SECONDS(W-SOK FR)) TO W-LEN
           DISPLAY "SOKLEN=" W-LEN
           DISPLAY "SBAD=["
               FUNCTION LOCALE-TIME-FROM-SECONDS(W-SBAD FR) "]"
           MOVE FUNCTION LENGTH(
               FUNCTION LOCALE-TIME-FROM-SECONDS(W-SBAD FR)) TO W-LEN
           DISPLAY "SBADLEN=" W-LEN
       >>TURN EC-ARGUMENT-FUNCTION CHECKING ON
           MOVE FUNCTION LOCALE-DATE(W-DBAD FR) TO W-ON
       >>TURN EC-ARGUMENT-FUNCTION CHECKING OFF
           DISPLAY "ON=[" W-ON "]"
           STOP RUN.
