      *>--- conformance-case
      *> id: pb373_object_computer_name_inert
      *> rules: [DOC-A.1-126, DOC-A.1-127]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §12.3.6.4 GR2 — computer-name-1 identifies NO equipment
      *> configuration (Annex A.1 item 127, optional): DOC-A.1-127.
      *>
      *> GR2: "Computer-name-1 may provide a means for identifying
      *> equipment configuration, in which case computer-name-1 and
      *> its implied configuration are specified by each implementor."
      *> A.1's preamble makes the item OPTIONAL ("The element may be
      *> provided at the implementor's option") and conditions the
      *> documentation duty on provision ("If the element is provided
      *> by the implementor, the implementor's user documentation
      *> shall document the element"); §4.2.5 is the conformance
      *> clause that governs the A.1 list.
      *>
      *> CONFORMANCE.md §7 DOC-A.1-127 makes TWO claims and this file
      *> asserts BOTH:
      *>   (1) there is ONE object computer whether the name is
      *>       present (GR2), absent (GR3), or the paragraph is
      *>       omitted (GR4)                    — PART 1 below
      *>   (2) "the clauses that follow it (PROGRAM COLLATING
      *>       SEQUENCE; CHARACTER CLASSIFICATION) behave identically
      *>       with or without a name"          — PART 2 below
      *>
      *> ================= PART 1 — one object computer ============
      *> THE CLAIM IS AN INVARIANCE, NOT "IT COMPILES".  So this
      *> measures the same equipment-configuration facts in FOUR
      *> source shapes of one compilation group and compares them:
      *>   L1OCW01  OBJECT-COMPUTER. WISE-OWL-9000.  a name (GR2)
      *>   L1OCW02  OBJECT-COMPUTER. ACME-4381.      a DIFFERENT name
      *>   L1OCW03  OBJECT-COMPUTER.                 no name (GR3)
      *>   L1OCW04  no OBJECT-COMPUTER paragraph     (GR4)
      *> §12.3.6.3 SR4 lets L1OCW03's second period go.  A name-keyed
      *> equipment configuration — exactly what GR2 permits an
      *> implementor to provide — is what would make L1OCW02 answer
      *> differently from L1OCW01.
      *> The four are SEPARATE outermost source units, so GR1's
      *> containment rule ("...and to any source unit contained
      *> within that source unit") carries no configuration between
      *> them: each leg answers on its own paragraph alone.
      *>
      *> The signature holds the facts an implied equipment
      *> configuration fixes: FUNCTION BYTE-LENGTH of a group holding
      *> BINARY-LONG and BINARY-DOUBLE (§13.18.60.4 GR21 — "The
      *> representation and length ... is implementor-defined"),
      *> FUNCTION ORD of a native character, a runtime relation under
      *> the native alphanumeric program collating sequence
      *> (§12.3.6.4 GR10) and a runtime class condition under the
      *> character classification in effect (§12.3.6.4 GR6).  No
      *> implementor-defined VALUE is pinned — only that all four
      *> legs agree.
      *>
      *> ============ PART 2 — the clauses AFTER the name ==========
      *> ⛔ PART 1 ALONE ASSERTS ONLY HALF THE DETERMINATION.  Legs
      *> 01–04 all run under the DEFAULT arms — GR10 (no PROGRAM
      *> COLLATING SEQUENCE clause) and GR6 (no CHARACTER
      *> CLASSIFICATION clause) — which are precisely the branches
      *> that apply BECAUSE no clause is written, so they say nothing
      *> about claim (2).  Two more pairs do:
      *>   L1OCW05  OBJECT-COMPUTER. WISE-OWL-9000
      *>                PROGRAM COLLATING SEQUENCE IS REV.
      *>   L1OCW06  OBJECT-COMPUTER.
      *>                PROGRAM COLLATING SEQUENCE IS REV.
      *>   L1OCW07  OBJECT-COMPUTER. WISE-OWL-9000
      *>                CHARACTER CLASSIFICATION IS TR.
      *>   L1OCW08  OBJECT-COMPUTER.
      *>                CHARACTER CLASSIFICATION IS TR.
      *> This is the shape that has actually broken once: kb/Work
      *> PB78 — `OBJECT-COMPUTER. PROGRAM COLLATING SEQUENCE IS REV.`
      *> was "unexpected 'PROGRAM'" because the grammar hung the
      *> clause off a REQUIRED name.  A name-keyed configuration
      *> would bite here first.
      *>
      *> PCS legs — §12.3.6.4 GR9 ("When the PROGRAM COLLATING
      *> SEQUENCE clause is specified, the initial alphanumeric
      *> program collating sequence is the collating sequence
      *> associated with alphabet-name-1") and GR11 ("The
      *> alphanumeric program collating sequence and national program
      *> collating sequence are used to determine the truth value of
      *> any alphanumeric comparisons and national comparisons").
      *> ALPHABET REV IS "B" "A" puts "B" in position 1 and "A" in
      *> position 2, so `IF X > "B"` is TRUE for X = "A" under REV
      *> and FALSE under the native sequence — the same observable
      *> tests/conformance/2023/pb78_object_computer_optional_name
      *> already pins.  Both legs must answer REV; the ELSE branch
      *> prints NATIVE, and a CALL that never ran leaves the flag 0
      *> and also prints NATIVE, so the leg fails LOUDLY.
      *>
      *> CC legs — §12.3.6.4 GR5 a ("If locale-name-1 is specified,
      *> the initial alphanumeric character classification is the
      *> character classification associated with locale-name-1") and
      *> GR7 a ("the uppercase and lowercase mappings of characters
      *> for the UPPER-CASE and LOWER-CASE intrinsic functions").
      *> The two legs are compared to EACH OTHER, never to a pinned
      *> Turkish value: §8.2.1 makes locale availability a runtime
      *> property, so if "tr-TR" is absent both legs fall back
      *> identically and the invariance still holds.  That keeps the
      *> golden environment-independent while still measuring the
      *> clause rather than GR6's default.  CC-NONZERO guards the
      *> vacuous case in which neither CALL ran.
      *>
      *> CONTROL LEG: the driver also compares its signature with a
      *> deliberately perturbed copy, so a green run cannot be a
      *> comparison that never reports a difference.  SIG-NONZERO
      *> guards a signature that is vacuously equal because nothing
      *> was computed; ALPHA-LOWER is pinned by §8.8.4.4.4 GR3c2
      *> ("a" is a lowercase letter and no locale is in effect).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1OCW01.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. WISE-OWL-9000.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-G.
           05 WS-G-A   PIC X(3).
           05 WS-G-L   USAGE BINARY-LONG.
           05 WS-G-D   USAGE BINARY-DOUBLE.
       01 WS-UP        PIC X VALUE "A".
       01 WS-UP2       PIC X VALUE "B".
       01 WS-LOW       PIC X VALUE "a".
       01 WS-CMP       PIC 9 VALUE 0.
       01 WS-CLS       PIC 9 VALUE 0.
       01 WS-SIG-A     PIC 9(12) VALUE 0.
       01 WS-SIG-X     PIC 9(12) VALUE 0.
       01 WS-PCS       PIC 9 VALUE 0.
       01 WS-CC-A      PIC 9(5) VALUE 0.
       01 WS-CC-B      PIC 9(5) VALUE 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 0 TO WS-CMP
           IF WS-UP < WS-UP2
               MOVE 1 TO WS-CMP
           END-IF
           MOVE 0 TO WS-CLS
           IF WS-LOW IS ALPHABETIC-LOWER
               MOVE 1 TO WS-CLS
           END-IF
           COMPUTE WS-SIG-A = FUNCTION BYTE-LENGTH(WS-G) * 1000000
                            + FUNCTION ORD(WS-UP) * 100
                            + WS-CMP * 10 + WS-CLS
           IF WS-SIG-A > 0
               DISPLAY "SIG-NONZERO=YES"
           ELSE
               DISPLAY "SIG-NONZERO=NO"
           END-IF
           IF WS-CLS = 1
               DISPLAY "ALPHA-LOWER=YES"
           ELSE
               DISPLAY "ALPHA-LOWER=NO"
           END-IF
      *> PART 2 — PROGRAM COLLATING SEQUENCE with and without a name.
           MOVE 0 TO WS-PCS
           CALL "L1OCW05" USING WS-PCS
           IF WS-PCS = 1
               DISPLAY "PCS-WITH-NAME=REV"
           ELSE
               DISPLAY "PCS-WITH-NAME=NATIVE"
           END-IF
           MOVE 0 TO WS-PCS
           CALL "L1OCW06" USING WS-PCS
           IF WS-PCS = 1
               DISPLAY "PCS-NO-NAME=REV"
           ELSE
               DISPLAY "PCS-NO-NAME=NATIVE"
           END-IF
      *> PART 2 — CHARACTER CLASSIFICATION with and without a name.
           MOVE 0 TO WS-CC-A
           CALL "L1OCW07" USING WS-CC-A
           MOVE 0 TO WS-CC-B
           CALL "L1OCW08" USING WS-CC-B
           IF WS-CC-A > 0
               DISPLAY "CC-NONZERO=YES"
           ELSE
               DISPLAY "CC-NONZERO=NO"
           END-IF
           IF WS-CC-B = WS-CC-A
               DISPLAY "CC-NAME-VS-NO-NAME=SAME"
           ELSE
               DISPLAY "CC-NAME-VS-NO-NAME=DIFFER"
           END-IF
      *> PART 1 — the equipment-configuration signature, four shapes.
           MOVE 0 TO WS-SIG-X
           CALL "L1OCW02" USING WS-SIG-X
           IF WS-SIG-X = WS-SIG-A
               DISPLAY "OTHER-NAME=SAME"
           ELSE
               DISPLAY "OTHER-NAME=DIFFER"
           END-IF
           MOVE 0 TO WS-SIG-X
           CALL "L1OCW03" USING WS-SIG-X
           IF WS-SIG-X = WS-SIG-A
               DISPLAY "NO-NAME=SAME"
           ELSE
               DISPLAY "NO-NAME=DIFFER"
           END-IF
           MOVE 0 TO WS-SIG-X
           CALL "L1OCW04" USING WS-SIG-X
           IF WS-SIG-X = WS-SIG-A
               DISPLAY "NO-PARAGRAPH=SAME"
           ELSE
               DISPLAY "NO-PARAGRAPH=DIFFER"
           END-IF
           COMPUTE WS-SIG-X = WS-SIG-A + 1
           IF WS-SIG-X = WS-SIG-A
               DISPLAY "CONTROL=SAME"
           ELSE
               DISPLAY "CONTROL=DIFFER"
           END-IF
           STOP RUN.
       END PROGRAM L1OCW01.
      *> Leg (d) — a DIFFERENTLY SPELLED computer-name.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1OCW02.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. ACME-4381.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-G.
           05 WS-G-A   PIC X(3).
           05 WS-G-L   USAGE BINARY-LONG.
           05 WS-G-D   USAGE BINARY-DOUBLE.
       01 WS-UP        PIC X VALUE "A".
       01 WS-UP2       PIC X VALUE "B".
       01 WS-LOW       PIC X VALUE "a".
       01 WS-CMP       PIC 9 VALUE 0.
       01 WS-CLS       PIC 9 VALUE 0.
       LINKAGE SECTION.
       01 LS-SIG       PIC 9(12).
       PROCEDURE DIVISION USING LS-SIG.
       SUB-PARA.
           MOVE 0 TO WS-CMP
           IF WS-UP < WS-UP2
               MOVE 1 TO WS-CMP
           END-IF
           MOVE 0 TO WS-CLS
           IF WS-LOW IS ALPHABETIC-LOWER
               MOVE 1 TO WS-CLS
           END-IF
           COMPUTE LS-SIG = FUNCTION BYTE-LENGTH(WS-G) * 1000000
                          + FUNCTION ORD(WS-UP) * 100
                          + WS-CMP * 10 + WS-CLS
           GOBACK.
       END PROGRAM L1OCW02.
      *> Leg (b) — the paragraph is present, computer-name-1 ABSENT
      *> (§12.3.6.4 GR3; §12.3.6.3 SR4 drops the second period).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1OCW03.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-G.
           05 WS-G-A   PIC X(3).
           05 WS-G-L   USAGE BINARY-LONG.
           05 WS-G-D   USAGE BINARY-DOUBLE.
       01 WS-UP        PIC X VALUE "A".
       01 WS-UP2       PIC X VALUE "B".
       01 WS-LOW       PIC X VALUE "a".
       01 WS-CMP       PIC 9 VALUE 0.
       01 WS-CLS       PIC 9 VALUE 0.
       LINKAGE SECTION.
       01 LS-SIG       PIC 9(12).
       PROCEDURE DIVISION USING LS-SIG.
       SUB-PARA.
           MOVE 0 TO WS-CMP
           IF WS-UP < WS-UP2
               MOVE 1 TO WS-CMP
           END-IF
           MOVE 0 TO WS-CLS
           IF WS-LOW IS ALPHABETIC-LOWER
               MOVE 1 TO WS-CLS
           END-IF
           COMPUTE LS-SIG = FUNCTION BYTE-LENGTH(WS-G) * 1000000
                          + FUNCTION ORD(WS-UP) * 100
                          + WS-CMP * 10 + WS-CLS
           GOBACK.
       END PROGRAM L1OCW03.
      *> Leg (c) — NO OBJECT-COMPUTER paragraph at all, and no
      *> CONFIGURATION SECTION to inherit one from (§12.3.6.4 GR4).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1OCW04.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-G.
           05 WS-G-A   PIC X(3).
           05 WS-G-L   USAGE BINARY-LONG.
           05 WS-G-D   USAGE BINARY-DOUBLE.
       01 WS-UP        PIC X VALUE "A".
       01 WS-UP2       PIC X VALUE "B".
       01 WS-LOW       PIC X VALUE "a".
       01 WS-CMP       PIC 9 VALUE 0.
       01 WS-CLS       PIC 9 VALUE 0.
       LINKAGE SECTION.
       01 LS-SIG       PIC 9(12).
       PROCEDURE DIVISION USING LS-SIG.
       SUB-PARA.
           MOVE 0 TO WS-CMP
           IF WS-UP < WS-UP2
               MOVE 1 TO WS-CMP
           END-IF
           MOVE 0 TO WS-CLS
           IF WS-LOW IS ALPHABETIC-LOWER
               MOVE 1 TO WS-CLS
           END-IF
           COMPUTE LS-SIG = FUNCTION BYTE-LENGTH(WS-G) * 1000000
                          + FUNCTION ORD(WS-UP) * 100
                          + WS-CMP * 10 + WS-CLS
           GOBACK.
       END PROGRAM L1OCW04.
      *> PART 2 leg — PROGRAM COLLATING SEQUENCE follows a NAME.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1OCW05.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. WISE-OWL-9000
           PROGRAM COLLATING SEQUENCE IS REV.
       SPECIAL-NAMES.
           ALPHABET REV IS "B" "A".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X            PIC X VALUE "A".
       LINKAGE SECTION.
       01 LS-PCS       PIC 9.
       PROCEDURE DIVISION USING LS-PCS.
       SUB-PARA.
           MOVE 0 TO LS-PCS
           IF X > "B"
               MOVE 1 TO LS-PCS
           END-IF
           GOBACK.
       END PROGRAM L1OCW05.
      *> PART 2 leg — the SAME clause with NO computer-name (the
      *> 2002 relaxation; kb/Work PB78).  GR9/GR11 must answer
      *> identically: that is claim (2) of the determination.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1OCW06.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER.
           PROGRAM COLLATING SEQUENCE IS REV.
       SPECIAL-NAMES.
           ALPHABET REV IS "B" "A".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X            PIC X VALUE "A".
       LINKAGE SECTION.
       01 LS-PCS       PIC 9.
       PROCEDURE DIVISION USING LS-PCS.
       SUB-PARA.
           MOVE 0 TO LS-PCS
           IF X > "B"
               MOVE 1 TO LS-PCS
           END-IF
           GOBACK.
       END PROGRAM L1OCW06.
      *> PART 2 leg — CHARACTER CLASSIFICATION follows a NAME.
      *> The value returned is FUNCTION ORD of the first character
      *> of FUNCTION UPPER-CASE("i") (§12.3.6.4 GR7 a).  It is NEVER
      *> compared with a pinned Turkish code point — only with the
      *> name-less leg's answer — so the leg is correct whether or
      *> not the environment provides "tr-TR" (§8.2.1 makes locale
      *> availability a RUNTIME property).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1OCW07.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. WISE-OWL-9000
           CHARACTER CLASSIFICATION IS TR.
       SPECIAL-NAMES.
           LOCALE TR IS "tr-TR".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S            PIC X(4).
       LINKAGE SECTION.
       01 LS-CC        PIC 9(5).
       PROCEDURE DIVISION USING LS-CC.
       SUB-PARA.
           MOVE FUNCTION UPPER-CASE("i") TO S
           MOVE FUNCTION ORD(S(1:1)) TO LS-CC
           GOBACK.
       END PROGRAM L1OCW07.
      *> PART 2 leg — the SAME clause with NO computer-name.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1OCW08.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER.
           CHARACTER CLASSIFICATION IS TR.
       SPECIAL-NAMES.
           LOCALE TR IS "tr-TR".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S            PIC X(4).
       LINKAGE SECTION.
       01 LS-CC        PIC 9(5).
       PROCEDURE DIVISION USING LS-CC.
       SUB-PARA.
           MOVE FUNCTION UPPER-CASE("i") TO S
           MOVE FUNCTION ORD(S(1:1)) TO LS-CC
           GOBACK.
       END PROGRAM L1OCW08.
