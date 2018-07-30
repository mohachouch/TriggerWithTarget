using System;
using Xamarin.Forms;

namespace TriggerWithTarget
{
	public partial class MainPage : ContentPage
	{
		
		public MainPage()
		{
			InitializeComponent();
			this.BindingContext = this;
		}

		private void Button_Clicked(object sender, EventArgs e)
		{
			this.IsBusy = !this.IsBusy;
		}
	}
}
