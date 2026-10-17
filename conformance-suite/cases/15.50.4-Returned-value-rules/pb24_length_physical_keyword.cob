*>--- conformance-case
*> id: pb24_length_physical_keyword
*> rules: [RV-15.50.4-8, FMT-15.50.2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PHYSICAL IS A KEYWORD OF FUNCTION LENGTH, NOT A SECOND ARGUMENT.
      *> 15.50.2's general format is  FUNCTION LENGTH ( argument-1 [ PHYSICAL ] ).
      *>
      *> ⛔ THIS GOLDEN EXISTS BECAUSE THE KEYWORD REJECTED LEGAL SOURCE (fix-queue PB24). The generic argument
      *> path counted PHYSICAL as an operand:
      *>     COBOLNET1504: FUNCTION LENGTH takes 1 argument(s); 2 given (ISO 15.3)
      *> It is now consumed exactly as ANYCASE is for the NUMVAL-C family, so no grammar change was needed - a
      *> bare word already parses as an argument context.
      *>
      *> ⚖ WHAT IT RETURNS IS AN IMPLEMENTOR DETERMINATION, and 15.50.4 r8's closing sentence is what makes it
      *> small: "If argument-1 is physically located where it is defined, LENGTH returns the same value that would
      *> be returned had the PHYSICAL argument not been specified." COBOL.NET determines that a variable-length
      *> group IS physically located where it is defined - a program has no addressable out-of-line pointer to
      *> observe - so PHYSICAL is semantically transparent. Recorded in docs/CONFORMANCE.md per 4.2.16.
      *> That is the point of the two PAIRS below: each PHYSICAL form must equal its plain form exactly.
      *>
      *> ⚠ AND A GENUINE ARITY ERROR MUST STILL FIRE - consuming a keyword must not become "accept anything".
      *> The negative case is tests/conformance/negative/pb24-length-two-operands.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB24PHYS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-FIX.
          05 F1 PIC X(4).
          05 F2 PIC X(6).
       01 WS-VAR.
          05 V1 PIC X(4).
          05 VD PIC X DYNAMIC LENGTH.
       01 WS-NAME PIC X(20).
       01 R PIC 9(4).
       PROCEDURE DIVISION.
           COMPUTE R = FUNCTION LENGTH(WS-FIX PHYSICAL)
           DISPLAY "FIXPHYS=" R
           COMPUTE R = FUNCTION LENGTH(WS-FIX)
           DISPLAY "FIXPLAIN=" R
           MOVE "XYZ" TO VD
           COMPUTE R = FUNCTION LENGTH(WS-VAR PHYSICAL)
           DISPLAY "VARPHYS=" R
           COMPUTE R = FUNCTION LENGTH(WS-VAR)
           DISPLAY "VARPLAIN=" R
      *> A reference-modified argument also takes the keyword (15.50.3 r1 admits the item; the keyword is
      *> orthogonal to which item argument-1 is).
           COMPUTE R = FUNCTION LENGTH(WS-NAME(1:5) PHYSICAL)
           DISPLAY "RMPHYS=" R
           STOP RUN.
