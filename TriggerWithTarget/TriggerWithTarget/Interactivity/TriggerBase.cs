using System;
using System.Collections;
using System.Collections.Generic;
using Xamarin.Forms;

namespace TriggerWithTarget.Interactivity
{
	public abstract class TriggerBase : BindableObject, IAttachedObject
	{
		bool _isSealed;

		internal TriggerBase()
		{
			EnterActions = new SealedList<TriggerAction>();
			ExitActions = new SealedList<TriggerAction>();
		}

		internal TriggerBase(Condition condition) : this()
		{
			Setters = new SealedList<Setter>();
			Condition = condition;
			Condition.ConditionChanged = OnConditionChanged;
		}

		public IList<TriggerAction> EnterActions { get; }

		public IList<TriggerAction> ExitActions { get; }

		public BindableObject BindableObject { get; private set; }

		public bool IsSealed
		{
			get { return _isSealed; }
			private set
			{
				if (_isSealed == value)
					return;
				if (!value)
					throw new InvalidOperationException("What is sealed can not be unsealed.");
				_isSealed = value;
				OnSeal();
			}
		}
		
		internal Condition Condition { get; }

		//Setters and Condition are used by Trigger, DataTrigger and MultiTrigger
		internal IList<Setter> Setters { get; }

		void IAttachedObject.AttachTo(BindableObject bindable)
		{


			BindableObject = bindable;

			Initialize();

			IsSealed = true;

			if (bindable == null)
				throw new ArgumentNullException("bindable");

			OnAttachedTo(bindable);
		}

		void IAttachedObject.DetachFrom(BindableObject bindable)
		{
			BindableObject = null;

			if (bindable == null)
				throw new ArgumentNullException("bindable");
			OnDetachingFrom(bindable);
		}

		internal virtual void OnAttachedTo(BindableObject bindable)
		{
			if (Condition != null)
				Condition.SetUp(bindable);
		}

		internal virtual void OnDetachingFrom(BindableObject bindable)
		{
			if (Condition != null)
				Condition.TearDown(bindable);

			BindableObject = null;
		}

		public virtual void Initialize()
		{
			
		}

		internal virtual void OnSeal()
		{
			((SealedList<TriggerAction>)EnterActions).IsReadOnly = true;
			((SealedList<TriggerAction>)ExitActions).IsReadOnly = true;
			if (Setters != null)
				((SealedList<Setter>)Setters).IsReadOnly = true;
			if (Condition != null)
				Condition.IsSealed = true;
		}

		void OnConditionChanged(BindableObject bindable, bool oldValue, bool newValue)
		{
			if (newValue)
			{
				foreach (TriggerAction action in EnterActions)
					action.DoInvoke(bindable);
				foreach (Setter setter in Setters)
					setter.Apply(bindable);
			}
			else
			{
				foreach (Setter setter in Setters)
					setter.UnApply(bindable);
				foreach (TriggerAction action in ExitActions)
					action.DoInvoke(bindable);
			}
		}

		internal class SealedList<T> : IList<T>
		{
			readonly IList<T> _actual;

			bool _isReadOnly;

			public SealedList()
			{
				_actual = new List<T>();
			}

			public void Add(T item)
			{
				if (IsReadOnly)
					throw new InvalidOperationException("This list is ReadOnly");
				_actual.Add(item);
			}

			public void Clear()
			{
				if (IsReadOnly)
					throw new InvalidOperationException("This list is ReadOnly");
				_actual.Clear();
			}

			public bool Contains(T item)
			{
				return _actual.Contains(item);
			}

			public void CopyTo(T[] array, int arrayIndex)
			{
				_actual.CopyTo(array, arrayIndex);
			}

			public int Count
			{
				get { return _actual.Count; }
			}

			public bool IsReadOnly
			{
				get { return _isReadOnly; }
				set
				{
					if (_isReadOnly == value)
						return;
					if (!value)
						throw new InvalidOperationException("Can't change this back to non readonly");
					_isReadOnly = value;
				}
			}

			public bool Remove(T item)
			{
				if (IsReadOnly)
					throw new InvalidOperationException("This list is ReadOnly");
				return _actual.Remove(item);
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return ((IEnumerable)_actual).GetEnumerator();
			}

			public IEnumerator<T> GetEnumerator()
			{
				return _actual.GetEnumerator();
			}

			public int IndexOf(T item)
			{
				return _actual.IndexOf(item);
			}

			public void Insert(int index, T item)
			{
				if (IsReadOnly)
					throw new InvalidOperationException("This list is ReadOnly");
				_actual.Insert(index, item);
			}

			public T this[int index]
			{
				get { return _actual[index]; }
				set
				{
					if (IsReadOnly)
						throw new InvalidOperationException("This list is ReadOnly");
					_actual[index] = value;
				}
			}

			public void RemoveAt(int index)
			{
				if (IsReadOnly)
					throw new InvalidOperationException("This list is ReadOnly");
				_actual.RemoveAt(index);
			}
		}
	}

	public abstract class TriggerAction
	{
		internal TriggerAction(Type associatedType)
		{
			if (associatedType == null)
				throw new ArgumentNullException("associatedType");
			AssociatedType = associatedType;
		}

		protected Type AssociatedType { get; private set; }

		protected abstract void Invoke(object sender);

		internal virtual void DoInvoke(object sender)
		{
			Invoke(sender);
		}
	}

	public abstract class TriggerAction<T> : TriggerAction where T : BindableObject
	{
		protected TriggerAction() : base(typeof(T))
		{
		}

		protected override void Invoke(object sender)
		{
			Invoke((T)sender);
		}

		protected abstract void Invoke(T sender);
	}
}
