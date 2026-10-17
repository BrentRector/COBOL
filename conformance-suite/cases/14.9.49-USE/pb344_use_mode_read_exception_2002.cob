*>--- conformance-case
*> id: pb344_use_mode_read_exception_2002
*> rules: [GR-14.9.49.4-6, DOC-A.1-218]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB344 — the ISO edition the compilation targets reaches the file connectors and the
      *> generated USE-declarative selector.  Until PB344 no edition reached either, so the 2023 rules
      *> were served to --std 85, 2002 and 2014 as well.
      *>
      *> THE RULES, and every expected value below derived from them (no observation):
      *>  14.9.49.4 GR6 b)–e) — the open-mode-scoped declarative, "for any file open in the input mode or in
      *>    the process of being opened in the input mode" and its OUTPUT / I-O / EXTEND siblings.  (R1..R4)
      *>  9.1.13.1 — the I-O status is set before the declarative runs; '47' is a READ attempted on a file not
      *>    open in the input or I-O mode (R1), '10' the at end condition (R2), '46' a sequential READ with no
      *>    valid next record after an unsuccessful one (R3), '35' a failed OPEN INPUT of a file that is not
      *>    present and not optional (R4).
      *>  14.9.30.4 GR24 a) — the at end condition's '10'.                                             (R2)
      *>
      *> EDITIONS - R1..R4 ARE IDENTICAL AT 85, 2002, 2014 AND 2023, and that is the DETERMINATION this
      *> program pins (kb/Work PB344).  Annex E.2 item 19 b) reads "READ processing.  If an exception that is
      *> not an invalid key or at end occurs, any declarative that specifies INPUT or I-O would not have been
      *> executed.  It will now be executed", which read as a behaviour change would make R1 and R3
      *> edition-dependent.  It is not one.  Annex E is INFORMATIVE, E.1 scopes it to "a list of the
      *> substantive changes between the previous COBOL standard and this Working Draft International
      *> Standard" - ONE prior edition - and its own justification says the previous standard "was not clear
      *> or missing processing of some I-O exceptions", each sub-item adding "This appears to be an error in
      *> previous standards".  A silence is not a prohibition.  For the 1985 edition the behaviour is fixed
      *> the other way by that edition's own validation suite: NIST CCVS SQ122A/SQ136A/SQ137A/SQ138A require
      *> the INPUT declarative to run for a '46' READ and SQ148A the OUTPUT one for a '47' READ - SQ137A
      *> fails with the literal remark "INPUT DECLARATIVE NOT EXECUTED" against ANSI X3.23-1985 VII-2 1.3.5,
      *> VII-51 4.6.4(5).  GR6's open-mode tier is therefore edition-invariant and all four copies of this
      *> program expect the same lines.  VERSION_CHANGE_REFERENCE row 26.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB344R02.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb344r02f.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS F-K
               FILE STATUS IS F-ST.
           SELECT H ASSIGN TO "pb344r02h.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS H-ST.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC.
          05 F-K PIC X(4).
          05 F-V PIC X(6).
       FD H.
       01 H-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01 F-ST PIC XX.
       01 H-ST PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-IN SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON INPUT.
       D-IN-P.
           DISPLAY "DECL-IN".
       D-OUT SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON OUTPUT.
       D-OUT-P.
           DISPLAY "DECL-OUT".
       END DECLARATIVES.
       MAIN SECTION.
       M-R1.
      *> R1 — a READ on a file open in the OUTPUT mode: '47', an exception that is neither at end nor
      *>      invalid key.
           OPEN OUTPUT F
           READ F NEXT RECORD END-READ
           DISPLAY "R1=" F-ST
           CLOSE F.
       M-R2.
      *> R2 — the AT END family with no AT END phrase written: the tier runs at EVERY edition.
           OPEN INPUT F
           READ F NEXT RECORD END-READ
           DISPLAY "R2=" F-ST.
       M-R3.
      *> R3 — a further sequential READ after the unsuccessful one: '46', again neither at end nor invalid key.
           READ F NEXT RECORD END-READ
           DISPLAY "R3=" F-ST
           CLOSE F.
       M-R4.
      *> R4 — a FAILED OPEN INPUT: not a READ, so the tier runs at every edition, through GR6 b)'s
      *>      "in the process of being opened in the input mode" half.
           OPEN INPUT H
           DISPLAY "R4=" H-ST
           STOP RUN.
