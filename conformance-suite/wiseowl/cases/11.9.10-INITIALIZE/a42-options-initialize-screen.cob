*>--- conformance-case
*> id: a42-options-initialize-screen
*> rules: [GR-11.9.10.4-3]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 11.9.10.4 GR3 - "If SCREEN is specified, all data items in the screen section are initialized
      *> as indicated in the rules for initial state." The OPTIONS paragraph's INITIALIZE clause names the
      *> screen section, which Annex A.4.2 makes optional and docs/CONFORMANCE.md section 5 records as
      *> Not claimed; A.4.1 admits an optional element's syntax only when support is claimed.
      *> ONLY the explicit SCREEN leg is refused: GR1 makes ALL imply LOCAL-STORAGE, SCREEN and
      *> WORKING-STORAGE, two of which are supported, so INITIALIZE ALL SECTION TO SPACES stays legal
      *> (pinned by ScreenFacilityConstructDriftTests.OptionsInitializeAll_StaysLegal).
      *> The module's SEVENTH source shape - it parsed, set OptionsSections.Screen, and nothing read it.
      *> Witness for kb/Work PB260.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. A42OIS.
       OPTIONS.
           INITIALIZE SCREEN SECTION TO SPACES.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X PIC X VALUE "A".
       PROCEDURE DIVISION.
           STOP RUN.
