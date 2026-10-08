// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// The WINDOW GEOMETRY of a variable-length group that lives in a <see cref="StorageCell"/> (kb/Work PB244): the same
/// facts a cell-backed group's helpers (<see cref="StorageCell.ContiguousAt"/> and its siblings) take as loose
/// arguments, bundled so that an ELEMENT of a dynamic-capacity table can be described one level down. A table's
/// elements are element cells that are a scope of their own (ISO §8.5.1.9.1; <c>CellComponents</c>), numbered from
/// zero, so an element that is itself a variable-length group (a runtime-length item or a nested table inside it)
/// composes its image with its own shape - the cell of the element cannot say what its slots mean.
/// </summary>
/// <param name="Width">The element's window width: its fixed run plus the one-element extent each nested dynamic-capacity
/// table reserves (<paramref name="DynTable"/>).</param>
/// <param name="DynFixedAt">Each component's position in the window, relative to the element.</param>
/// <param name="DynTable">Each component's table element width, 0 for a dynamic-length item.</param>
/// <param name="Elems">For each component that is a table, the shape of ITS element when that element is itself a
/// variable-length group; null where every component's element is a fixed image.</param>
public sealed record CellGroupShape(int Width, int[] DynFixedAt, int[] DynTable, CellGroupShape?[]? Elems = null);
