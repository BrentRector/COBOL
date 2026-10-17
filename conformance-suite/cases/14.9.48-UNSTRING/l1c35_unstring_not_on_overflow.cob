      *>--- conformance-case
      *> id: l1c35_unstring_not_on_overflow
      *> rules: [GR-14.9.48.4-17]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.48.4 GR17 — no overflow: ON OVERFLOW ignored, NOT ON
      *>   runs
      *> "If, at the time of execution of an UNSTRING statement, the
      *> conditions described in General rule 15 are not encountered,
      *>   after
      *> completion of the transfer of data according to the other
      *>   general
      *> rules, the ON OVERFLOW phrase, if specified, is ignored and
      *>   control
      *> is transferred to the end of the UNSTRING statement or, if the
      *>   NOT
      *> ON OVERFLOW phrase is specified, to imperative-statement-2. If
      *> control is returned from imperative-statement-2, control is
      *>   then
      *> transferred to the end of the UNSTRING statement."
      *> OK  §14.9.48.4 17)  (General rules)
      *> Sibling (the overflow branch, V5 only, for contrast):
      *> OK  §14.9.48.4 15) b) "all receiving areas have been acted
      *>   upon,
      *>     and the data item referenced by identifier-1 contains
      *>     characters that have not been examined"
      *> OK  §14.9.48.4 16) e) "The NOT ON OVERFLOW phrase, if
      *>   specified, is
      *>     ignored."
      *> GR13: P = initial value + characters examined; GR14: T =
      *>   initial
      *> value + receivers accessed.
      *> Derivation. S = "AB,CD" (5 chars), R1..R3 PIC XX preset "**".
      *> V1 P=1 T=0 INTO R1 R2: every character examined, both areas
      *>   used,
      *>    nothing left: no overflow -> imperative-statement-2 runs
      *>      AFTER
      *>    the transfer, so it sees P=06 T=2 R1 "AB" R2 "CD"; then
      *>      control
      *>    reaches the end of the statement: "V1 END".
      *> V2 INTO R1 R2 R3: S is exhausted with R3 unused - not a GR15
      *>    condition -> NOT branch: "V2 NOT [AB][CD][**]", "V2 END".
      *> V3 ON OVERFLOW only, no overflow: the phrase is ignored,
      *>   control
      *>    goes to the end: only "V3 END".
      *> V4 WITH POINTER 5 (= the size of S, so not GR15a): "D" is
      *>    examined into R1, P=06, no overflow -> "V4 NOT [D ] P=06".
      *> V5 INTO R1 only: "CD" unexamined -> overflow; ON branch only:
      *>    "V5 OVF", "V5 END" (NOT ignored, GR16e).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C35G.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S  PIC X(5) VALUE "AB,CD".
       01 R1 PIC XX.
       01 R2 PIC XX.
       01 R3 PIC XX.
       01 P  PIC 99.
       01 T  PIC 9.
       PROCEDURE DIVISION.
           PERFORM PRESET
           UNSTRING S DELIMITED BY "," INTO R1 R2
               WITH POINTER P TALLYING IN T
               ON OVERFLOW DISPLAY "V1 OVF"
               NOT ON OVERFLOW
                   DISPLAY "V1 NOT P=" P " T=" T " [" R1 "][" R2 "]"
           END-UNSTRING
           DISPLAY "V1 END"
           PERFORM PRESET
           UNSTRING S DELIMITED BY "," INTO R1 R2 R3
               ON OVERFLOW DISPLAY "V2 OVF"
               NOT ON OVERFLOW
                   DISPLAY "V2 NOT [" R1 "][" R2 "][" R3 "]"
           END-UNSTRING
           DISPLAY "V2 END"
           PERFORM PRESET
           UNSTRING S DELIMITED BY "," INTO R1 R2
               ON OVERFLOW DISPLAY "V3 OVF"
           END-UNSTRING
           DISPLAY "V3 END"
           PERFORM PRESET
           MOVE 5 TO P
           UNSTRING S DELIMITED BY "," INTO R1 WITH POINTER P
               ON OVERFLOW DISPLAY "V4 OVF"
               NOT ON OVERFLOW DISPLAY "V4 NOT [" R1 "] P=" P
           END-UNSTRING
           DISPLAY "V4 END"
           PERFORM PRESET
           UNSTRING S DELIMITED BY "," INTO R1
               ON OVERFLOW DISPLAY "V5 OVF"
               NOT ON OVERFLOW DISPLAY "V5 NOT"
           END-UNSTRING
           DISPLAY "V5 END"
           STOP RUN.
       PRESET.
           MOVE "**" TO R1 R2 R3
           MOVE 1 TO P
           MOVE 0 TO T.
