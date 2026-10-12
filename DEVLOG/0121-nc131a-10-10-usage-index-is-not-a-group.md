## Entry 121 — 2026-03-20: NC131A 10/10 — USAGE INDEX Is Not a Group

### The bug

`DataSymbol.IsElementary` was defined as `PicString != null`. USAGE INDEX items have no PIC
clause, so they were classified as **groups** even when they had zero children. This caused
the storage layout to give them 1 byte via the empty-group fallback instead of 4 bytes via
the elementary path.

NC131A's TEST-4 and TEST-5 both compare USAGE INDEX items. TEST-4 compares a standalone
level-77 INDEX item with a table INDEXED BY index. TEST-5 compares a level-02 INDEX item
(child of a group) with the same level-77 item. Each failure pointed to a different layer
of the same root cause.

### The debugging odyssey

This took far too long — multiple iterations chasing the wrong layer:

1. **First attempt**: Normalize level-77 USAGE INDEX to S9(9) COMP in SemanticBuilder.
   Fixed TEST-4 but broke TEST-5 (level-02 items not covered).
2. **Second attempt**: Broaden normalization to all USAGE INDEX items. Broke group items
   — I-DATA-GROUP (level 01 with children) got a synthetic PIC, becoming elementary and
   losing its children.
3. **Third attempt**: Move to layout layer (FieldSizeCalculator + CompilerPicDescriptorFactory).
   Fixed the PicDescriptor and size, but the StorageLayoutComputer still routed the item
   through `LayoutGroup` → 1-byte fallback.
4. **Root cause found**: `IsElementary => PicString != null` was the wrong predicate. USAGE
   INDEX items without children ARE elementary. Fixed `IsElementary` and `IsGroup` to account
   for this.

### The fix (three layers)

| Layer | Change |
|-------|--------|
| `DataSymbol.IsElementary/IsGroup` | INDEX items without children are elementary |
| `FieldSizeCalculator` | USAGE INDEX → 4 bytes |
| `CompilerPicDescriptorFactory` | Elementary USAGE INDEX → S9(9) COMP PicDescriptor |
| `SemanticBuilder` | Level-77 USAGE INDEX → S9(9) COMP (early normalization) |

### Lesson

This is a variant of the "PIC-less elementary item" category. COBOL has items that are
elementary despite having no PIC clause: USAGE INDEX, USAGE POINTER, USAGE OBJECT REFERENCE.
The IsElementary predicate should account for all of them. Currently only INDEX is handled;
POINTER and OBJECT REFERENCE will need the same treatment when those features are implemented.

### Numbers

- NC131A: **10/10** (was 9/10 for 3 iterations, different test failing each time)
- NC140A: **70/70**, NC141A: **9/9** (from earlier this session)
- 119 unit, 182 integration (1 skip), guard ALL GREEN

---

