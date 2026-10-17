*>--- conformance-case
*> id: pb487-message-tag-usage
*> rules: [GR-8.4.3.10.4-4, GR-8.8.4.2.1-9, SR-13.18.60.3-21, GR-13.18.60.4-9, SR-14.9.39.3-35, GR-14.9.39.4-40, GR-14.9.39.4-41, DOC-A.1-212]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB487 - the BARE (USAGE-less) spelling of the MESSAGE-TAG usage.  ISO 13.18.60.2 prints
      *> `[ USAGE IS ]` as OPTIONAL for every usage in its general format, so `01 M MESSAGE-TAG.` IS the USAGE
      *> clause - but MESSAGE-TAG had no lexer token and no usageKeyword arm, so the word fell to the 13.16.2
      *> vendor catch-all and was DISCARDED.  The entry then bound with no usage and no PICTURE at all, and the
      *> COMPILER ITSELF failed: an unhandled System.NullReferenceException in MoveEmitter on a MOVE into it,
      *> or a raw Roslyn CS0103 the user cannot act on.
      *> The conforming answer is a named refusal.  13.18.60.4 GR9: "The MESSAGE-TAG clause specifies that a
      *> data item is a message-tag data item and contains an implementor-defined value that identifies a
      *> message and a server or requestor.  The class and category of a message-tag data item are message-tag."
      *> That facility is the asynchronous messaging module, a processor-dependent element (4.2.6; Annex A.3
      *> item 4) this implementation does not support - docs/CONFORMANCE.md section 4 item 1.  Its SEND/RECEIVE
      *> statements are accepted inert (COBOLNET1578) because an inert statement changes nothing observable;
      *> the DATA item cannot be, because GR9 fixes its class and category, so an accepted item would bind as
      *> some OTHER class and every reference to it would answer wrong.  Hence COBOLNET1943, an Error.
      *> ONLY at 2023: MESSAGE-TAG is a COBOL-2023 addition (Annex E.2 item 25) and is not reserved below it,
      *> where the same source is a different program and draws the ordinary introduction gate instead.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB487MT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 M MESSAGE-TAG.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
