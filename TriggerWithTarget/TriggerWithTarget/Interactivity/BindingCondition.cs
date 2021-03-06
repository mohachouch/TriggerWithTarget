﻿using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TriggerWithTarget.Interactivity
{
	public sealed class BindingCondition : Condition
	{
		readonly BindableProperty _boundProperty;

		BindingBase _binding;
		object _triggerValue;

		public BindingCondition()
		{
			_boundProperty = BindableProperty.CreateAttached("Bound", typeof(object), typeof(BindingCondition), null, propertyChanged: OnBoundPropertyChanged);
		}

		public BindingBase Binding
		{
			get { return _binding; }
			set
			{
				if (_binding == value)
					return;
				if (IsSealed)
					throw new InvalidOperationException("Can not change Binding once the Condition has been applied.");
				_binding = value;
			}
		}

		public object Value
		{
			get { return _triggerValue; }
			set
			{
				if (_triggerValue == value)
					return;
				if (IsSealed)
					throw new InvalidOperationException("Can not change Value once the Condition has been applied.");
				_triggerValue = value;
			}
		}
		
		internal override bool GetState(BindableObject bindable)
		{
			object newValue = bindable.GetValue(_boundProperty);
			return EqualsToValue(newValue);
		}

		internal override void SetUp(BindableObject bindable)
		{
			if (Binding != null)
				bindable.SetBinding(_boundProperty, Binding.Clone());  //CLONE
		}

		internal override void TearDown(BindableObject bindable)
		{
			bindable.RemoveBinding(_boundProperty);
			bindable.ClearValue(_boundProperty);
		}
		
		bool EqualsToValue(object other)
		{
			if ((other == Value) || (other != null && other.Equals(Value)))
				return true;
			
			object converted = null;

			converted = Setter.ConvertTo(Value, other != null ? other.GetType() : typeof(object), null, null);
		
			return (other == converted) || (other != null && other.Equals(converted));
		}

		void OnBoundPropertyChanged(BindableObject bindable, object oldValue, object newValue)
		{
			bool oldState = EqualsToValue(oldValue);
			bool newState = EqualsToValue(newValue);

			if (newState == oldState)
				return;

			if (ConditionChanged != null)
				ConditionChanged(bindable, oldState, newState);
		}
	}
}
