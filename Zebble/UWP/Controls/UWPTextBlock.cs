namespace Zebble.UWP
{
	using System;
	using System.Threading.Tasks;
	using Windows.UI.Text;
	using Windows.UI.Xaml;
	using Windows.UI.Xaml.Media;
	using controls = Windows.UI.Xaml.Controls;
	using xaml = Windows.UI.Xaml;

	public class UWPTextBlock : controls.Grid, UIChangeCommand.IHandler, INativeRenderer
	{
		TextView View;

		protected controls.TextBlock Result;

		public Task<FrameworkElement> Render(Renderer renderer) => Task.FromResult((FrameworkElement)this);

		public UWPTextBlock(TextView view)
		{
			View = view;
			Background = Colors.Transparent.RenderBrush(); // Without setting the background, the events won't fire

			Result = new controls.TextBlock();
			Configure();

			Children.Add(Result);
			HandleEvents();
		}

		void Configure()
		{
			Result.TextWrapping = View.Wrap == false ? xaml.TextWrapping.NoWrap : xaml.TextWrapping.Wrap;
			Result.Text = View.TransformedText;
			Result.Foreground = View.TextColor.RenderBrush();

			PaddingChanged();
			SetAlignments();
			FontChanged();
			ViewLineHeightChanged();
		}

		void PaddingChanged()
		{
			Result.Padding = View.Padding.RenderThickness();
			Result.Margin = new Thickness(0, -View.Font.GetUnwantedExtraTopPadding(), 0, 0);
		}

		void SetAlignments()
		{
			Result.TextAlignment = View.TextAlignment.RenderTextAlignment();
			Result.VerticalAlignment = View.TextAlignment.RenderVerticalAlignment();
			HorizontalAlignment = View.TextAlignment.RenderHorizontalAlignment();
			VerticalAlignment = View.TextAlignment.RenderVerticalAlignment();
		}

		void HandleEvents()
		{
			View.TextChanged.HandleOnUI(ViewTextChanged);
			View.TextAlignmentChanged.HandleOnUI(SetAlignments);
			View.FontChanged.HandleOnUI(FontChanged);
			View.PaddingChanged.HandleOnUI(PaddingChanged);
			View.LineHeightChanged.HandleOnUI(ViewLineHeightChanged);
		}

		public void Apply(string property, UIChangedEventArgs change)
		{
			switch (property)
			{
				case "TextColor":
					ColorChanged((TextColorChangedEventArgs)change);
					break;
			}
		}

		void ViewLineHeightChanged()
		{
			if (View.LineHeight.HasValue)
				Result.LineHeight = View.LineHeight.Value;
		}

		void ViewTextChanged()
		{
			Result.Text = View.TransformedText;
			ViewLineHeightChanged();
		}

		void ColorChanged(TextColorChangedEventArgs args)
		{
			if (args.Animated())
			{
				Color oldColor()
				{
					return (Result.Foreground as SolidColorBrush)
					.Get(x => new Color(x.Color.R, x.Color.G, x.Color.B)) ?? Colors.Transparent;
				}

				Result.Animate(args.Animation, "(TextBlock.Foreground).(SolidColorBrush.Color)", oldColor, () => args.Value);
			}
			else
			{
				Result.Foreground = args.Value.RenderBrush();
			}
		}

		void FontChanged()
		{
			if (View.Font is null)
				throw new Exception("Font is not supported for " + View.GetType().FullName);

			Result.RenderFont(View.Font);

			ViewLineHeightChanged();
		}

		public void Dispose() { }
	}
}