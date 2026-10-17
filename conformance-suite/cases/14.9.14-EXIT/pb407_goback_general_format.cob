*>--- conformance-case
*> id: pb407_goback_general_format
*> rules: [SR-14.9.14.3-6, FMT-14.9.18.2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB407 - ISO 14.9.18.2's GENERAL FORMAT, read from the printed figure (PDF page 661 /
      *> printed folio 631, rendered at 300 dpi). The tail bracket carries CHOICE INDICATORS, so per
      *> ISO 5.2.6.4 "zero or more of the alternatives contained within the choice indicators shall be
      *> specified, but any single alternative may be specified only once" and "The alternatives may be
      *> specified in any order" - both phrases, either order, each once. STATUS is not underlined, so
      *> ISO 5.2.3 makes it an optional word; the EXCEPTION after LAST is not underlined either.
      *>
      *> EXPECTED VALUES ARE DERIVED FROM THE RULES, NOT MEASURED. Each subprogram DISPLAYs before its
      *> GOBACK and the main program DISPLAYs after each CALL, so the order is fixed by ISO 14.9.18.4 GR2
      *> ("control to return to the calling statement"). The status phrase reaches the operating system
      *> ONLY in a main program (GR7-GR10 each open "If the GOBACK ... is executing in a main program"),
      *> so in these CALLED programs it is screened and then inert - the run unit's own STOP RUN decides
      *> the termination indication. The RAISING phrase raises in the ACTIVATOR only "if checking for that
      *> exception condition is enabled in the activating runtime element" (GR1 b)); no >>TURN appears
      *> here, so nothing is raised and every CALL returns normally.
      *>
      *> The status phrase is a COBOL-2023 introduction (Annex E.3.3 item 32), which is why this golden
      *> lives at 2023. The RAISING LAST spelling in PB407D is 2002-available and is carried here because
      *> it shares the one raisingPhrase grammar rule with the spellings above it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB407M.
       PROCEDURE DIVISION.
       MAIN.
           CALL "PB407A".
           DISPLAY "AFTER-A".
           CALL "PB407B".
           DISPLAY "AFTER-B".
           CALL "PB407C".
           DISPLAY "AFTER-C".
           CALL "PB407D".
           DISPLAY "AFTER-D".
           STOP RUN.
       END PROGRAM PB407M.

      *> Both alternatives of the choice-indicator bracket, raising-phrase first.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB407A.
       PROCEDURE DIVISION RAISING EC-USER-PBFMT.
       P.
           DISPLAY "IN-A".
           GOBACK RAISING EXCEPTION EC-USER-PBFMT WITH NORMAL STATUS 0.
       END PROGRAM PB407A.

      *> The same two alternatives in the OTHER order - 5.2.6.4's "in any order".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB407B.
       PROCEDURE DIVISION RAISING EC-USER-PBFMT.
       P.
           DISPLAY "IN-B".
           GOBACK WITH NORMAL STATUS 0 RAISING EXCEPTION EC-USER-PBFMT.
       END PROGRAM PB407B.

      *> STATUS omitted before the operand - an optional word per 5.2.3.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB407C.
       PROCEDURE DIVISION.
       P.
           DISPLAY "IN-C".
           GOBACK WITH NORMAL 0.
       END PROGRAM PB407C.

      *> LAST without the trailing EXCEPTION, written in a declarative procedure - the position
      *> ISO 14.9.18.3 SR5 admits. The declarative is never selected here; the statement is present for
      *> its SYNTAX.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB407D.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-SEC SECTION. USE AFTER EXCEPTION CONDITION EC-SIZE.
       D-P.
           DISPLAY "IN-D-DECL".
           GOBACK RAISING LAST.
       END DECLARATIVES.
       M-SEC SECTION.
       M-P.
           DISPLAY "IN-D".
           GOBACK.
       END PROGRAM PB407D.
