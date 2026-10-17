*>--- conformance-case
*> id: pb319_sr8_antecedent
*> rules: [SR-14.9.27.3-8]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB319 - THE COMPLEMENT OF 14.9.27.3 SR8: the rule's
      *> ANTECEDENT, written out and then falsified in each of the two ways
      *> a compilable program can falsify it.
      *> THE RULE (ISO 14.9.27.3 SR8): "When file-name-1 is not subject to
      *> an APPLY COMMIT clause, then if the sharing phrase is omitted from
      *> the OPEN statement and the ALL phrase is specified in the SHARING
      *> clause of the file control entry for file-name-1 or if the ALL
      *> phrase is specified on the OPEN statement, the LOCK MODE clause
      *> shall be specified in the file control entry for file-name-1."
      *> Two disjuncts, and BOTH name an OPEN statement. So:
      *>   (a) F-OVR declares SHARING WITH ALL OTHER and has NO LOCK MODE
      *>       clause, and its OPEN writes its own sharing phrase, NO
      *>       OTHER. Disjunct 1 needs the phrase OMITTED; disjunct 2 needs
      *>       ALL on the OPEN. Neither holds - the program is LEGAL.
      *>   (b) F-NEV declares SHARING WITH ALL OTHER and has NO LOCK MODE
      *>       clause and is NEVER OPENED. SR8 speaks only about an OPEN
      *>       statement's file-name-1, so it never speaks - LEGAL.
      *> THAT THIS PROGRAM COMPILES AT ALL IS THE ASSERTION for both. It
      *> used to be rejected COBOLNET1512 at the SELECT line, by a second
      *> copy of SR8 in DataBinder.BindFileControl that had to drop the
      *> antecedent because a file control entry cannot see an OPEN.
      *> THE PRINTED STATUSES stop a compiler from passing this by simply
      *> IGNORING the sharing phrase: if NO OTHER did not override the
      *> file control entry's ALL OTHER (14.9.27.4 GR23 sentence 1), the
      *> second connector's open would SUCCEED and print 00 instead of 61.
      *> The complement - SR8 still firing when its antecedent HOLDS - is
      *> negative/sharing-all-no-lockmode (the file control clause arm) and
      *> negative/pb316-open-sharing-all-no-lockmode (the OPEN phrase arm).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB319SR8ANT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
      *> (a) SHARING WITH ALL OTHER, NO LOCK MODE clause - legal because
      *>     every OPEN of it below writes its own non-ALL sharing phrase.
           SELECT F-OVR ASSIGN TO "pb319sr8.dat"
               ORGANIZATION IS SEQUENTIAL
               SHARING WITH ALL OTHER
               FILE STATUS IS WS-OVR.
      *> A second connector on the SAME physical file. It has a LOCK MODE
      *> clause, so SR8 is satisfied for it however it is opened; it exists
      *> only to OBSERVE the sharing mode F-OVR's phrase established.
           SELECT F-RIV ASSIGN TO "pb319sr8.dat"
               ORGANIZATION IS SEQUENTIAL
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL
               FILE STATUS IS WS-RIV.
      *> (b) SHARING WITH ALL OTHER, NO LOCK MODE clause, NEVER OPENED.
           SELECT F-NEV ASSIGN TO "pb319nev.dat"
               ORGANIZATION IS SEQUENTIAL
               SHARING WITH ALL OTHER.
       DATA DIVISION.
       FILE SECTION.
       FD F-OVR.
       01 R-OVR PIC X(5).
       FD F-RIV.
       01 R-RIV PIC X(5).
       FD F-NEV.
       01 R-NEV PIC X(5).
       WORKING-STORAGE SECTION.
       01 WS-OVR PIC XX.
       01 WS-RIV PIC XX.
       PROCEDURE DIVISION.
       MAIN.
      *> (a) the phrase is present and is NOT ALL - SR8's antecedent fails.
           OPEN OUTPUT SHARING WITH NO OTHER F-OVR
           DISPLAY "OVR=" WS-OVR
           MOVE "HELLO" TO R-OVR
           WRITE R-OVR
      *> The phrase really took effect: F-OVR holds the file "sharing with
      *> no other", so this request is Table 19 row SHARING WITH ALL OTHER
      *> / INPUT against that column - Unsuccessful open - and 9.1.13.9
      *> item 1 a) makes the I-O status 61.
           OPEN INPUT F-RIV
           DISPLAY "RIV1=" WS-RIV
           CLOSE F-OVR
      *> With the conflicting connector closed, the SAME open succeeds:
      *> Table 18 row INPUT, file available - Normal open - so 00, and the
      *> record written above reads back. That is what proves the 61 was
      *> the sharing conflict and not a standing property of F-RIV.
           OPEN INPUT F-RIV
           DISPLAY "RIV2=" WS-RIV
           READ F-RIV
           DISPLAY "REC=" R-RIV
           CLOSE F-RIV
      *> (b) reached only because the never-opened F-NEV compiled.
           DISPLAY "NEV-DECLARED"
           STOP RUN.
