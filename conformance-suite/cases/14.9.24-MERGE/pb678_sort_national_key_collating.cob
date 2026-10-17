*>--- conformance-case
*> id: pb678_sort_national_key_collating
*> rules: [GR-14.9.24.4-5, GR-14.9.40.4-5]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.40.4 GR5 (SORT) - "The alphanumeric collating sequence that applies to the
      *> comparison of key data items of class alphabetic and class alphanumeric, and the national
      *> collating sequence that applies to the comparison of key data items of class national, are
      *> each separately determined at the beginning of the execution of the SORT statement in the
      *> following order of precedence: a) First, the collating sequence established by the
      *> COLLATING SEQUENCE phrase ... The collating sequence associated with alphabet-name-1
      *> applies to key data items of class alphabetic and alphanumeric; the collating sequence
      *> associated with alphabet-name-2 applies to key data items of class national. b) Second,
      *> the collating sequences established as the program collating sequences."
      *>
      *> THIS FILE PINS GR5a - THE STATEMENT PHRASE - FOR A **FILE** SORT (Format 1). Before
      *> kb/Work PB678 the file sort carried ONE sequence and compared every key's RECORD-IMAGE
      *> BYTES under it, so a class-national key was doubly wrong:
      *>   1. its window was its CARRIER width (n) over an image whose basis is BYTES (2n for a
      *>      national position - 13.18.60.4 GR8, D-N1), so PIC N(1) keys all collapsed onto the
      *>      shared high byte U+0000 and compared EQUAL - a stable sort returning the RELEASE
      *>      order; and
      *>   2. what it did compare was weighed by the ALPHANUMERIC sequence.
      *>
      *> THE THREE ORDERS ARE DELIBERATELY ALL DIFFERENT so no leg can pass for the wrong reason:
      *> native is A < B < C; REV-AN ("CBA") is C < B < A; REV-NAT (N"BAC") is B < A < C. A leg
      *> answering with another leg's order is a visible failure.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB678SORTNAT.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET REV-AN IS "CBA"
           ALPHABET REV-NAT FOR NATIONAL IS N"BAC".
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "pb678sortnat.tmp".
           SELECT MF1 ASSIGN TO "pb678merge1.tmp".
           SELECT MF2 ASSIGN TO "pb678merge2.tmp".
       DATA DIVISION.
       FILE SECTION.
       SD SF.
       01 SR.
      *> SNK is wrapped in a GROUP-USAGE NATIONAL group so LEG 8 can name the GROUP as the key
      *> without moving a byte: 13.18.29.4 GR2b makes SNG "an elementary data item of usage
      *> national ... PICTURE N(m)" over the same two bytes SNK occupies, so every other leg is
      *> byte-for-byte the program it was.
          05 SNG GROUP-USAGE NATIONAL.
             10 SNK PIC N(1).
          05 SAK PIC X(1).
          05 SP  PIC X(3).
       FD MF1.
       01 MR1.
          05 M1NK PIC N(1).
          05 M1AK PIC X(1).
          05 M1P  PIC X(3).
       FD MF2.
       01 MR2.
          05 M2NK PIC N(1).
          05 M2AK PIC X(1).
          05 M2P  PIC X(3).
       WORKING-STORAGE SECTION.
       01 DONE-FLAG PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN.
      *> LEG 1 - no COLLATING phrase and no program collating sequence: GR5b leaves BOTH classes
      *> native. The national key orders by code point (D-N3): A < B < C.
           DISPLAY "L1 NATIVE-NAT-KEY".
           SORT SF ON ASCENDING KEY SNK
               INPUT PROCEDURE IS FEED
               OUTPUT PROCEDURE IS DRAIN.
      *> LEG 2 - alphabet-name-1 ONLY. GR5a gives REV-AN to keys of class alphabetic and
      *> alphanumeric; a key of class NATIONAL is not one of those, so it stays on the (native)
      *> national program collating sequence: A < B < C, UNCHANGED FROM LEG 1.
           DISPLAY "L2 ALNUM-ALPHABET-ON-NAT-KEY".
           SORT SF ON ASCENDING KEY SNK
               COLLATING SEQUENCE IS REV-AN
               INPUT PROCEDURE IS FEED
               OUTPUT PROCEDURE IS DRAIN.
      *> LEG 3 - THE CONTROL. The SAME alphabet-name-1 over an ALPHANUMERIC key: it DOES apply,
      *> C < B < A. Without this leg, LEG 2 would pass under a compiler that had simply stopped
      *> collating altogether.
           DISPLAY "L3 ALNUM-ALPHABET-ON-ALNUM-KEY".
           SORT SF ON ASCENDING KEY SAK
               COLLATING SEQUENCE IS REV-AN
               INPUT PROCEDURE IS FEED
               OUTPUT PROCEDURE IS DRAIN.
      *> LEG 4 - alphabet-name-2 through the FOR NATIONAL form: REV-NAT applies to the national
      *> key, B < A < C.
           DISPLAY "L4 NAT-ALPHABET-ON-NAT-KEY".
           SORT SF ON ASCENDING KEY SNK
               COLLATING SEQUENCE FOR NATIONAL IS REV-NAT
               INPUT PROCEDURE IS FEED
               OUTPUT PROCEDURE IS DRAIN.
      *> LEG 5 - BOTH sequences in ONE statement, each on its own class, with a duplicate national
      *> key so the minor alphanumeric key decides. National under REV-NAT: B(BA2) < A(AC1, AA4)
      *> < C(CB3); the two A-keyed records then order on SAK under REV-AN: "C"(AC1) < "A"(AA4).
      *> No two records are equal on BOTH keys, so GR4's undefined-order case never arises and the
      *> DUPLICATES phrase is not needed to make the expected order derivable.
           DISPLAY "L5 BOTH-SEQUENCES-ONE-STATEMENT".
           SORT SF ON ASCENDING KEY SNK ASCENDING KEY SAK
               COLLATING SEQUENCE FOR ALPHANUMERIC IS REV-AN
                                  FOR NATIONAL IS REV-NAT
               INPUT PROCEDURE IS FEED4
               OUTPUT PROCEDURE IS DRAIN.
      *> LEG 6 - the same two keys with NO phrase: both classes native. National A(AC1, AA4) <
      *> B(BA2) < C(CB3); the two A-keyed records order on SAK natively: "A"(AA4) < "C"(AC1).
           DISPLAY "L6 BOTH-NATIVE-TWO-KEYS".
           SORT SF ON ASCENDING KEY SNK ASCENDING KEY SAK
               INPUT PROCEDURE IS FEED4
               OUTPUT PROCEDURE IS DRAIN.
      *> LEG 7 - the SAME two bytes named as a national GROUP. 13.18.29.4 GR2b makes SNG an
      *> elementary national operand of PICTURE N(1), so it takes the NATIONAL sequence and its
      *> key window is the group's BYTE extent (2) decoded back to 1 national position - and it
      *> must give LEG 4's answer exactly. THE AGREEMENT IS THE REQUIREMENT: read as an ordinary
      *> group it would have been 2 UTF-16BE bytes under the ALPHANUMERIC sequence, which under
      *> REV-AN answers CB3 BA2 AC1 - the reverse.
           DISPLAY "L7 NAT-GROUP-KEY".
           SORT SF ON ASCENDING KEY SNG
               COLLATING SEQUENCE FOR ALPHANUMERIC IS REV-AN
                                  FOR NATIONAL IS REV-NAT
               INPUT PROCEDURE IS FEED
               OUTPUT PROCEDURE IS DRAIN.
      *> LEG 8 - MERGE. 14.9.24.4 GR5 is SORT GR5 word for word ("The alphanumeric collating
      *> sequence that applies to the comparison of key data items of class alphabetic and class
      *> alphanumeric, and the national collating sequence that applies to the comparison of key
      *> data items of class national, are separately determined ..."), so MERGE needs its OWN
      *> evidence rather than SORT's: the two verbs share one binder and one runtime comparator,
      *> and that is exactly the claim a MERGE-free corpus could not check. Each USING file is
      *> pre-sorted IN THE SEQUENCE THE STATEMENT NAMES (GR6 - unordered input is undefined):
      *> under REV-NAT (N"BAC") B < A < C, so MF1 holds B then C and MF2 holds A alone. The merged
      *> order is B(MB1), A(MA2), C(MC1) - and under the ALPHANUMERIC weights that used to be
      *> applied to it, C would have led.
           DISPLAY "L8 MERGE-NAT-ALPHABET".
           PERFORM BUILD-MERGE-INPUTS.
           MERGE SF ON ASCENDING KEY SNK
               COLLATING SEQUENCE FOR NATIONAL IS REV-NAT
               USING MF1 MF2
               OUTPUT PROCEDURE IS DRAIN.
           STOP RUN.
       BUILD-MERGE-INPUTS.
           OPEN OUTPUT MF1.
           MOVE N"B" TO M1NK. MOVE "x" TO M1AK. MOVE "MB1" TO M1P.
           WRITE MR1.
           MOVE N"C" TO M1NK. MOVE "x" TO M1AK. MOVE "MC1" TO M1P.
           WRITE MR1.
           CLOSE MF1.
           OPEN OUTPUT MF2.
           MOVE N"A" TO M2NK. MOVE "x" TO M2AK. MOVE "MA2" TO M2P.
           WRITE MR2.
           CLOSE MF2.
       FEED.
           MOVE N"A" TO SNK. MOVE "C" TO SAK. MOVE "AC1" TO SP.
           RELEASE SR.
           MOVE N"B" TO SNK. MOVE "A" TO SAK. MOVE "BA2" TO SP.
           RELEASE SR.
           MOVE N"C" TO SNK. MOVE "B" TO SAK. MOVE "CB3" TO SP.
           RELEASE SR.
       FEED4.
           MOVE N"A" TO SNK. MOVE "C" TO SAK. MOVE "AC1" TO SP.
           RELEASE SR.
           MOVE N"B" TO SNK. MOVE "A" TO SAK. MOVE "BA2" TO SP.
           RELEASE SR.
           MOVE N"C" TO SNK. MOVE "B" TO SAK. MOVE "CB3" TO SP.
           RELEASE SR.
           MOVE N"A" TO SNK. MOVE "A" TO SAK. MOVE "AA4" TO SP.
           RELEASE SR.
       DRAIN.
           MOVE "N" TO DONE-FLAG.
           PERFORM UNTIL DONE-FLAG = "Y"
               RETURN SF RECORD
                   AT END MOVE "Y" TO DONE-FLAG
                   NOT AT END DISPLAY "  " SP
               END-RETURN
           END-PERFORM.
