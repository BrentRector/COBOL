*>--- conformance-case
*> id: l1-formatted-datetime-offset-local-format
*> rules: [AR-15.40.3-6]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.40.3 r6 — "Argument-4 shall not be specified if the time portion of the format in
      *> argument-1 is neither a UTC format nor an offset format."
      *> (cite.py --check 15.40.3 "Argument-4 shall not be specified if the time portion of the format in
      *> argument-1 is neither a UTC format nor an offset format" -> OK, §15.40.3 rule 6.)
      *>
      *> THE RULE IS FULLY COMPILE-TIME DECIDABLE and therefore a DIAGNOSTIC, not a run-time check: r1
      *> already makes argument-1 a literal, so the binder holds the format string and the argument COUNT at
      *> the same point. "YYYYMMDDThhmmss" is a combined date and time format whose time portion is a
      *> COMMON time format — §15.3.3.5 makes a UTC format "a common time format followed by 'Z'", and
      *> §15.3.3.6 makes an offset format one carrying an explicit '+hhmm' / '+hh:mm' subformat — so this
      *> format's time portion is LOCAL and is neither. The fourth argument is therefore barred.
      *>
      *> ⛔ THE PREDICATE IS THE §15.3.1/§15.3.2 FORMAT RECOGNISER, NOT AN 'is there a Z anywhere' PROXY:
      *> the zone comes from DateTimeFormatGrammar.Describe's classifier, which is why a trailing 'Z' on a
      *> DATE portion cannot be mistaken for a UTC time portion.
      *>
      *> MEASURED BEFORE kb/Work PB11: this exact reference compiled with zero diagnostics, printed
      *> 20210616T123456 and SILENTLY DISCARDED argument-4 — the value the program wrote had no effect and
      *> no report, which is the fabricated-agreement failure mode, not over-acceptance.
      *>
      *> ⚠ THE CONVERSE IS LEGAL AND MUST STAY LEGAL: omitting argument-4 for a UTC or offset format "shall
      *> be evaluated as though 0 were specified" (§15.40.3 r7), so the screen is deliberately one-sided.
      *> The legal side of THIS rule — argument-4 written against an OFFSET format — is
      *> conformance:2014/l1_formatted_datetime_offset_magnitude, which would go red if the screen widened.
      *> Expected: COBOLNET1633, the datetime-offset-argument-not-permitted diagnostic.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGL1FDTLOC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P20 PIC X(20).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION FORMATTED-DATETIME("YYYYMMDDThhmmss", 153569,
               45296, 120) TO P20.
           DISPLAY P20.
           STOP RUN.
       END PROGRAM NEGL1FDTLOC.
