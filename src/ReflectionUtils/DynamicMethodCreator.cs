using System;
using System.Reflection;
using System.Reflection.Emit;

namespace ReflectionUtils
{

	public static class DynamicMethodCreator
	{
		public static Func<object[], object> GetFactory(this ConstructorInfo ctor)
		{
			ctor.ThrowIfNull("ctor");

			var type = ctor.DeclaringType;

			var args = ctor.GetParameters();

			var module = type.Module;

			var dynamicMethod = new DynamicMethod("", typeof(object), new Type[] { typeof(object[]) }, module, true);

			var il = dynamicMethod.GetILGenerator();

			il.CheckArgumentLength(args.Length, true);

			il.LoadArguments(args, true);

			il.Emit(OpCodes.Newobj, ctor);

			if (type.IsValueType)
			{
				il.Emit(OpCodes.Box, type);
			}

			il.Emit(OpCodes.Ret);

			return (Func<object[], object>)dynamicMethod.CreateDelegate(typeof(Func<object[], object>));
		}

		public static Func<object, object> GetGetter(this PropertyInfo property)
		{
			property.ThrowIfNull("property");

			if (!property.CanRead)
			{
				return null;
			}

			var methodInfo = property.GetGetMethod(true);
			if (methodInfo == null) return null;

			var module = methodInfo.DeclaringType.Module;
			var dynamicMethod = new DynamicMethod("", typeof(object), new Type[] { typeof(object) }, module, true);

			var il = dynamicMethod.GetILGenerator();
			if (!methodInfo.IsStatic)
			{
				il.Emit(OpCodes.Ldarg_0);
			}

			il.Call(methodInfo);

			if (property.PropertyType.IsValueType)
			{
				il.Emit(OpCodes.Box, property.PropertyType);
			}
			il.Emit(OpCodes.Ret);

			return (Func<object, object>)dynamicMethod.CreateDelegate(typeof(Func<object, object>));
		}

		public static Action<object, object> GetSetter(this PropertyInfo property)
		{
			property.ThrowIfNull("property");

			if (!property.CanWrite) return null;

			var method = property.GetSetMethod(true);
			if (method == null) return null;

			var module = method.DeclaringType.Module;

			var dynamicMethod = new DynamicMethod("", null, new Type[] { typeof(object), typeof(object) }, module, true);

			ILGenerator il = dynamicMethod.GetILGenerator();

			if (!method.IsStatic) il.Emit(OpCodes.Ldarg_0);

			il.UnboxIfNeeded(property.PropertyType);

			il.Call(method);

			il.Emit(OpCodes.Ret);

			return (Action<object, object>)dynamicMethod.CreateDelegate(typeof(Action<object, object>));
		}

		public static Func<object, object> GetGetter(FieldInfo field)
		{
			return null;
		}

		private static void Call(this ILGenerator gen, MethodInfo method)
		{
			gen.Emit(method.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, method);
		}

		private static void CheckArgumentLength(this ILGenerator gen, int argCount, bool isCtor)
		{
			var jump = gen.DefineLabel();

			gen.Emit(isCtor ? OpCodes.Ldarg_0 : OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Ldlen);
			gen.Emit(OpCodes.Conv_I4);
			gen.Emit(OpCodes.Ldc_I4, argCount);
			gen.Emit(OpCodes.Bge_S, jump);

			gen.Emit(OpCodes.Ldstr, "Missing arguments");
			gen.Emit(OpCodes.Newobj, typeof(TypeLoadException).GetConstructor(new Type[] { typeof(string) }));
			gen.Emit(OpCodes.Throw);

			gen.MarkLabel(jump);
		}

		private static void LoadArguments(this ILGenerator il, ParameterInfo[] args, bool isCtor)
		{
			for (int i = 0; i < args.Length; i++)
			{
				var argType = args[i].ParameterType;

				il.Emit(isCtor ? OpCodes.Ldarg_0 : OpCodes.Ldarg_1);
				il.Emit(OpCodes.Ldc_I4, i);
				il.Emit(OpCodes.Ldelem_Ref);
				il.UnboxIfNeeded(argType);
			}
		}

		private static void UnboxIfNeeded(this ILGenerator gen, Type type)
		{
			if (type.IsValueType)
			{
				gen.Emit(OpCodes.Unbox_Any, type);
			}
			else
			{
				gen.Emit(OpCodes.Castclass, type);
			}
		}


	}
}
