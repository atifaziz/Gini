namespace Gini.Tests
{
    using System;
    using System.Collections.Generic;

    static class Extensions
    {
        public static T Read<T>(this IEnumerator<T> enumerator) =>
            enumerator.TryRead(out var item) ? item : throw new InvalidOperationException();

        public static bool TryRead<T>(this IEnumerator<T> enumerator, out T item)
        {
            if (enumerator == null) throw new ArgumentNullException(nameof(enumerator));

            if (!enumerator.MoveNext())
            {
                item = default;
                return false;
            }

            item = enumerator.Current;
            return true;
        }
    }
}
