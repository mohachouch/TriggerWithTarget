using System;
using System.Collections.Generic;
using System.Reflection;
using Xamarin.Forms.Xaml;
using Xamarin.Forms.Internals;
using Xamarin.Forms;

namespace TriggerWithTarget.Interactivity
{
	[ContentProperty("Setters")]
	public sealed class Trigger : TriggerBase
	{
		public Trigger() 
			: base(new PropertyCondition())
		{
		}

		string propertyName;

		public string PropertyName
		{
			get { return ((PropertyCondition)Condition).Property.PropertyName ; }
			set
			{
				if (((PropertyCondition)Condition).Property?.PropertyName == value)
					return;

				propertyName = value;

				if (IsSealed)
					throw new InvalidOperationException("Can not change Property once the Trigger has been applied.");
				OnPropertyChanging();
				if(BindableObject != null)
					((PropertyCondition)Condition).Property = ConvertFrom(BindableObject.GetType(), value);
				OnPropertyChanged();
			}
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

		public new IList<Setter> Setters
		{
			get { return base.Setters; }
		}

		public object Value
		{
			get { return ((PropertyCondition)Condition).Value; }
			set
			{
				if (((PropertyCondition)Condition).Value == value)
					return;
				if (IsSealed)
					throw new InvalidOperationException("Can not change Value once the Trigger has been applied.");
				OnPropertyChanging();
				((PropertyCondition)Condition).Value = value;
				OnPropertyChanged();
			}
		}
		
		public override void Initialize()
		{
			PropertyName = propertyName;
		}
	}
}
