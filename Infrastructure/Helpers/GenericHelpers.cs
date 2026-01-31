using System;
using System.Collections.Generic;
using System.Linq;

namespace EasyLingo.Infrastructure.Helpers
{
    public static class GenericHelpers
    {
        public static T? FindById<T>(IEnumerable<T> items, Func<T, int> idSelector, int id)
            => items.FirstOrDefault(x => idSelector(x) == id);
    }
}
