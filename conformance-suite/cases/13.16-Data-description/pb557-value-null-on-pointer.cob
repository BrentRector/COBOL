*>--- conformance-case
*> id: pb557-value-null-on-pointer
*> rules: [SR-13.16.3-10, SR-13.18.63.3-9]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 13.16.3 SR10 - "The VALUE clause shall not be specified for data items of class index,
*> message-tag, object, or pointer" - and 8.5.2.1 Table 2 files category data-pointer (USAGE POINTER) under
*> class pointer. (13.18.63.3 SR9 restates the rule for four usages and does not name POINTER; SR10 does, by
*> class - kb/Work PB515.) NULL is no escape: 8.4.3.10.1 makes it "a predefined address of class pointer or a
*> predefined content of class message-tag", an identifier under 8.4.3, not a literal.
*> Nothing is lost by the prohibition: 13.18.63.4 GR4 - "data items of class message-tag, class object, and
*> class pointer are initialized to null" - already gives the item this value with no clause written, which
*> tests/conformance/2002/pb557_pointer_class_initial_value_is_null pins. (COBOLNET2168)
*> MEASURED BEFORE: rc=70 with no COBOL diagnostic at all - the raw literal text reached the record-struct
*> initializer and Roslyn reported `CS0029: Cannot implicitly convert type 'string' to
*> 'CobolNet.Runtime.ManagedPointer'`, naming a generated .g.cs file.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB557PTRNULL.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 P USAGE POINTER VALUE NULL.
PROCEDURE DIVISION.
MAIN.
    DISPLAY "OK"
    STOP RUN.
