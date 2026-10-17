*>--- conformance-case
*> id: w59_mcs_facility_witness
*> rules: [GR-14.6.11-7, SR-14.9.31.3-1, SR-14.9.31.3-2, SR-14.9.31.3-3, GR-14.9.31.4-1, GR-14.9.31.4-2, GR-14.9.31.4-3, GR-14.9.31.4-4, GR-14.9.31.4-5, SR-14.9.38.3-1, SR-14.9.38.3-2, SR-14.9.38.3-3, SR-14.9.38.3-4, GR-14.9.38.4-1, GR-14.9.38.4-2, GR-14.9.38.4-3, GR-14.9.38.4-4, GR-14.9.38.4-5, GR-14.9.38.4-6, GR-14.9.38.4-7, GR-14.9.38.4-8, FMT-14.9.31.2, FMT-14.9.38.2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work R43 / PB1198 — the asynchronous messaging (MCS) facility
      *> is DECLINED. Annex A.3 item 4: "The asynchronous messaging
      *> facility is dependent on the capability of a processor to allow
      *> run units to communicate with each other." §4.2.6 lets an
      *> implementor decline it ("Language elements that pertain to
      *> specific processor-dependent elements for which support is not
      *> claimed need not be implemented") and makes the compile-time
      *> WARNING mandatory: "An implementation shall provide a warning
      *> mechanism at compile time to indicate use of syntactically-
      *> detectable processor-dependent language elements not supported
      *> by that implementation." That warning, COBOLNET1578, once at
      *> the RECEIVE and once at the SEND, is the DECLINE half, pinned by
      *> conformance-test DocumentedNonSupportWitnessTests.
      *> THE OUTPUT IS THE INERT HALF. Declined, neither statement
      *> performs message I-O, so no message is ever received or sent:
      *> neither the ON EXCEPTION nor the NOT ON EXCEPTION imperative
      *> runs, the receiving area keeps its VALUE, and control reaches
      *> the next statement. A conforming MCS would print OK or EXC at
      *> each statement (§14.9.31.4 GR4/GR5, §14.9.38.4 GR2/GR3), so this
      *> .out is the posture docs/CONFORMANCE.md §4 item 1 documents, and
      *> it discriminates a silently half-implemented facility from the
      *> declined one.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W59MCS01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-TAG USAGE POINTER.
       01 WS-REPLY USAGE POINTER.
       01 WS-LEN PIC 9(4) VALUE 7.
       01 WS-BUF PIC X(10) VALUE "UNCHANGED".
       01 WS-MSG PIC X(10) VALUE "HELLO".
       PROCEDURE DIVISION.
           RECEIVE FROM WS-TAG GIVING WS-BUF WS-LEN
               ON EXCEPTION DISPLAY "RECEIVE EXC"
               NOT ON EXCEPTION DISPLAY "RECEIVE OK"
           END-RECEIVE.
           DISPLAY "AFTER RECEIVE BUF=" WS-BUF " LEN=" WS-LEN.
           SEND TO "W59SERVER" FROM WS-MSG RETURNING WS-REPLY
               ON EXCEPTION DISPLAY "SEND EXC"
               NOT ON EXCEPTION DISPLAY "SEND OK"
           END-SEND.
           DISPLAY "AFTER SEND".
           STOP RUN.
