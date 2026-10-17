*>--- conformance-case
*> id: pb248-integer-arg-float-item
*> rules: [AR-15.90.3-1, AR-15.91.3-1, AR-15.100.3-1, AR-15.100.3-2,
*>    AR-15.100.3-4]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB248 - THE 'i' (ISO 15.3 type 6) SCREEN HAD NO FLOATING-POINT ARM.
*> THE GOVERNING TEXT IS 15.3 TYPE 6 ITSELF, and the argument is PB40's applied to the arm PB40's own
*> primitive could not see. Type 6 admits exactly two things: "an arithmetic expression that will ALWAYS
*> result in an integer value OR an integer data item". A floating-point item is neither. It is not an
*> always-integral arithmetic expression, because 14.6.8.3 sets a floating-point item's content to "the
*> algebraic value of the sending operand" - its DECLARED value set contains non-integers, so no reference
*> to it is provably integral. That a run happens to store 7.0 in it is the same irrelevance as a PIC 9V9
*> happening to hold 7.0, which the scale arm has rejected since PB40. IT IS A TYPE TEST, NOT A VALUE TEST.
*> 5.5 2)b)2. CORROBORATES AND DOES NOT GOVERN: it defines the term for an identifier operand as "a
*> FIXED-POINT numeric data item ... whose description does not include any digit positions to the right of
*> the radix point" - this arm and the scale arm in one sentence - but 5.5 2) scopes itself to "a syntax
*> rule", and 5.3.1 says "Except for intrinsic functions, rules are categorized as syntax rules and general
*> rules. Intrinsic functions have argument rules and returned value rules INSTEAD".
*> WHY IT WAS INVISIBLE: a floating-point item (14.6.8.3 - "a data item described with the FLOAT-SHORT usage,
*> the FLOAT-LONG usage, the FLOAT-EXTENDED usage, or any standard floating-point usage", plus the COMP-1 /
*> COMP-2 synonyms) is PICTURE-less, so its synthesized profile carries Scale 0 and a scale-only test admits
*> it. Nothing consulted the float flag here.
*> WHAT IT DID: FUNCTION TEST-DATE-YYYYMMDD over a COMP-2 holding 20240229.9 compiled clean, was truncated at
*> the runtime seam and answered 0 - "the date is valid" - on an argument that is not a date at all.
*> CHAR (15.15.3 r1) is the 85-and-later type-6 witness used here.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB248FLOATITEM.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 WS-F USAGE COMP-2.
01 R PIC 9(3).
PROCEDURE DIVISION.
MAIN.
    MOVE 65.5 TO WS-F.
    COMPUTE R = FUNCTION ORD(FUNCTION CHAR(WS-F)).
    STOP RUN.
