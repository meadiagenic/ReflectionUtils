namespace System
{
	using System.ComponentModel;
	using System.Globalization;

	public static class ConvertUtils
	{
		public static bool CanConvertType(this Type initialType, Type targetType)
		{
			if (targetType.IsNullable())
			{
				targetType = Nullable.GetUnderlyingType(targetType);
			}

			if (targetType == initialType) return true;

			if (typeof(IConvertible).IsAssignableFrom(initialType) && typeof(IConvertible).IsAssignableFrom(targetType))
			{
				return true;
			}

			if (initialType == typeof(DateTime) && targetType == typeof(DateTimeOffset))
				return true;

			if (initialType == typeof(Guid) && (targetType == typeof(Guid) || targetType == typeof(string)))
			{
				return true;
			}

			if (initialType == typeof(Type) && targetType == typeof(string))
			{
				return true;
			}

			TypeConverter toConverter = GetConverter(initialType);

			if (toConverter != null && !IsComponentConverter(toConverter) && toConverter.CanConvertTo(targetType))
			{
				if (toConverter.GetType() != typeof(TypeConverter))
				{
					return true;
				}
			}

			var fromConverter = GetConverter(targetType);
			if (fromConverter != null && !IsComponentConverter(fromConverter) && fromConverter.CanConvertFrom(initialType))
			{
				return true;
			}

			if (initialType == typeof(DBNull))
			{
				if (targetType.IsNullable())
					return true;
			}

			return false;

		}

		public static T Convert<T>(this object initialValue)
		{
			return initialValue.Convert<T>(CultureInfo.CurrentCulture);
		}

		public static T Convert<T>(this object initialValue, CultureInfo culture)
		{
			return (T)initialValue.Convert(culture, typeof(T));
		}

		public static object Convert(this object initialValue, CultureInfo culture, Type targetType)
		{
			initialValue.ThrowIfNull("initialValue");
			if (targetType.IsNullable())
			{
				targetType = Nullable.GetUnderlyingType(targetType);
			}

			Type initialType = initialValue.GetType();

			if (targetType == initialType) return initialValue;

			if (initialValue is string && typeof(Type).IsAssignableFrom(targetType))
			{
				return Type.GetType((string)initialValue, true);
			}

			if (!targetType.IsConcrete())
			{
				throw new ArgumentException(
					string.Format(culture, "Target type {0} is not a value type or a non-abstract class.", targetType), "targetType");
			}

			if (initialValue is IConvertible && typeof(IConvertible).IsAssignableFrom(targetType))
			{
				if (targetType.IsEnum)
				{
					if (initialValue is string)
					{
						return Enum.Parse(targetType, initialValue.ToString(), true);
					}
					if (initialValue.IsInteger())
					{
						return Enum.ToObject(targetType, initialValue);
					}
				}

				return System.Convert.ChangeType(initialValue, targetType, culture);
			}

			if (initialValue is DateTime && targetType == typeof(DateTimeOffset))
			{
				return new DateTimeOffset((DateTime)initialValue);
			}

			if (initialValue is string)
			{
				if (targetType == typeof(Guid))
				{
					return new Guid((string)initialValue);
				}
				if (targetType == typeof(Uri))
				{
					return new Uri((string)initialValue);
				}
				if (targetType == typeof(TimeSpan))
				{
					return TimeSpan.Parse((string)initialValue, CultureInfo.InvariantCulture);
				}
			}
		}

		internal static TypeConverter GetConverter(Type t)
		{
			return TypeDescriptor.GetConverter(t);
		}

		private static bool IsComponentConverter(TypeConverter converter)
		{
			return (converter is ComponentConverter);
		}

		public static bool IsInteger(this object value)
		{
			switch (System.Convert.GetTypeCode(value))
			{
				case TypeCode.Byte:
				case TypeCode.SByte:
				case TypeCode.Int16:
				case TypeCode.Int32:
				case TypeCode.UInt16:
				case TypeCode.Int64:
				case TypeCode.UInt32:
				case TypeCode.UInt64:
					return true;
				default:
					return false;
			}
		}
	}
}
