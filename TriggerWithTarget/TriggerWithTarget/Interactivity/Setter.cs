using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using Xamarin.Forms.Xaml;

namespace TriggerWithTarget.Interactivity
{
	[ContentProperty("Value")]
	public sealed class Setter : IValueProvider
	{
		readonly ConditionalWeakTable<BindableObject, object> _originalValues = new ConditionalWeakTable<BindableObject, object>();

		public string PropertyName { get; set; }

		public BindableProperty Property { get; private set; }

		public object Value { get; set; }
		
		public Xamarin.Forms.VisualElement TargetReference { get; set; }
		
		internal void Apply(BindableObject target, bool fromStyle = false)
		{
			if (target == null)
				throw new ArgumentNullException("target");
			if (PropertyName == null)
				return;

			if (TargetReference != null)
			{
				target = TargetReference;
			}
			
			object originalValue = target.GetValue(Property);
			if (!Equals(originalValue, Property.DefaultValue))
			{
				_originalValues.Remove(target);
				_originalValues.Add(target, originalValue);
			}

			var dynamicResource = Value as DynamicResource;
			var binding = Value as BindingBase;
			if (binding != null)
				target.SetBinding(Property, binding.Clone()); //clone
			//else if (dynamicResource != null)
			//target.SetDynamicResource(Property, dynamicResource.Key, fromStyle);
			else
			{
				target.SetValue(Property, Value);
			}
		}

		internal void UnApply(BindableObject target, bool fromStyle = false)
		{
			if (target == null)
				throw new ArgumentNullException(nameof(target));
			if (PropertyName == null)
				return;

			if (TargetReference != null)
			{
				target = TargetReference;
			}
			
			object actual = target.GetValue(Property);

			if (!Equals(actual, Value) &&!(Value is Binding) && !(Value is DynamicResource))
			{
				//Do not reset default value if the value has been changed
				_originalValues.Remove(target);
				return;
			}

			object defaultValue;
			if (_originalValues.TryGetValue(target, out defaultValue))
			{
				//reset default value, unapply bindings and dynamicResource
				target.SetValue(Property, defaultValue);
				_originalValues.Remove(target);
			}
			else
				target.ClearValue(Property);
		}
		
		object IValueProvider.ProvideValue(IServiceProvider serviceProvider)
		{
			Property = ConvertFrom(TargetReference.GetType(), PropertyName);

			if (Property == null)
			{
				var lineInfoProvider = serviceProvider.GetService(typeof(IXmlLineInfoProvider)) as IXmlLineInfoProvider;
				IXmlLineInfo lineInfo = lineInfoProvider != null ? lineInfoProvider.XmlLineInfo : new XmlLineInfo();
				throw new XamlParseException("Property not set", lineInfo);
			}

			Func<MemberInfo> minforetriever =
				() =>
				(MemberInfo)Property.DeclaringType.GetRuntimeProperty(Property.PropertyName) ?? (MemberInfo)Property.DeclaringType.GetRuntimeMethod("Get" + Property.PropertyName, new[] { typeof(BindableObject) });
			
			object value = ConvertTo(Value, Property.ReturnType, minforetriever, serviceProvider);
			Value = value;
			return this;
		}

		BindableProperty ConvertFrom(Type type, string propertyName)
		{
			string name = propertyName + "Property";
			FieldInfo bpinfo = type.GetField(fi => fi.Name == name && fi.IsStatic && fi.IsPublic && fi.FieldType == typeof(BindableProperty));
			if (bpinfo == null)
				throw new Exception($"Can't resolve {name} on {type.Name}");
			var bp = bpinfo.GetValue(null) as BindableProperty;
			var isObsolete = bpinfo.GetCustomAttribute<ObsoleteAttribute>() != null;
			if (bp.PropertyName != propertyName && !isObsolete)
				throw new Exception($"The PropertyName of {type.Name}.{name} is not {propertyName}");
			return bp;
		}


		internal static object ConvertTo(object value, Type toType, Func<MemberInfo> minfoRetriever,
			IServiceProvider serviceProvider)
		{
				Func<object> getConverter = () =>
				{
					MemberInfo memberInfo;

					var converterTypeName = GetTypeConverterTypeName(toType.GetTypeInfo().CustomAttributes);
					if (minfoRetriever != null && (memberInfo = minfoRetriever()) != null)
						converterTypeName = GetTypeConverterTypeName(memberInfo.CustomAttributes) ?? converterTypeName;
					if (converterTypeName == null)
						return null;

					var convertertype = Type.GetType(converterTypeName);
					return Activator.CreateInstance(convertertype);
				};

			return ConvertTo(value, toType, getConverter, serviceProvider);
		}


