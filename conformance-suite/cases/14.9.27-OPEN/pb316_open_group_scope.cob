*>--- conformance-case
*> id: pb316_open_group_scope
*> rules: [GR-14.9.27.4-20, GR-14.9.27.4-22, GR-14.9.27.4-23,
*>    GR-14.9.27.4-24, FMT-14.9.27.2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB316OPNGRP.
      *> kb/Work PB316 - the OPEN statement's general format (ISO
      *> 14.9.27.2) has TWO nested brace pairs. The OUTER pair encloses
      *> the whole group
      *>     {open-mode} [sharing-phrase] [retry-phrase]
      *>                                {file-name-1 [WITH NO REWIND]} ...
      *> and carries its own trailing ellipsis, so the sharing- and
      *> retry-phrases sit INSIDE the repeated group, beside the open mode
      *> and the file-names they govern - confirmed against the printed
      *> page, not only the transcription.
      *>
      *> 14.9.27.4 GR20 then fixes the semantics: "If more than one
      *> file-name is specified in an OPEN statement, the result of
      *> executing this OPEN statement is the same as if a separate OPEN
      *> statement had been written for each file-name in the same order
      *> as specified in the OPEN statement. These separate OPEN
      *> statements would each have the same open mode specification, the
      *> sharing-phrase, retry-phrase, and REWIND phrase as specified in
      *> the OPEN statement." The open mode in that list is unarguably
      *> per group - a multi-group OPEN exists to open files in differing
      *> modes - so the phrases listed beside it are per group too.
      *> 14.9.27.4 GR23 bounds the other direction: "If there is no
      *> SHARING phrase on the OPEN statement, then file sharing is
      *> completely specified in the file control entry."
      *>
      *> THE SHAPE OF THE TEST: every arbitration below is written TWICE -
      *> once as separate OPEN statements (GR20's own reference form) and
      *> once as one statement with the same groups - and the two forms
      *> shall print the same statuses. That is GR20 asserted directly,
      *> and it does not depend on any implementor-defined default.
      *>
      *> RE-TRUED BY kb/Work PB321 (landing train 9). REF-A / ONE-A read
      *> '00' when this golden was written, because an OPEN of a file
      *> carrying no SHARING and no LOCK MODE clause was not arbitrated
      *> against the physical-file registry at all - so F-SEED, open on
      *> pb316grp.dat, was INVISIBLE to F-A's arbitration. PB321 made
      *> 14.9.27.4 Table 19 arbitrate EVERY open, and 9.1.15 is explicit
      *> for the sharing-with-no-other row: "Associating this file
      *> connector with the physical file will be unsuccessful if the
      *> physical file is currently open through other file connectors."
      *> F-A declares SHARING WITH NO OTHER and F-SEED holds the physical
      *> file open, so '61' (9.1.13.9 item 1) is the conforming answer and
      *> the old '00' was the hole. GR20's assertion is untouched: the two
      *> forms still agree, now at '61'.
      *>
      *> KNOWN WEAKENING, recorded rather than hidden: with every open
      *> arbitrated, parts (1) and (2) no longer DISCRIMINATE a leak of
      *> F-E's RETRY phrase into F-SEED's group. A leaked retry would
      *> route F-SEED through the phrase-bearing entry, which registers it
      *> with the same UNDETERMINED sharing mode an unregistered connector
      *> already arbitrates under, so no printed status moves. Parts (3)
      *> and (4) (the SHARING phrase) and part (5) (SR8, at compile time)
      *> still discriminate their leaks.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
      *> Three connectors on ONE physical file. F-SEED carries neither a
      *> SHARING clause nor a LOCK MODE clause; F-A declares SHARING WITH
      *> NO OTHER; F-E declares SHARING WITH READ ONLY.
           SELECT F-SEED ASSIGN TO "pb316grp.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS SEED-ST.
           SELECT F-A ASSIGN TO "pb316grp.dat"
               ORGANIZATION IS SEQUENTIAL
               SHARING WITH NO OTHER
               FILE STATUS IS A-ST.
           SELECT F-B ASSIGN TO "pb316grp.dat"
               ORGANIZATION IS SEQUENTIAL
               LOCK MODE IS MANUAL
               FILE STATUS IS B-ST.
           SELECT F-E ASSIGN TO "pb316grp.dat"
               ORGANIZATION IS SEQUENTIAL
               SHARING WITH READ ONLY
               FILE STATUS IS E-ST.
      *> Two connectors on their own files, for the SR8 half. F-C carries
      *> NEITHER a SHARING clause NOR a LOCK MODE clause, so it is the
      *> file 14.9.27.3 SR8 would reject if a sibling group's ALL phrase
      *> reached it.
           SELECT F-C ASSIGN TO "pb316gc.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS C-ST.
           SELECT F-D ASSIGN TO "pb316gd.dat"
               ORGANIZATION IS SEQUENTIAL
               LOCK MODE IS MANUAL
               FILE STATUS IS D-ST.
       DATA DIVISION.
       FILE SECTION.
       FD F-SEED.
       01 SEED-REC PIC X(5).
       FD F-A.
       01 A-REC PIC X(5).
       FD F-B.
       01 B-REC PIC X(5).
       FD F-E.
       01 E-REC PIC X(5).
       FD F-C.
       01 C-REC PIC X(5).
       FD F-D.
       01 D-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01 SEED-ST PIC XX.
       01 A-ST    PIC XX.
       01 B-ST    PIC XX.
       01 E-ST    PIC XX.
       01 C-ST    PIC XX.
       01 D-ST    PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F-SEED.
           MOVE "AAAAA" TO SEED-REC.
           WRITE SEED-REC.
           CLOSE F-SEED.
      *> ------------------------------------------------------------
      *> (1) THE RETRY PHRASE is nested in the same repeated group as the
      *> sharing phrase, so it is scoped the same way. Its leak is not a
      *> status of its own - it is that a file in a group carrying NO
      *> phrase gets treated as a file-sharing participant, which changes
      *> what LATER opens arbitrate against and so escapes the statement.
      *> GR20's reference form first: F-SEED, whose group carries no
      *> phrase and whose file control entry specifies no sharing, then a
      *> retry group on F-E, then an OPEN of the no-other connector F-A
      *> while F-SEED is still open.
           OPEN INPUT F-SEED.
           OPEN INPUT RETRY 2 TIMES F-E.
           CLOSE F-E.
           OPEN INPUT F-A.
           DISPLAY "REF-S=" SEED-ST " REF-E=" E-ST " REF-A=" A-ST.
           CLOSE F-A.
           CLOSE F-SEED.
      *> (2) The same two groups as ONE statement. GR20 requires all
      *> three statuses to match (1) exactly: F-SEED's group carries
      *> neither phrase, so by GR23 its sharing stays whatever its file
      *> control entry gives it - which is what (1) measured - and F-E's
      *> RETRY phrase shall not reach it.
           OPEN INPUT F-SEED INPUT RETRY 2 TIMES F-E.
           CLOSE F-E.
           OPEN INPUT F-A.
           DISPLAY "ONE-S=" SEED-ST " ONE-E=" E-ST " ONE-A=" A-ST.
           CLOSE F-A.
           CLOSE F-SEED.
      *> ------------------------------------------------------------
      *> (3) THE SHARING PHRASE, GR20's reference form: one OPEN
      *> statement per file-name. F-A opens under its own file control
      *> SHARING WITH NO OTHER; F-B then requests SHARING WITH ALL OTHER
      *> in the INPUT mode against that existing no-other connector,
      *> which Table 19 (row SHARING WITH ALL OTHER / INPUT, column
      *> "sharing with no other") makes an "Unsuccessful open" - I-O
      *> status '61', 9.1.13.9 item 1. F-B is therefore NOT open and is
      *> not closed. These two parts come LAST among the shared-file
      *> parts precisely because a compiler that leaks the phrase leaves
      *> F-B open, and nothing after them may depend on that.
           OPEN INPUT F-A.
           OPEN INPUT SHARING WITH ALL OTHER F-B.
           DISPLAY "REF-A2=" A-ST " REF-B=" B-ST.
           CLOSE F-A.
      *> (4) The SAME two groups written as ONE statement. GR20 requires
      *> the identical result: F-A's group carries no sharing phrase, so
      *> by GR23 its sharing is still completely specified in its file
      *> control entry, and F-B's ALL OTHER stays inside F-B's group.
           OPEN INPUT F-A INPUT SHARING WITH ALL OTHER F-B.
           DISPLAY "ONE-A2=" A-ST " ONE-B=" B-ST.
           CLOSE F-A.
      *> ------------------------------------------------------------
      *> (5) 14.9.27.3 SR8 is a per-group rule for the same reason: "if
      *> the sharing phrase is omitted from the OPEN statement and the
      *> ALL phrase is specified in the SHARING clause of the file
      *> control entry for file-name-1 or if the ALL phrase is specified
      *> on the OPEN statement, the LOCK MODE clause shall be specified
      *> in the file control entry for file-name-1". F-C's group carries
      *> no sharing phrase and F-C's file control entry carries no
      *> SHARING clause, so neither antecedent holds for F-C and its
      *> missing LOCK MODE clause is legal. The ALL phrase belongs to
      *> F-D's group alone. Both orderings are the same repeated group
      *> and shall compile alike - this program compiling IS that
      *> assertion.
           OPEN OUTPUT SHARING WITH ALL OTHER F-D OUTPUT F-C.
           DISPLAY "FWD-D=" D-ST " FWD-C=" C-ST.
           CLOSE F-D.
           CLOSE F-C.
           OPEN OUTPUT F-C OUTPUT SHARING WITH ALL OTHER F-D.
           DISPLAY "REV-C=" C-ST " REV-D=" D-ST.
           CLOSE F-C.
           CLOSE F-D.
           STOP RUN.
