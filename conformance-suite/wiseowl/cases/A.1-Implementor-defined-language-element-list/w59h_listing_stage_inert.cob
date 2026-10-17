*>--- conformance-case
*> id: w59h_listing_stage_inert
*> rules: [DOC-A.1-200]
*> accept: [2002]
*> files: [w59hcb.cpy]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1536 Q1 / R43 item 3 - Annex A.1 item 200 (the STAGE of
      *> processing the LISTING and PAGE directives and the SUPPRESS phrase
      *> of COPY) is conditioned on the implementor producing listings, and
      *> COBOL.NET never produces one (A.1 item 117): docs/CONFORMANCE.md
      *> section 7 records item 200 as "Condition absent.".
      *>   cite.py --check A.1 "Conditionally required: If the associated
      *>     feature or language element is implemented then this element
      *>     is also required." -> OK A.1
      *>   cite.py --check 7.3.18.3 "If the compiler does not produce a
      *>     source listing, the LISTING directive shall be ignored"
      *>     -> OK 7.3.18.3 1)
      *>   cite.py --check 7.3.19.4 "If a source listing is not being
      *>     produced, a PAGE directive shall have no effect."
      *>     -> OK 7.3.19.4 3)
      *>   cite.py --check 7.2.3.4 "If the SUPPRESS phrase is specified,
      *>     library text incorporated as a result of COPY statement
      *>     processing is not listed." -> OK 7.2.3.4 4)
      *> THIS PROGRAM IS THE WITNESS that none of the three reaches the
      *> program: each is written at a point where it would change a
      *> listing, and the run is exactly the run without them.
      *> DERIVATION of every .out line:
      *>   V=ABC   the copybook's 01 W-V VALUE "ABC" is incorporated in
      *>           full; SUPPRESS PRINTING governs only its LISTING.
      *>   N=2     the procedure between >>LISTING OFF and >>LISTING ON
      *>           and after >>PAGE is compiled: ADD 1 runs twice.
      *> 2002 dir: >>LISTING and >>PAGE (7.3) are COBOL-2002 directives.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W59HLIST.
       >>LISTING OFF
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       COPY w59hcb SUPPRESS PRINTING.
       01 N PIC 9 VALUE 0.
       >>PAGE STAGE OF PROCESSING
       PROCEDURE DIVISION.
       MAIN.
           ADD 1 TO N
       >>LISTING ON
           ADD 1 TO N
           DISPLAY "V=" W-V
           DISPLAY "N=" N
           STOP RUN.
