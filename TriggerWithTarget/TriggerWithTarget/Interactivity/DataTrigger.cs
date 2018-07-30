using System;
using System.Collections.Generic;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TriggerWithTarget.Interactivity
{
	[ContentProperty("Setters")]
	public sealed class DataTrigger : TriggerBase
	{
		public DataTrigger() : 
			base(new BindingCondition())
		{
		}

		public BindingBase Binding
		{
			get { return ((BindingCondition)Condition).Binding; }
			set
			{
				if (((BindingCondition)Condition).Binding == value)
					return;
				if (IsSealed)
					throw new InvalidOperationException("Can not change Binding once the Trigger has been applied.");
				OnPropertyChanging();
				((BindingCondition)Condition).Binding = value;
				OnPropertyChanged();
			}
		}

		public new IList<Setter> Setters
		{
			get { return base.Setters; }
		}

		public object Value
		{
			get { return ((BindingCondition)Condition).Value; }
			set
			{
				if (((BindingCondition)Condition).Value == value)
					return;
				if (IsSealed)
					throw new InvalidOperationException("Can not change Value once the Trigger has been applied.");
				OnPropertyChanging();
				((BindingCondition)Condition).Value = value;
				OnPropertyChanged();
			}
		}
	}
}