		internal static object ConvertTo(object value, Type toType, Func<object> getConverter,
			IServiceProvider serviceProvider)
		{
			if (value == null)
				return null;

			var str = value as string;
			if (str != null)
			{
				//If there's a [TypeConverter], use it
				object converter = getConverter?.Invoke();
				var xfTypeConverter = converter as TypeConverter;
				var xfExtendedTypeConverter = xfTypeConverter as IExtendedTypeConverter;
				if (xfExtendedTypeConverter != null)
					return value = xfExtendedTypeConverter.ConvertFromInvariantString(str, serviceProvider);
				if (xfTypeConverter != null)
					return value = xfTypeConverter.ConvertFromInvariantString(str);
				var converterType = converter?.GetType();
				if (converterType != null)
				{
					var convertFromStringInvariant = converterType.GetRuntimeMethod("ConvertFromInvariantString",
						new[] { typeof(string) });
					if (convertFromStringInvariant != null)
						return value = convertFromStringInvariant.Invoke(converter, new object[] { str });
				}
				var ignoreCase = true;//(serviceProvider?.GetService(typeof(IConverterOptions)) as IConverterOptions)?.IgnoreCase ?? false;

				//If the type is nullable, as the value is not null, it's safe to assume we want the built-in conversion
				if (toType.GetTypeInfo().IsGenericType && toType.GetGenericTypeDefinition() == typeof(Nullable<>))
					toType = Nullable.GetUnderlyingType(toType);

				//Obvious Built-in conversions
				if (toType.GetTypeInfo().IsEnum)
					return Enum.Parse(toType, str, ignoreCase);
				if (toType == typeof(SByte))
					return SByte.Parse(str, CultureInfo.InvariantCulture);
				if (toType == typeof(Int16))
					return Int16.Parse(str, CultureInfo.InvariantCulture);
				if (toType == typeof(Int32))
					return Int32.Parse(str, CultureInfo.InvariantCulture);
				if (toType == typeof(Int64))
					return Int64.Parse(str, CultureInfo.InvariantCulture);
				if (toType == typeof(Byte))
					return Byte.Parse(str, CultureInfo.InvariantCulture);
				if (toType == typeof(UInt16))
					return UInt16.Parse(str, CultureInfo.InvariantCulture);
				if (toType == typeof(UInt32))
					return UInt32.Parse(str, CultureInfo.InvariantCulture);
				if (toType == typeof(UInt64))
					return UInt64.Parse(str, CultureInfo.InvariantCulture);
				if (toType == typeof(Single))
					return Single.Parse(str, CultureInfo.InvariantCulture);
				if (toType == typeof(Double))
					return Double.Parse(str, CultureInfo.InvariantCulture);
				if (toType == typeof(Boolean))
					return Boolean.Parse(str);
				if (toType == typeof(TimeSpan))
					return TimeSpan.Parse(str, CultureInfo.InvariantCulture);
				if (toType == typeof(DateTime))
					return DateTime.Parse(str, CultureInfo.InvariantCulture);
				if (toType == typeof(Char))
				{
					char c = '\0';
					Char.TryParse(str, out c);
					return c;
				}
				if (toType == typeof(String) && str.StartsWith("{}", StringComparison.Ordinal))
					return str.Substring(2);
				if (toType == typeof(String))
					return value;
				if (toType == typeof(Decimal))
					return Decimal.Parse(str, CultureInfo.InvariantCulture);
			}

			//if the value is not assignable and there's an implicit conversion, convert
			if (value != null && !toType.IsAssignableFrom(value.GetType()))
			{
				var opImplicit = GetImplicitConversionOperator(value.GetType(), fromType: value.GetType(), toType: toType)
								?? GetImplicitConversionOperator(toType, fromType: value.GetType(), toType: toType);

				if (opImplicit != null)
				{
					value = opImplicit.Invoke(null, new[] { value });
					return value;
				}
			}

		//	var nativeValueConverterService = DependencyService.Get<INativeValueConverterService>();

			//object nativeValue = null;
			//if (nativeValueConverterService != null && nativeValueConverterService.ConvertTo(value, toType, out nativeValue))
				//return nativeValue;

			
			return value;
		}

		static string GetTypeConverterTypeName(IEnumerable<CustomAttributeData> attributes)
		{
			string[] TypeConvertersType = { "Xamarin.Forms.TypeConverterAttribute", "System.ComponentModel.TypeConverterAttribute" };

			var converterAttribute =
				attributes.FirstOrDefault(cad => TypeConvertersType.Contains(cad.AttributeType.FullName));
			if (converterAttribute == null)
				return null;
			if (converterAttribute.ConstructorArguments[0].ArgumentType == typeof(string))
				return (string)converterAttribute.ConstructorArguments[0].Value;
			if (converterAttribute.ConstructorArguments[0].ArgumentType == typeof(Type))
				return ((Type)converterAttribute.ConstructorArguments[0].Value).AssemblyQualifiedName;
			return null;
		}

		internal static MethodInfo GetImplicitConversionOperator(Type onType, Type fromType, Type toType)
		{
#if NETSTANDARD1_0
			var mi = onType.GetRuntimeMethod("op_Implicit", new[] { fromType });
#else
			var bindingFlags = BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy;
			var mi = onType.GetMethod("op_Implicit", bindingFlags, null, new[] { fromType }, null);
#endif
			if (mi == null) return null;
			if (!mi.IsSpecialName) return null;
			if (!mi.IsPublic) return null;
			if (!mi.IsStatic) return null;
			if (!toType.IsAssignableFrom(mi.ReturnType)) return null;

			return mi;
		}
	}


}
