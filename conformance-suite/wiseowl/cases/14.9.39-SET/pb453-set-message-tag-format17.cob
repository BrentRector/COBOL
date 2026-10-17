*>--- conformance-case
*> id: pb453-set-message-tag-format17
*> rules: [SR-14.9.39.3-35, GR-14.9.39.4-40, GR-14.9.39.4-41]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB453 - SET Format 17 (message-tag) NAMES THE DECLINED FACILITY instead of drawing another
      *> format's rule.
      *> ISO 14.9.39.2 Format 17 is `SET data-name-4 TO { data-name-5 | NULL }` and 14.9.39.3 SR35 - "Data-name-4
      *> and data-name-5 shall be message-tag data items" (cite.py --check 14.9.39.3 OK) - makes BOTH operands
      *> message-tag, a class 13.18.60.4 GR9 gives only the MESSAGE-TAG usage.  That usage is the DATA half of
      *> the asynchronous messaging facility, a processor-dependent element (4.2.6; Annex A.3 item 4) this
      *> implementation does not support (docs/CONFORMANCE.md section 4 item 1), and 4.2.6 paragraph 3 makes a
      *> compile-time warning mechanism naming it MANDATORY.
      *> MEASURED BEFORE THE FIX, on this worktree's own build: the entry drew its named COBOLNET1943, and then
      *> the STATEMENT drew `COBOLNET0867: SET 'MT1': the receiving operand of an object-reference SET shall be
      *> a USAGE OBJECT REFERENCE data item (ISO 14.9.39.3 SR8)` - Format 5's rule, about a statement nobody
      *> wrote, naming a repair that is not one.  The sibling `SET MT1 TO MT2` drew COBOLNET0844, the 8.8.1.1
      *> arithmetic screen, calling a message-tag item "of category alphanumeric".
      *> Both arms are here on purpose: `TO NULL` reaches the format selection through
      *> setObjectReferenceStatement and `TO MT2` through setToValueStatement, and it was the FIRST arm that
      *> produced the SR8 diagnostic - one dispatch, two arms (feedback_two_arm_dispatch).
      *> 2023 ONLY, DELIBERATELY: MESSAGE-TAG is an Annex E.2 item-25 COBOL-2023 addition, so below 2023 the
      *> word is an ordinary user-defined word and the edition-correct answer is the introduction gate.  That
      *> routing is broken for the whole MESSAGE-TAG surface (COBOLNET1943 fires at 2002 and 2014 too, against
      *> its own message text) and is registered as its own mechanism; pinning 2002 here would freeze the
      *> defect (feedback_green_test_can_hold_a_gap_open).  The entry's own witness is
      *> negative/pb487-message-tag-usage; this one is the STATEMENT's.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGPB453F17.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 MT1 USAGE MESSAGE-TAG.
       01 MT2 USAGE MESSAGE-TAG.
       PROCEDURE DIVISION.
       MAIN-P.
           SET MT1 TO NULL.
           SET MT1 TO MT2.
           STOP RUN.
