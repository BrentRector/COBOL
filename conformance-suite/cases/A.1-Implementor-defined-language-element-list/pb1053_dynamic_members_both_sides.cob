*>--- conformance-case
*> id: pb1053_dynamic_members_both_sides
*> rules: [DOC-A.1-63]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1053 - a record whose dynamic-length members sit on
      *> BOTH sides of a fixed member (and side by side) round-trips
      *> through every organization that frames its records, and through
      *> the sort. The record is sent as its contiguous image (ISO
      *> 8.5.1.11.2), so "AA" "KEY" "CCCC" and "AAKEY" "CCC" "C" are the
      *> same characters; the frame carries each member's length beside
      *> the record (determination D-FRA (v), docs/CONFORMANCE.md section
      *> 3), and a READ / RETURN into the same description gives every
      *> member back the content it was written with (8.5.1.10.4: "the
      *> new value becomes the content of the item"). The keys below
      *> follow a dynamic member and precede another; the trailing FILLER
      *> keeps each key's furthest reach within the minimum record size
      *> (14.9.40.3 SR6 g; 12.4.5.12.3 SR4), and each key is found at the
      *> position the record's own member lengths give it (14.9.30.4 GR32;
      *> 14.9.40.4 GR8).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1053-BOTH-SIDES.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT QF ASSIGN TO "pb1053.seq"
               ORGANIZATION IS SEQUENTIAL.
           SELECT VF ASSIGN TO "pb1053.rel"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS VK.
           SELECT XF ASSIGN TO "pb1053.idx"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS XK.
           SELECT SF ASSIGN TO "pb1053.srt".
           SELECT UF ASSIGN TO "pb1053.use"
               ORGANIZATION IS SEQUENTIAL.
           SELECT GF ASSIGN TO "pb1053.giv"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD QF.
       01 QR.
          05 QA PIC X DYNAMIC LENGTH LIMIT 5.
          05 QK PIC X(3).
          05 QC PIC X DYNAMIC LENGTH LIMIT 5.
          05 QD PIC X DYNAMIC LENGTH LIMIT 5.
          05 FILLER PIC X(2).
       FD VF.
       01 VR.
          05 VA PIC X DYNAMIC LENGTH LIMIT 5.
          05 VKY PIC X(3).
          05 VC PIC X DYNAMIC LENGTH LIMIT 5.
       FD XF.
       01 XR.
          05 XA PIC X DYNAMIC LENGTH LIMIT 5.
          05 XK PIC X(2).
          05 XC PIC X DYNAMIC LENGTH LIMIT 5.
          05 FILLER PIC X(8).
       SD SF.
       01 SR.
          05 SA PIC X DYNAMIC LENGTH LIMIT 5.
          05 SK PIC 9(2).
          05 SC PIC X DYNAMIC LENGTH LIMIT 5.
          05 FILLER PIC X(10).
       FD UF.
       01 UR.
          05 UA PIC X DYNAMIC LENGTH LIMIT 5.
          05 UK PIC 9(2).
          05 UC PIC X DYNAMIC LENGTH LIMIT 5.
          05 FILLER PIC X(10).
       FD GF.
       01 GR.
          05 GA PIC X DYNAMIC LENGTH LIMIT 5.
          05 GK PIC 9(2).
          05 GC PIC X DYNAMIC LENGTH LIMIT 5.
          05 FILLER PIC X(10).
       WORKING-STORAGE SECTION.
       01 VK PIC 9(4).
       01 EOF-F PIC X VALUE "N".
       PROCEDURE DIVISION.
       P0.
      *> Record sequential: members on both sides of QK, and QC / QD
      *> side by side.
           OPEN OUTPUT QF.
           MOVE "AA" TO QA. MOVE "KEY" TO QK. MOVE "CCCC" TO QC.
           MOVE "D" TO QD.
           WRITE QR.
           MOVE "AAAAA" TO QA. MOVE "K2Y" TO QK. MOVE "C" TO QC.
           MOVE "DDD" TO QD.
           WRITE QR.
           CLOSE QF.
           OPEN I-O QF.
           READ QF.
           DISPLAY "SEQ1 [" QA "] [" QK "] [" QC "] [" QD "]".
      *> The same record length (14.9.35.4 GR16), another split.
           MOVE "B" TO QA. MOVE "CCCCC" TO QC. MOVE "D" TO QD.
           REWRITE QR.
           READ QF.
           DISPLAY "SEQ2 [" QA "] [" QK "] [" QC "] [" QD "]".
           CLOSE QF.
           OPEN INPUT QF.
           READ QF.
           DISPLAY "SEQ3 [" QA "] [" QK "] [" QC "] [" QD "]".
           CLOSE QF.
      *> Relative.
           OPEN OUTPUT VF.
           MOVE "A" TO VA. MOVE "R01" TO VKY. MOVE "CCCCC" TO VC.
           MOVE 1 TO VK. WRITE VR.
           MOVE "AAAA" TO VA. MOVE "R02" TO VKY. MOVE "CC" TO VC.
           MOVE 2 TO VK. WRITE VR.
           CLOSE VF.
           OPEN INPUT VF.
           MOVE 2 TO VK. READ VF.
           DISPLAY "REL2 [" VA "] [" VKY "] [" VC "]".
           MOVE 1 TO VK. READ VF.
           DISPLAY "REL1 [" VA "] [" VKY "] [" VC "]".
           CLOSE VF.
      *> Indexed: XK follows XA and precedes XC.
           OPEN OUTPUT XF.
           MOVE "A" TO XA. MOVE "20" TO XK. MOVE "CCCCC" TO XC.
           WRITE XR.
           MOVE "AAAAA" TO XA. MOVE "10" TO XK. MOVE "C" TO XC.
           WRITE XR.
           MOVE "AAA" TO XA. MOVE "30" TO XK. MOVE "Q" TO XC.
           WRITE XR.
           CLOSE XF.
           OPEN I-O XF.
           MOVE "N" TO EOF-F.
           PERFORM UNTIL EOF-F = "Y"
               READ XF NEXT AT END MOVE "Y" TO EOF-F
               NOT AT END DISPLAY "IX " XK " [" XA "] [" XC "]"
               END-READ
           END-PERFORM.
           MOVE "ZZZ" TO XA. MOVE "20" TO XK. MOVE "Y" TO XC.
           READ XF KEY IS XK INVALID KEY DISPLAY "NO 20"
               NOT INVALID KEY DISPLAY "KEY " XK " [" XA "] [" XC "]"
           END-READ.
           MOVE "QQ" TO XA. MOVE "QQQ" TO XC.
           REWRITE XR INVALID KEY DISPLAY "NO REWRITE 20".
           MOVE "Z" TO XA. MOVE "20" TO XK. MOVE "ZZZZZ" TO XC.
           READ XF KEY IS XK INVALID KEY DISPLAY "NO 20"
               NOT INVALID KEY DISPLAY "RW  " XK " [" XA "] [" XC "]"
           END-READ.
           MOVE "WWWW" TO XA. MOVE "10" TO XK. MOVE "W" TO XC.
           DELETE XF INVALID KEY DISPLAY "NO DELETE 10".
           MOVE "V" TO XA. MOVE "00" TO XK. MOVE "VVVVV" TO XC.
           START XF KEY IS > XK INVALID KEY DISPLAY "NO START".
           MOVE "N" TO EOF-F.
           PERFORM UNTIL EOF-F = "Y"
               READ XF NEXT AT END MOVE "Y" TO EOF-F
               NOT AT END DISPLAY "IX2 " XK " [" XA "] [" XC "]"
               END-READ
           END-PERFORM.
           CLOSE XF.
      *> The sort: RELEASE / RETURN, then USING / GIVING.
           SORT SF ASCENDING SK
               INPUT PROCEDURE IS P-IN
               OUTPUT PROCEDURE IS P-OUT.
           OPEN OUTPUT UF.
           MOVE "U" TO UA. MOVE 60 TO UK. MOVE "UUUUU" TO UC.
           WRITE UR.
           MOVE "UUUUU" TO UA. MOVE 50 TO UK. MOVE "U" TO UC.
           WRITE UR.
           CLOSE UF.
           SORT SF ASCENDING SK USING UF GIVING GF.
           OPEN INPUT GF.
           MOVE "N" TO EOF-F.
           PERFORM UNTIL EOF-F = "Y"
               READ GF AT END MOVE "Y" TO EOF-F
               NOT AT END DISPLAY "GIV " GK " [" GA "] [" GC "]"
               END-READ
           END-PERFORM.
           CLOSE GF.
           STOP RUN.
       P-IN.
           MOVE "S" TO SA. MOVE 40 TO SK. MOVE "SSSSS" TO SC.
           RELEASE SR.
           MOVE "SSSSS" TO SA. MOVE 30 TO SK. MOVE "S" TO SC.
           RELEASE SR.
       P-OUT.
           MOVE "N" TO EOF-F.
           PERFORM UNTIL EOF-F = "Y"
               RETURN SF AT END MOVE "Y" TO EOF-F
               NOT AT END DISPLAY "SRT " SK " [" SA "] [" SC "]"
               END-RETURN
           END-PERFORM.
