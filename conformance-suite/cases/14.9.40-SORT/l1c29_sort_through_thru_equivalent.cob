      *>--- conformance-case
      *> id: l1c29_sort_through_thru_equivalent
      *> rules: [SR-14.9.40.3-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.40.3 SR7 — THROUGH and THRU name the same SORT procedure range
      *> Rule (§14.9.40.3 SR7): "The words THROUGH and THRU are equivalent."
      *> cite.py --check 14.9.40.3 "The words THROUGH and THRU are equivalent"
      *>   -> OK  §14.9.40.3 7)  (Syntax rules)
      *> Format 1 (§14.9.40.2) prints { THROUGH | THRU } in BOTH the INPUT
      *> PROCEDURE and the OUTPUT PROCEDURE phrase. Two SORTs over the same
      *> three-paragraph ranges: SORT 1 writes INPUT ... THROUGH and OUTPUT
      *> ... THRU; SORT 2 swaps the spellings. Each range has a MIDDLE
      *> paragraph, so a spelling that bound only the first or only the
      *> named paragraphs would drop records or the MID line.
      *> Derivation:
      *>  Input range IP-A..IP-B releases C/IPA1, D/IPM1, A/IPB1, B/IPB2
      *>  (all three paragraphs execute under either spelling, SR7).
      *>  SORT 1 ASCENDING on SK -> A B C D; OP-A returns the first
      *>  record ("S1 FIRST AIPB1"), OP-M prints "S1 MID", OP-B returns
      *>  the rest in order. SORT 2 DESCENDING -> D C B A, same shape
      *>  tagged S2. Keys are all distinct, so GR4 (undefined order of
      *>  equal keys) never applies.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C29A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "L1C29A.TMP".
       DATA DIVISION.
       FILE SECTION.
       SD  SF.
       01  SR.
           05 SK PIC X(1).
           05 SP PIC X(4).
       WORKING-STORAGE SECTION.
       01  EOF-FLAG PIC X VALUE "N".
       01  TAG      PIC X(2).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "S1" TO TAG.
           SORT SF ON ASCENDING KEY SK
               INPUT PROCEDURE IS IP-A THROUGH IP-B
               OUTPUT PROCEDURE IS OP-A THRU OP-B.
           MOVE "S2" TO TAG.
           SORT SF ON DESCENDING KEY SK
               INPUT PROCEDURE IS IP-A THRU IP-B
               OUTPUT PROCEDURE IS OP-A THROUGH OP-B.
           DISPLAY "END".
           STOP RUN.
       IP-A.
           MOVE "C" TO SK. MOVE "IPA1" TO SP.
           RELEASE SR.
       IP-M.
           MOVE "D" TO SK. MOVE "IPM1" TO SP.
           RELEASE SR.
       IP-B.
           MOVE "A" TO SK. MOVE "IPB1" TO SP.
           RELEASE SR.
           MOVE "B" TO SK. MOVE "IPB2" TO SP.
           RELEASE SR.
       OP-A.
           MOVE "N" TO EOF-FLAG.
           RETURN SF
               AT END MOVE "Y" TO EOF-FLAG
               NOT AT END DISPLAY TAG " FIRST " SK SP
           END-RETURN.
       OP-M.
           DISPLAY TAG " MID".
       OP-B.
           PERFORM UNTIL EOF-FLAG = "Y"
               RETURN SF
                   AT END MOVE "Y" TO EOF-FLAG
                   NOT AT END DISPLAY TAG " NEXT " SK SP
               END-RETURN
           END-PERFORM.
