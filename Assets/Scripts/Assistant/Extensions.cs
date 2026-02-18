#region License
// Copyright (C) 2022-2025 Sascha Puligheddu
// 
// This project is a complete reproduction of AssistUO for MobileUO and ClassicUO.
// Developed as a lightweight, native assistant.
// 
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// 
// SPECIAL PERMISSION: Integration with projects under BSD 2-Clause (like ClassicUO)
// is permitted, provided that the integrated result remains publicly accessible 
// and the AGPL-3.0 terms are respected for this specific module.
//
// This program is distributed WITHOUT ANY WARRANTY. 
// See <https://www.gnu.org> for details.
#endregion

using System.Collections.Generic;
using System;
using ClassicUO.Game.GameObjects;

public static class IEnumerableExtensions
{
    public static IEnumerable<T> Flatten<T>(this IEnumerable<T> source, Func<T, IEnumerable<T>> childrenSelector, Predicate<T> shouldRecurse = null)
    {
        foreach (var item in source)
        {
            // Returns current element
            yield return item;

            if (shouldRecurse != null && !shouldRecurse(item))
                continue;

            // Recovers childrens recursively
            var children = childrenSelector(item);
            if (children != null)
            {
                foreach (var child in Flatten(children, childrenSelector))
                {
                    yield return child;
                }
            }
        }
    }
}