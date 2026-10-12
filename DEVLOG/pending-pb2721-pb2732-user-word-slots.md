## PB2721 + PB2732: a name slot is cobolWord, never a bare IDENTIFIER, and an index-name is a word

Wave 1069 H took two R2 findings that share one root: a user-defined-word slot in the grammar that does not admit the
word set its declaration admits. ISO §8.3.2.1 3) lets a §8.10 context-sensitive word be a user-defined word outside its
construct, and many of those words (CAPACITY, CYCLE, BYTES, FOREVER, ...) carry a lexer token that `cobolWord` admits and
`IDENTIFIER` does not.

**PB2721, two reference slots.** The re-probe on the worktree build reproduced both: `01 CAPACITY TYPEDEF PIC 9(3).`
then `01 V2 TYPE CAPACITY.` answered `COBOL0001: unexpected 'CAPACITY'`, and `FUNCTION-ID. CAPACITY.` compiled while
`REPOSITORY. FUNCTION CAPACITY.` and `FUNCTION CAPACITY(V)` each answered `COBOL0001`. `typeClause` read `TYPE TO?
IDENTIFIER` and `functionName` read `IDENTIFIER` plus nine reserved intrinsic-name tokens. Now `typeClause` reads
`TYPE TO? cobolWord`, and `functionName` is `cobolWord | intrinsicKeywordName`, the second rule holding the nine tokens.
The split matters beyond tidiness: `TokenRetypes` read the FIRST set of `functionName` off the ATN to decide which
reserved tokens keep their '(' a subscript trigger (the keyword-omitted intrinsic call), and the cobolWord arm would
have swept every context-sensitive token into that set. It now reads `intrinsicKeywordName`. The arm also brings the
word after FUNCTION under the §8.9 funnel, which screens every IDENTIFIER `cobolWord` position-blind; I measured that
the only §15 names `reserved-words.json` reserves are LENGTH, RANDOM, SIGN and SUM, all tokens in the other arm, so no
intrinsic call can draw a false COBOLNET0901.

**PB2732, a defining slot spelled as a reference.** OCCURS INDEXED BY took a `dataReferenceList`, and the only reader
took each reference's `GetText()`, which glues tokens: `INDEXED BY IX OF G` compiled clean and declared an index named
`IXOFG` (re-probed: exit 0; `SET IX TO 2` then said `IX` is not defined; a later `01 IXOFG` collided with an index the
user never wrote). §13.18.38.2 prints `{ index-name-1 } …`, a defining word (§8.3.2.2.11), so both OCCURS alternatives
now take `indexNameList : cobolWord (COMMA? cobolWord)*`, and a qualifier or subscript is the syntax error the format
makes it (`COBOL0307: unexpected 'OF'`, at every edition). The sibling sweep found no other glue: the façade's other
reference accessor is PB1281's REDEFINES, and every DataBinder clause read of a `dataReference` goes through
ClauseDataName, KeyReference or WrittenText.

**The guard.** `UserWordSlotDriftTests` reads every parser `.g4` and fails on a bare `IDENTIFIER` outside a
predicate-led keyword (`{wordAhead("...")}? IDENTIFIER`) and the seven rules it argues by name (cobolWord itself,
formatWord, enterOperand, genericClause, and the three tokenless-keyword clauses for COMMA and MINUS). A stale argued
entry fails too, and a probe grammar proves the scan reports a bare IDENTIFIER and passes a predicate-led one.

**Goldens and rows.** `conformance:2002/pb2721_context_word_user_word_slots` (TYPE CYCLE, TYPE TO BYTES, FUNCTION
CAPACITY, `INDEXED BY FOREVER, I2`) prints `007 WXYZ`, `6`, `ZQZ` at 2002, 2014 and 2023;
`conformance:negative/pb2732-qualified-index-name` rejects at 85 through 2023. SR-8.3.2.1-3 was already CONFORMS (PB655,
which made those tokens cobolWord alternatives); it gains the golden and the drift test as witnesses. Found on the way
(I9): the `type-clause-2002` construct cited §13.18.58 (TYPEDEF) for the TYPE clause; it cites §13.18.57 now.
