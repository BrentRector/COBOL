*>--- conformance-case
*> id: pb183-usage-pointer-in-weak-typedef
*> rules: [SR-13.18.60.3-14]
*> source-format: free
*> portable: unverified
*>---
*> ISO 13.18.60.3 SR14, second arm - THE ONCE-PER-DECLARATION FIXTURE. SR14 admits a subordinate
*> pointer only under "a type declaration that includes the STRONG phrase". W is a WEAK typedef, so
*> WP is nonconforming; and the verdict must land ONCE, at the TEMPLATE, however many TYPE W
*> reference sites exist. Two are written here for exactly that reason: a screen keyed on the
*> post-expansion strong flag rather than on the declaration-side TYPEDEF STRONG would miss the
*> template and then fire once per reference site - wrong site, wrong count, and a diagnostic naming
*> a line the programmer cannot fix.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB183D.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W IS TYPEDEF.
   05 WP USAGE POINTER.
   05 WA PIC X(4).
01 V1 TYPE W.
01 V2 TYPE W.
PROCEDURE DIVISION.
MAIN.
    SET WP IN V1 TO NULL.
    SET WP IN V2 TO NULL.
    STOP RUN.
