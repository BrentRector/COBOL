*>--- conformance-case
*> id: declined-validate-class
*> rules: [SR-13.18.11.3-1, GR-13.18.11.4-1, GR-13.18.11.4-2, GR-13.18.11.4-3, GR-13.18.11.4-4, FMT-13.18.11.2]
*> source-format: free
*> portable: unverified
*>---
*> ISO 13.18.11 CLASS clause - the DECLINED VALIDATE facility (Annex A.4.14), by OWNER DECISION 2026-09-02
*> (kb/Work PB375). The annex never LISTS this clause; it reaches the module through 13.16.2 Format 1, whose
*> printed validation-clauses group opens with `[ class-clause ]` and maps it to "13.18.11, CLASS clause"
*> (RENDERED, PDF p394 / folio 364), and 13.18.11.1 gives it no content outside the module: "to be checked
*> during the content validation stage of the execution of a VALIDATE statement". A.4.1 admits an optional
*> element's syntax only when support is claimed, so the clause is refused BY NAME (COBOLNET1708) where it
*> drew the bare "COBOL0307: unexpected 'CLASS'" measured before this landing.
*> BOTH OPERAND ARMS ARE EXERCISED - a class-name-1 (via cobolWord) and one of the printed keywords, the
*> latter written WITHOUT the optional word IS (13.18.11.2: IS is not underlined). The keyword arm is the one
*> that would regress if the operand alternation were inlined into the clause rule: the derived clause namer
*> in DeclinedFacilityPass takes the leading TERMINAL run, so an inlined `CLASS IS NUMERIC` would report "the
*> CLASS NUMERIC clause". Make it fail once: swap the expected code to COBOLNET1580 and it goes red.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DCLCLASS.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CLASS HEXDIG IS "0" THRU "9".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-REC.
          05 WS-A PIC X(4) CLASS IS HEXDIG.
          05 WS-B PIC X(4) CLASS ALPHABETIC-LOWER.
       PROCEDURE DIVISION.
           DISPLAY "UNREACHABLE".
           STOP RUN.
