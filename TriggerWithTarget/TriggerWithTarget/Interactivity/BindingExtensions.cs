using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace TriggerWithTarget.Interactivity
{
   public static class BindingExtensions
	{
	   public static BindingBase Clone(this BindingBase bindingBase)
		{
			Binding binding = (Binding)bindingBase;
			return new Binding(binding.Path, binding.Mode)
			{
				Converter = binding.Converter,
				ConverterParameter = binding.ConverterParameter,
				StringFormat = binding.StringFormat,
				Source = binding.Source,
				UpdateSourceEventName = binding.UpdateSourceEventName,
				TargetNullValue = binding.TargetNullValue,
				FallbackValue = binding.FallbackValue,
			};
		}
	}
}
