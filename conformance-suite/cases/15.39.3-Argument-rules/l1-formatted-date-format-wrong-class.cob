*>--- conformance-case
*> id: l1-formatted-date-format-wrong-class
*> rules: [AR-15.39.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.39.3 r1 — "Argument-1 shall be a national or alphanumeric LITERAL." One sentence, TWO
      *> constraints: a CLASS constraint and a LITERAL-ness constraint. B"1010" satisfies literal-ness
      *> outright, so this file cannot draw the COBOLNET1517 literal screen; §8.3.3.4.1 puts it in class
      *> BOOLEAN ("Boolean literals are of the class and category boolean"), which is neither "national"
      *> nor "alphanumeric", so the CLASS half is the only thing wrong here.
      *> ⛔ WHY IT IS A SEPARATE FILE. The class screen is PER FUNCTION —
      *> IntrinsicArgumentRules declares ["FORMATTED-DATE"] = Schema("§15.39.3 r1/r3", ['t','i']) — so
      *> pb124-formatted-alphabetic, which pins the same half for FORMATTED-CURRENT-DATE, is blind to a
      *> change in FORMATTED-DATE's own row (a 't' → 's' there would admit class alphabetic silently), and
      *> l1-formatted-date-format-not-literal reaches only the literal-ness half. pb11-format-wrong-kind is
      *> §15.39.3 r2's CONTENT screen (COBOLNET1631), a third question again.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1NFDTCLS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-S PIC X(8).
       PROCEDURE DIVISION.
           MOVE FUNCTION FORMATTED-DATE(B"1010" 143951) TO W-S
           STOP RUN.
       END PROGRAM L1NFDTCLS.
