using System;
using System.Reflection;

namespace Application.Helpers
{
    public static class DateTimeHelper
    {
        public static void FixDateTimesToUtc<T>(T obj)
        {
            if (obj == null) return;

            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in props)
            {
                if (prop.PropertyType == typeof(DateTime))
                {
                    if (prop.GetValue(obj) is DateTime dt && dt.Kind == DateTimeKind.Unspecified)
                        prop.SetValue(obj, DateTime.SpecifyKind(dt, DateTimeKind.Utc));
                }
                else if (prop.PropertyType == typeof(DateTime?))
                {
                    if (prop.GetValue(obj) is DateTime dt && dt.Kind == DateTimeKind.Unspecified)
                        prop.SetValue(obj, DateTime.SpecifyKind(dt, DateTimeKind.Utc));
                }

            }
        }
    }
}
