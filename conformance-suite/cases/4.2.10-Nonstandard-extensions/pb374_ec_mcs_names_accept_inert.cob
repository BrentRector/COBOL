*>--- conformance-case
*> id: pb374_ec_mcs_names_accept_inert
*> rules: [GR-4.2.10-2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB374 - the asynchronous messaging facility's exception-
      *> names are ACCEPTED INERT. Annex A.3 item 4 makes the facility
      *> processor-dependent, so ISO 4.2.6 applies: "An implementation
      *> shall provide a warning mechanism at compile time to indicate
      *> use of syntactically-detectable processor-dependent language
      *> elements not supported by that implementation" - each EC-MCS
      *> name below draws the COBOLNET1578 warning and the program runs.
      *> No SEND or RECEIVE sets an EC-MCS condition here (14.6.13.1.1
      *> licenses raising none), but a RAISE raises it like any level-3
      *> name: 14.9.29.4 GR1 "the associated exception condition is
      *> raised", with checking enabled by the TURN directive for the
      *> declarative and by the WHEN phrase for the PERFORM. Both are
      *> NONFATAL (Table 13), so control continues after the RAISE.
       >>TURN EC-MCS-INVALID-TAG CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB374EC.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE AFTER EXCEPTION CONDITION EC-MCS-INVALID-TAG.
       D1-P.
           DISPLAY "DECLARATIVE "
               FUNCTION TRIM (FUNCTION EXCEPTION-STATUS).
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           RAISE EXCEPTION EC-MCS-INVALID-TAG.
           DISPLAY "AFTER RAISE".
           PERFORM
               RAISE EXCEPTION EC-MCS-MESSAGE-LENGTH
           WHEN EC-MCS-MESSAGE-LENGTH
               DISPLAY "WHEN "
                   FUNCTION TRIM (FUNCTION EXCEPTION-STATUS)
           END-PERFORM.
           DISPLAY "DONE".
           STOP RUN.
