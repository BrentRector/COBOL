*>--- conformance-case
*> id: pb570-group-no-sign-below-2023
*> rules: [GR-13.18.60.4-11]
*> source-format: free
*> portable: unverified
*>---
      *> ISO Annex E.3.2 item 5 - "The NO SIGN phrase of the USAGE clause. The USAGE clause has been enhanced to
      *> allow the representation of a PACKED-DECIMAL data item in the storage of the computer to contain no sign
      *> value." A COBOL-2023 introduction, so the phrase is not COBOL-85 / 2002 / 2014 source in EITHER of its
      *> two legal spellings. The elementary spelling has been gated since the phrase landed; this fixture pins
      *> the GROUP spelling, whose SR31 screen and whose GR11 width were both lost before kb/Work PB570 - an
      *> edition gate that fires on only one of a construct's spellings is the edition-gate sweep's own failure
      *> mode (feedback_edition_gate_sweep).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB570NEGEDITION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  GS USAGE PACKED-DECIMAL WITH NO SIGN.
           05  W1  PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY W1
           STOP RUN.
