      *>--- conformance-case
      *> id: l1c08_nonfatal_enabled_no_handler
      *> rules: [GR-14.6.13.1.4-4]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.6.13.1.4 4) — an ENABLED nonfatal condition with no
      *> phrase, WHEN or USE: the statement continues per its own rules
      *> Rule 4): "Execution of the statement continues as specified in
      *>   the rules for that statement."
      *> cite.py:
      *>   OK  §14.6.13.1.4 4)  (Nonfatal exception conditions)
      *>   OK  §14.6.13.1.4 1)  (Nonfatal exception conditions) [rule 1
      *>       needs a conditional phrase WITHOUT NOT - none here]
      *>   OK  §14.9.43.4 8) a)  "No further data is transferred to the
      *>       data item referenced by identifier-3."
      *>   OK  §14.9.43.4 8) d)  "If the ON OVERFLOW phrase is not
      *>       specified, execution continues as specified in
      *>       14.6.13.1.4, Nonfatal exception conditions."
      *>   OK  §14.9.43.4 8) e)  "The NOT ON OVERFLOW phrase, if
      *>       specified, is ignored."
      *>   OK  §14.9.29.4 1)  NOTE "For nonfatal exception conditions
      *>       where there are no applicable exception processing
      *>       procedures, the RAISE statement acts as a CONTINUE
      *>       statement."
      *>   OK  §14.6.13.1.1   (General) "the associated exception
      *>       condition is raised, the last exception status is set to
      *>       indicate that exception condition"
      *> Checking is ON for both conditions (TURN), there are no
      *> declaratives, no PERFORM ... WHEN, and the STRING has no
      *> ON OVERFLOW phrase - so rules 1)-3) do not apply and rule 4)
      *> does. Both conditions are nonfatal (Table 13 EC-OVERFLOW-STRING
      *> NF; 14.6.13.1.1 "All user-defined exception conditions shall
      *> be nonfatal.").
      *> DERIVATION OF EVERY EXPECTED LINE:
      *> T=ABCD P=05   STRING moves A,B,C,D into positions 1-4 of T,
      *>               P reaching 5; before the 5th character P exceeds
      *>               4, so 8) a) no further transfer and the overflow
      *>               EC exists. Rule 4) -> STRING's own rules: 8) e)
      *>               the NOT ON OVERFLOW phrase is ignored, so no
      *>               NOT-OVF line precedes this one.
      *> ST=EC-OVERFLOW-STRING  checking was enabled, so the condition
      *>               was RAISED and the last exception status names
      *>               it (14.6.13.1.1) - the enabled branch.
      *> AFTER-RAISE   RAISE EXCEPTION EC-USER-GOON, enabled, nonfatal,
      *>               no handler: rule 4) -> RAISE acts as CONTINUE,
      *>               and the next statement executes.
      *> ST=EC-USER-GOON  the raise did happen (enabled).
       >>TURN EC-OVERFLOW-STRING CHECKING ON
       >>TURN EC-USER-GOON CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C08P.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T PIC X(4) VALUE "....".
       01 P PIC 99 VALUE 1.
       PROCEDURE DIVISION.
       MAIN-P.
           STRING "ABCDEF" DELIMITED BY SIZE INTO T WITH POINTER P
               NOT ON OVERFLOW DISPLAY "NOT-OVF"
           END-STRING
           DISPLAY "T=" T " P=" P
           DISPLAY "ST=" FUNCTION EXCEPTION-STATUS
           RAISE EXCEPTION EC-USER-GOON
           DISPLAY "AFTER-RAISE"
           DISPLAY "ST=" FUNCTION EXCEPTION-STATUS
           STOP RUN.
