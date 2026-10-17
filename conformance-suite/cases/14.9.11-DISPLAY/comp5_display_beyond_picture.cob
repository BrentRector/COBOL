*>--- conformance-case
*> id: comp5_display_beyond_picture
*> rules: [GR-14.9.11.4-1, DOC-A.1-56]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work R13 - OWNER DECISION 2026-08-08: follow GnuCOBOL. DISPLAY of a BinaryCapacity item
      *> holding a beyond-PICTURE value renders the PICTURE-DIGIT IMAGE (the value mod 10^digits),
      *> not IBM's full-container rendering (the vendors are split; 14.9.11.4 GR1 makes the
      *> conversion implementor-defined - CONFORMANCE.md section 7 item 56). The full value remains
      *> reachable through the spec-fixed MOVE path (14.9.25.4 GR6a - the sending size is the digit
      *> count... of the RECEIVER'S width here: PIC 9(20) holds all twenty digits). The R10 golden
      *> deliberately left this unpinned while the question was open; this pins the decision.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R13PIN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC 9(18) COMP-5.
       01 BIG PIC 9(20).
       PROCEDURE DIVISION.
           MOVE 18446744073709551615 TO W
           DISPLAY W
           MOVE W TO BIG
           DISPLAY BIG.
           STOP RUN.
