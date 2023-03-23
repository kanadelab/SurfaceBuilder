using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SurfaceBuilder
{
	/// <summary>
	/// GridListView.xaml の相互作用ロジック
	/// </summary>
	public partial class GridListView : Window
	{
		public GridListView()
		{
			InitializeComponent();
		}

		private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			//コマンドをバインドできないので仕方なく
			if(DataContext is MainViewModel main && MainList.SelectedItem is GridListItemViewModel item)
			{
				main.RequestSelect(item);
			}
		}
	}

	//グリッドアイテム
	internal class GridListItemViewModel
	{
		private List<ElementViewModel> items;

		public ReadOnlyCollection<ElementViewModel> Items => new ReadOnlyCollection<ElementViewModel>(items);

		public GridListItemViewModel(IEnumerable<ElementViewModel> elementViewModels)
		{
			items = new List<ElementViewModel>();
			items.AddRange(elementViewModels);
		}
	}

	//逆オフセット
	internal class ReverseOffsetConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return -(int)value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	//int4つからrectに変換、サイズ制限付き
	internal class IntToRectConverter : IMultiValueConverter
	{
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			if (values.Length < 6)
				return new RectangleGeometry(Rect.Empty);

			if(
				values[0] is int offsetX &&
				values[1] is int offsetY &&
				values[2] is int width &&
				values[3] is int height &&
				values[4] is int imageWidth &&
				values[5] is int imageHeight
				)
			{
				//制限をかける
				var rect = new Rect(offsetX, offsetY, width, height);

				var resultRect = new Rect();
				resultRect.X = Math.Min(rect.X, imageWidth);
				resultRect.Y = Math.Min(rect.Y, imageHeight);
				resultRect.Width = Math.Min(resultRect.X + rect.Width, imageWidth) - resultRect.X;
				resultRect.Height = Math.Min(resultRect.Y + rect.Height, imageHeight) - resultRect.Y;
				return new RectangleGeometry(resultRect);

			}
			return new RectangleGeometry(Rect.Empty);
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
