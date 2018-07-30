using System.Collections.Generic;
using Xamarin.Forms;

namespace TriggerWithTarget.Interactivity
{
    public static class Interactivity
    {
		public static readonly BindableProperty TriggersProperty = BindableProperty.CreateAttached("Triggers",
			typeof(IList<TriggerBase>),
			typeof(Interactivity),
			default(IList<TriggerBase>),
			defaultValueCreator: bindable =>
			{
				var collection = new AttachedCollection<TriggerBase>();
				collection.AttachTo(bindable);
				return collection;
			});
		
		public static IList<TriggerBase> GetTriggers(BindableObject view)
		{
			return (IList<TriggerBase>)view.GetValue(TriggersProperty);
		}
	}
}
