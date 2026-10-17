*>--- conformance-case
*> id: pb1025_key_after_dynamic_member
*> rules: [SR-12.4.5.6.3-5, SR-12.4.5.12.3-4]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1025 - a SORT key and an indexed RECORD KEY / ALTERNATE
      *> RECORD KEY that FOLLOW a dynamic-length item. The record is held
      *> as its contiguous image (ISO 8.5.1.11.2), so each key's byte
      *> positions move with the current length of NM; 14.9.40.3 SR6 g)
      *> and 12.4.5.12.3 SR4 / 12.4.5.6.3 SR5 admit the key because the
      *> trailing FILLER keeps its furthest reach (NM at its LIMIT 10 plus
      *> the key) within the minimum record size (13.18.43.4 GR9: NM at
      *> zero length, so 22 / 23 bytes). The NM lengths are chosen so a
      *> key sliced at its FIXED-run offset would read NM's characters
      *> and order the records 30 / 20 / 10 - the expected order below
      *> is the key's own (14.9.40.4 GR8; 14.9.30.4 GR32; 14.9.41.4).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1025-KEY-AFTER-DYN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "pb1025.srt".
           SELECT XF ASSIGN TO "pb1025.idx"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS XK
               ALTERNATE RECORD KEY IS XA WITH DUPLICATES.
       DATA DIVISION.
       FILE SECTION.
       SD SF.
       01 SR.
          05 SN PIC X DYNAMIC LENGTH LIMIT 10.
          05 SK PIC 9(2).
          05 FILLER PIC X(20).
       FD XF.
       01 XR.
          05 XN PIC X DYNAMIC LENGTH LIMIT 10.
          05 XK PIC X(2).
          05 XA PIC X(1).
          05 XF-REST PIC X(20).
       WORKING-STORAGE SECTION.
       01 EOF-F PIC X VALUE "N".
       PROCEDURE DIVISION.
       P0.
           SORT SF ASCENDING SK
               INPUT PROCEDURE IS P-IN
               OUTPUT PROCEDURE IS P-OUT.
           OPEN OUTPUT XF.
           MOVE "CCCCC" TO XN. MOVE "20" TO XK. MOVE "Q" TO XA.
           WRITE XR.
           MOVE "A" TO XN. MOVE "30" TO XK. MOVE "P" TO XA.
           WRITE XR.
           MOVE "BBB" TO XN. MOVE "10" TO XK. MOVE "R" TO XA.
           WRITE XR.
           CLOSE XF.
           OPEN INPUT XF.
           MOVE "N" TO EOF-F.
           PERFORM UNTIL EOF-F = "Y"
               READ XF NEXT AT END MOVE "Y" TO EOF-F
               NOT AT END DISPLAY "IX " XK " " XA " [" XN "]"
               END-READ
           END-PERFORM.
           MOVE "ZZZZZZZZ" TO XN.
           MOVE "20" TO XK.
           READ XF KEY IS XK INVALID KEY DISPLAY "NO 20"
               NOT INVALID KEY DISPLAY "KEY " XK " [" XN "]"
           END-READ.
           MOVE "R" TO XA.
           READ XF KEY IS XA INVALID KEY DISPLAY "NO R"
               NOT INVALID KEY DISPLAY "ALT " XK " " XA " [" XN "]"
           END-READ.
           MOVE "P" TO XA.
           START XF KEY IS = XA INVALID KEY DISPLAY "NO P"
           END-START.
           MOVE "N" TO EOF-F.
           PERFORM UNTIL EOF-F = "Y"
               READ XF NEXT AT END MOVE "Y" TO EOF-F
               NOT AT END DISPLAY "BY-ALT " XA " " XK " [" XN "]"
               END-READ
           END-PERFORM.
           CLOSE XF.
           STOP RUN.
       P-IN.
           MOVE "CCCCC" TO SN. MOVE 20 TO SK. RELEASE SR.
           MOVE "A" TO SN. MOVE 30 TO SK. RELEASE SR.
           MOVE "BBB" TO SN. MOVE 10 TO SK. RELEASE SR.
       P-OUT.
           PERFORM UNTIL EOF-F = "Y"
               RETURN SF AT END MOVE "Y" TO EOF-F
               NOT AT END DISPLAY "SORT " SK " [" SN "]"
               END-RETURN
           END-PERFORM.
