*>--- conformance-case
*> id: dyn_capacity_bounds
*> rules: [GR-13.18.38.4-17, SR-14.9.39.3-30]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> OCCURS DYNAMIC — the TO clause is the EXPECTED capacity, NOT a hard cap (increment 2, D9; ISO 8.5.1.9.1:
      *> "The TO phrase of the OCCURS clause may be used to specify an upper value for the current capacity of a
      *> dynamic table, which may be exceeded with a nonfatal exception"). With runtime checking OFF (the default),
      *> SET past TO continues and the current capacity becomes the requested value. (EC-BOUND-OVERFLOW is the
      *> checking-ON observation of "current exceeds expected" — a later increment.)
      *>
      *> ⛔ THE AMOUNT IS arithmetic-expression-4, NOT integer-1, AND THAT IS THE WHOLE POINT (kb/Work PB458).
      *> 14.9.39.2 Format 14 writes the amount as a choice, { integer-1 | arithmetic-expression-4 }, and 5.5 rule 1
      *> makes integer-n "a fixed-point integer literal". 14.9.39.3 SR30 is a SYNTAX rule over the literal arm
      *> alone — "Integer-1 shall be nonnegative and, if TO is specified, integer-1 shall be not less than the
      *> minimum capacity defined in the corresponding OCCURS clause and not greater than the expected capacity,
      *> if specified" — so `SET WS-CAP TO 9` over `TO 4` is a program the standard requires the compiler to
      *> REJECT, and this golden used to pin it as accepted (with a literal) while asserting a run-time rule.
      *> Written through a data item the statement is conforming and the run-time behaviour 8.5.1.9.1 describes is
      *> what is actually measured. The literal arm's refusal is pinned by
      *> conformance:negative/pb458-set-capacity-literal-below-minimum.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DYN-CAP-BOUNDS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ED       PIC ZZZ9.
       01 WS-REQ   PIC 9(2) VALUE 9.
       01 WS-TABLE.
          05 WS-E PIC 9(3) OCCURS DYNAMIC CAPACITY IN WS-CAP FROM 2 TO 4.
       PROCEDURE DIVISION.
       MAIN-PARA.
           SET WS-CAP TO WS-REQ.
           MOVE WS-CAP TO ED.
           DISPLAY "OVER=[" ED "]".
           STOP RUN.
