using System.Collections.Generic;
using System.Linq;
using System.Dynamic;
using System.Collections.Concurrent;
using System.Collections;

namespace System
{
	public static class ReflectionExtensions
	{
		public static bool IsNullable(this Type type)
		{
			return type.IsGenericType && (typeof (Nullable<>) == type.GetGenericTypeDefinition());
		}

		public static bool IsConcrete(this Type type)
		{
			return (!type.IsInterface && !type.IsAbstract && !type.IsValueType);
		}

		public static void ThrowIfNull(this object @obj, string argumentName)
		{
			if (@obj == null) throw new ArgumentNullException(argumentName);
		}

	}
}
