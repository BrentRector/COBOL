*>--- conformance-case
*> id: pb165_returning_wide_carrier
*> rules: [GR-14.9.4.4-4]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB165 closing GR-14.9.4.4-4 - ISO 14.9.4.4 GR4: "If a RETURNING phrase is specified, the
      *> result of the activated program is placed into identifier-3." The delivery has to be TOTAL over the
      *> carriers this compiler emits. Measured before PB165: the character-shaped delivery leg stored only
      *> into a string carrier or, via long.TryParse, a long one - so a result reaching a PIC 9(30)
      *> identifier-3 (an Int128/UInt128 carrier, i.e. EVERY receiver past 18 digits) was DISCARDED with no
      *> store and no diagnostic; R kept its previous value 7. Here the pair is a DYNAMIC (Format 1) CALL,
      *> where no bind-time 14.8.3 screen exists, and the callee's returning item is category alphanumeric
      *> while identifier-3 is category numeric - a pair 14.8.3.3 declares non-conforming, which 14.9.4.4 GR3d
      *> makes EC-PROGRAM-ARG-MISMATCH when checking is enabled in both elements. It is NOT enabled here, so
      *> the call proceeds; what GR4 forbids is the result not arriving at all. The digit image "000123" is
      *> what the receiver would have seen had the pair conformed by length, and that is what it receives.
      *> W pins the same GR4 sentence on the CONFORMING wide pair, so the golden fails if the total delivery
      *> were "fixed" by breaking the identical-description case. P pins GR4 over a USAGE POINTER returning
      *> item - the carrier whose missing StoreReturn leg was a raw Roslyn CS1503 on conforming source before
      *> kb/Work PB133 wave B, and which the totality rule now keeps loud rather than lost if it regresses.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB165R.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC 9(30) VALUE 7.
       01 W PIC 9(30) VALUE 0.
       01 P USAGE POINTER.
       01 T PIC X(4) VALUE "TGT".
       PROCEDURE DIVISION.
       MAIN.
           CALL "PB165RC" RETURNING R
           DISPLAY "R=" R
           CALL "PB165RW" RETURNING W
           DISPLAY "W=" W
           SET P TO ADDRESS OF T
           CALL "PB165RP" RETURNING P
           IF P = NULL
             DISPLAY "P=NULL"
           ELSE
             DISPLAY "P=NOT-NULL"
           END-IF
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB165RC.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LR PIC X(6).
       PROCEDURE DIVISION RETURNING LR.
       M1.
           MOVE "000123" TO LR
           GOBACK.
       END PROGRAM PB165RC.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB165RW.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LW PIC 9(30).
       PROCEDURE DIVISION RETURNING LW.
       M2.
           MOVE 123456789012345678901234567890 TO LW
           GOBACK.
       END PROGRAM PB165RW.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB165RP.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LP USAGE POINTER.
       PROCEDURE DIVISION RETURNING LP.
       M3.
           SET LP TO NULL
           GOBACK.
       END PROGRAM PB165RP.
       END PROGRAM PB165R.
