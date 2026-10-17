*>--- conformance-case
*> id: pb1025-sort-key-after-dynamic-beyond-minimum
*> rules: [SR-14.9.24.3-4, SR-14.9.40.3-6]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1025 - ISO 14.9.40.3 SR6 g): in a file of
      *> variable-length records every key data item "shall be contained
      *> within the first x bytes of the record", x the minimum record
      *> size. SK follows NM, a dynamic-length item of LIMIT 10, so in a
      *> record where NM is full SK reaches byte 12 - past the minimum of
      *> 2 (13.18.43.4 GR9: NM at zero length). With trailing fixed
      *> material the same key is legal (2014/pb1025_key_after_dynamic_
      *> member); without it the rule rejects the key.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1025NEG2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "pb1025neg2.srt".
       DATA DIVISION.
       FILE SECTION.
       SD SF.
       01 SR.
          05 SN PIC X DYNAMIC LENGTH LIMIT 10.
          05 SK PIC 9(2).
       PROCEDURE DIVISION.
       P0.
           SORT SF ASCENDING SK
               INPUT PROCEDURE IS P-IN
               OUTPUT PROCEDURE IS P-OUT.
           STOP RUN.
       P-IN.
           MOVE "A" TO SN. MOVE 10 TO SK. RELEASE SR.
       P-OUT.
           DISPLAY "UNREACHED".
