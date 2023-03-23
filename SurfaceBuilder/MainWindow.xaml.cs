using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO.Packaging;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Resources;
using System.Windows.Shapes;
using System.Xml.XPath;

namespace SurfaceBuilder
{
	/// <summary>
	/// MainWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class MainWindow : Window
	{
		public string loadFilePath;

		internal new MainViewModel DataContext
		{
			get => (MainViewModel)base.DataContext;
			set => base.DataContext = value;
		}

		public MainWindow()
		{
			InitializeComponent();
			DataContext = new MainViewModel(this);
		}

		private void MenuItem_Click(object sender, RoutedEventArgs e)
		{
			var dialog = new SaveFileDialog();
			dialog.Filter = "surface project|*.surfaceproj";
			dialog.InitialDirectory = Environment.CurrentDirectory;
			if (dialog.ShowDialog() == true)
			{
				JsonUtility.SerializeToFile(dialog.FileName, DataContext.Serialize());
			}
		}

		private void MenuItem_Click_1(object sender, RoutedEventArgs e)
		{
			var dialog = new OpenFileDialog();
			dialog.Filter = "surface project|*.surfaceproj";
			dialog.InitialDirectory = Environment.CurrentDirectory;
			if(dialog.ShowDialog() == true)
			{
				LoadFile(dialog.FileName);
			}
		}

		private void MenuItem_Click_2(object sender, RoutedEventArgs e)
		{
			if(string.IsNullOrEmpty(loadFilePath))
			{
				MenuItem_Click(sender, e);
				return;
			}

			JsonUtility.SerializeToFile(loadFilePath, DataContext.Serialize());
		}

		public void LoadFile(string fullPath)
		{
			try
			{
				var data = JsonUtility.DeserializeFromFile<JObject>(fullPath);
				DataContext = new MainViewModel(this, data);
				loadFilePath = fullPath;
			}
			catch
			{
			}

		}
	}

	internal class MainViewModel : NotificationObject
	{
		private int gridViewMaxOffsetX;
		private int gridViewMaxOffsetY;
		private int gridViewMaxWidth;
		private int gridViewMaxHeight;
		private string gridViewMaxOffsetXString;
		private string gridViewMaxOffsetYString;
		private string gridViewMaxWidthString;
		private string gridViewMaxHeightString;

		private int gridViewCurrentOffsetX;
		private int gridViewCurrentOffsetY;
		private int gridViewCurrentWidth;
		private int gridViewCurrentHeight;

		private GridListView gridViewWindow;
		private CancellationTokenSource makeGridCanceller;
		private Task makeGridTask;
		private ObservableCollection<CategoryViewModel> items;
		private ObservableCollection<GridListItemViewModel> gridItems;

		//アイテムリスト
		public ReadOnlyObservableCollection<CategoryViewModel> Items => new ReadOnlyObservableCollection<CategoryViewModel>(items);

		//網羅表示アイテムリスト
		public ReadOnlyObservableCollection<GridListItemViewModel> GridItems => new ReadOnlyObservableCollection<GridListItemViewModel>(gridItems);

		public string SurfacesString
		{
			get
			{
				var list = new List<string>();
				list.Add("surface0");
				list.Add("{");

				var itemCount = 0;
				foreach (var item in items)
				{
					if (item.SelectedItem != null)
					{
						list.Add(string.Format("element{0},overlay,{1},{2},{3}", itemCount, item.SelectedItem.FileName, item.SelectedItem.OffsetX, item.SelectedItem.OffsetY));
					}
					itemCount++;
				}
				list.Add("}");

				return string.Join("\r\n", list);
			}
		}

		public MainWindow MainWindow { get; set; }
		public ActionCommand DragEnterCommand { get; }
		public ActionCommand DropCommand { get; }
		public ActionCommand ShowGridWindowCommand { get; }

		public int GridViewMaxOffsetX
		{
			get => gridViewMaxOffsetX;
			set
			{
				gridViewMaxOffsetX = value;
				NotifyChanged();
			}
		}

		public int GridViewMaxOffsetY
		{
			get => gridViewMaxOffsetY;
			set
			{
				gridViewMaxOffsetY = value;
				NotifyChanged();
			}
		}

		public int GridViewMaxWidth
		{
			get => gridViewMaxWidth;
			set
			{
				gridViewMaxWidth = value;
				NotifyChanged();
			}
		}

		public int GridViewMaxHeight
		{
			get => gridViewMaxHeight;
			set
			{
				gridViewMaxHeight = value;
				NotifyChanged();
			}
		}

		public string GridViewMaxOffsetXString
		{
			get => gridViewMaxOffsetXString;
			set
			{
				gridViewMaxOffsetXString = value;
				NotifyChanged();
				int val;
				if (int.TryParse(value, out val))
					GridViewMaxOffsetX = val;
			}
		}

		public string GridViewMaxOffsetYString
		{
			get => gridViewMaxOffsetYString;
			set
			{
				gridViewMaxOffsetYString = value;
				NotifyChanged();
				int val;
				if (int.TryParse(value, out val))
					GridViewMaxOffsetY = val;
			}
		}

		public string GridViewMaxWidthString
		{
			get => gridViewMaxWidthString;
			set
			{
				gridViewMaxWidthString = value;
				NotifyChanged();
				int val;
				if (int.TryParse(value, out val))
					GridViewMaxWidth = val;
			}
		}

		public string GridViewMaxHeightString
		{
			get => gridViewMaxHeightString;
			set
			{
				gridViewMaxHeightString = value;
				NotifyChanged();
				int val;
				if (int.TryParse(value, out val))
					GridViewMaxHeight = val;
			}
		}

		public int GridViewCurrentOffsetX
		{
			get => gridViewCurrentOffsetX;
			set
			{
				gridViewCurrentOffsetX = value;
				NotifyChanged();
			}
		}

		public int GridViewCurrentOffsetY
		{
			get => gridViewCurrentOffsetY;
			set
			{
				gridViewCurrentOffsetY = value;
				NotifyChanged();
			}
		}

		public int GridViewCurrentWidth
		{
			get => gridViewCurrentWidth;
			set
			{
				gridViewCurrentWidth = value;
				NotifyChanged();
			}
		}

		public int GridViewCurrentHeight
		{
			get => gridViewCurrentHeight;
			set
			{
				gridViewCurrentHeight = value;
				NotifyChanged();
			}
		}

		public MainViewModel(MainWindow mainWindow, JObject data) : this(mainWindow)
		{
			var itemsArray = data[nameof(Items)]?.ToObject<JArray>() ?? new JArray();
			foreach (JObject jobj in itemsArray)
			{
				items.Add(new CategoryViewModel(this, jobj));
			}

			gridViewMaxOffsetX = data[nameof(GridViewMaxOffsetX)]?.Value<int>() ?? gridViewMaxOffsetX;
			gridViewMaxOffsetY = data[nameof(GridViewMaxOffsetY)]?.Value<int>() ?? gridViewMaxOffsetY;
			gridViewMaxWidth = data[nameof(GridViewMaxWidth)]?.Value<int>() ?? gridViewMaxWidth;
			gridViewMaxHeight = data[nameof(GridViewMaxHeight)]?.Value<int>() ?? gridViewMaxHeight;
			gridViewCurrentOffsetX = data[nameof(GridViewCurrentOffsetX)]?.Value<int>() ?? gridViewCurrentOffsetX;
			gridViewCurrentOffsetY = data[nameof(GridViewCurrentOffsetY)]?.Value<int>() ?? gridViewCurrentOffsetY;
			gridViewCurrentWidth = data[nameof(GridViewCurrentWidth)]?.Value<int>() ?? gridViewCurrentWidth;
			gridViewCurrentHeight = data[nameof(GridViewCurrentHeight)]?.Value<int>() ?? gridViewCurrentHeight;
		}

		public JObject Serialize()
		{
			var result = new JObject();
			var itemsArray = new JArray(items.Select(o => o.Serialize()).ToArray());

			result[nameof(Items)] = itemsArray;

			return result;
		}

		public MainViewModel(MainWindow mainWindow)
		{
			MainWindow = mainWindow;
			items = new ObservableCollection<CategoryViewModel>();
			gridItems = new ObservableCollection<GridListItemViewModel>();

			gridViewMaxOffsetX = 1000;
			gridViewMaxOffsetY = 1000;
			gridViewMaxWidth = 1000;
			gridViewMaxHeight = 1000;
			gridViewCurrentHeight = 200;
			gridViewCurrentWidth = 200;

			gridViewMaxOffsetXString = gridViewMaxOffsetX.ToString();
			gridViewMaxOffsetYString = gridViewMaxOffsetY.ToString();
			gridViewMaxHeightString = gridViewMaxHeight.ToString();
			gridViewMaxWidthString = gridViewMaxWidth.ToString();

			DragEnterCommand = new ActionCommand(
				o =>
				{
					if(o is DragEventArgs e)
					{
						if (e.Data.GetDataPresent(DataFormats.FileDrop))
						{
							e.Effects = DragDropEffects.Copy;
						}
					}
				});

			DropCommand = new ActionCommand(
				o =>
				{
					if(o is DragEventArgs e)
					{
						if (e.Data.GetDataPresent(DataFormats.FileDrop))
						{
							e.Handled = true;
							var files = (string[])e.Data.GetData(DataFormats.FileDrop);

							//カテゴリを新設して出力してみる
							var cat = AddCategory();
							foreach(var f in files)
							{
								if(System.IO.Path.GetExtension(f) == ".surfaceproj")
								{
									MainWindow.LoadFile(f);
									return;	//打ち切り
								}
								cat.AddElement(f);
							}
							
						}
					}
				});

			ShowGridWindowCommand = new ActionCommand(
				o =>
				{
					if (gridViewWindow == null)
					{
						MakeGridViewItemList();
						gridViewWindow = new GridListView();
						gridViewWindow.DataContext = this;
						gridViewWindow.Owner = MainWindow;
						gridViewWindow.Closed += (s,e) => { gridViewWindow = null; };
						gridViewWindow.Show();
					}
					else
					{
						gridViewWindow.Focus();
					}
				});
		}

		//カテゴリ追加
		public CategoryViewModel AddCategory()
		{
			var cat = new CategoryViewModel(this);
			items.Add(cat);
			return cat;
		}

		//カテゴリの移動
		public void MoveCategory(int targetIndex, CategoryViewModel item)
		{
			items.Remove(item);
			items.Insert(targetIndex, item);
		}

		//カテゴリの削除
		public void RemoveCategory(CategoryViewModel item)
		{
			items.Remove(item);
		}

		//surfaces.txt変更通知
		public void NotifyUpdateSurfacesString()
		{
			NotifyChanged(nameof(SurfacesString));
		}

		//グリッドデータ作成
		public void MakeGridViewItemList()
		{
			if(makeGridTask != null)
			{
				makeGridCanceller.Cancel();
				makeGridTask.Wait();
			}

			gridItems.Clear();
			makeGridCanceller = new CancellationTokenSource();

			//順番変更は問題なので先に配列をフリーズ
			var categoryList = Items.ToArray();
			var elementList = Items.Select(o => o.Items.ToArray()).ToArray();

			makeGridTask = Task.Run(() =>
			{
				//インデックスの全組み合わせを作成
				var indexList = new int[categoryList.Length];

				while (true)
				{
					if (makeGridCanceller.Token.IsCancellationRequested)
						return;

					//出力
					var images = new ElementViewModel[categoryList.Length];
					bool isSkip = false;
					for (var i = 0; i < categoryList.Length; i++)
					{
						var category = categoryList[i];
						var element = elementList[i][indexList[i]];
						if (!element.IsShowGrid)
							isSkip = true;
						images[i] = element;
					}

					if (!isSkip)
					{
						Application.Current.Dispatcher.Invoke(() =>
							gridItems.Add(new GridListItemViewModel(images)), System.Windows.Threading.DispatcherPriority.ContextIdle
						);
					}

					//インクリメント(差分作成上、末尾から)
					bool isContinue = false;
					for (var i = categoryList.Length - 1; i >= 0; i--)
					{
						if (indexList[i] < elementList[i].Length - 1)
						{
							indexList[i]++;
							isContinue = true;
							break;
						}
						else
						{
							indexList[i] = 0;
						}
					}

					//終了
					if (!isContinue)
						break;
				}
			}
			);
		}

		//グリッドの表示指定が変更→作り直す
		public void NotifyShowGridChanged()
		{
			//ウインドウがあるときだけ
			if(gridViewWindow != null)
			{
				MakeGridViewItemList();
			}
		}

		//GirdViewのアイテムから選択をリクエスト
		public void RequestSelect(GridListItemViewModel item)
		{
			foreach(var element in item.Items)
			{
				//所属カテゴリを検索
				var category = Items.FirstOrDefault(o => o.Items.Contains(element));

				//選択をリクエスト
				if(category != null)
					category.RequestSelect(element, true);
			}
		}
	}

	//カテゴリ情報
	internal class CategoryViewModel : NotificationObject
	{
		private ObservableCollection<ElementViewModel> items;
		private ElementViewModel selectedItem;
		private int offsetX;
		private int offsetY;
		private string offsetString;

		public MainViewModel Main { get; set; }

		//エレメントのリスト
		public ReadOnlyObservableCollection<ElementViewModel> Items => new ReadOnlyObservableCollection<ElementViewModel>(items);

		//選択中のアイテム
		public ElementViewModel SelectedItem
		{
			get => selectedItem;
			set
			{
				selectedItem = value;
				NotifyChanged();
				Main.NotifyUpdateSurfacesString();

				//選択状態の更新
				foreach (var i in items)
				{
					i.NotifySelectionUpdate();
				}
			}
		}

		//オフセットX
		public int OffsetX
		{
			get => offsetX;
			set
			{
				offsetX = value;
				NotifyChanged();
			}
		}

		//オフセットY
		public int OffsetY
		{
			get => offsetY;
			set
			{
				offsetY = value;
				NotifyChanged();
			}
		}

		//数値オフセット
		public string OffsetString
		{
			get => offsetString;
			set
			{
				offsetString = value;
				NotifyChanged();

				//解析、オフセットに適用
				var sp = value.Split(new char[] { ',' }, 2);
				bool isValid = false;
				if (sp.Length == 2)
				{
					int ox, oy;
					if (int.TryParse(sp[0], out ox) && int.TryParse(sp[1], out oy))
					{
						offsetX = ox;
						offsetY = oy;
						isValid = true;
					}
				}

				if (!isValid)
				{
					offsetX = 0;
					offsetY = 0;
				}

				SelectedItem?.NotifyOffsetUpdate();
			}
		}

		public ActionCommand RemoveCommand { get; }
		public ActionCommand MoveUpCommand { get; }
		public ActionCommand MoveDownCommand { get; }
		public ActionCommand DragEnterCommand { get; }
		public ActionCommand DropCommand { get; }

		public CategoryViewModel(MainViewModel main, JObject data):this(main)
		{
			var itemsArray = data[nameof(Items)]?.ToObject<JArray>() ?? new JArray();
			foreach(JObject jobj in itemsArray)
			{
				items.Add(new ElementViewModel(this, jobj));
			}

			OffsetString = data[nameof(OffsetString)]?.ToString() ?? string.Empty;
			var selectedItemIndex = data[nameof(SelectedItem)]?.Value<int>() ?? 0;

			if (selectedItemIndex >= 0)
			{
				Items[selectedItemIndex].IsSelected = true;
			}
		}

		public JObject Serialize()
		{
			var result = new JObject();
			var itemsArray = new JArray(items.Select(o => o.Serialize()).ToArray());
			var selectedItemIndex = Items.IndexOf(SelectedItem);

			result[nameof(Items)] = itemsArray;
			result[nameof(OffsetString)] = offsetString;
			result[nameof(SelectedItem)] = selectedItemIndex;
			return result;
		}

		public CategoryViewModel(MainViewModel main )
		{
			items = new ObservableCollection<ElementViewModel>();
			Main = main;

			RemoveCommand = new ActionCommand(
				o =>
				{
					Main.RemoveCategory(this);
				});

			MoveUpCommand = new ActionCommand(
				o =>
				{
					if (Main.Items.IndexOf(this) > 0)
					{
						Main.MoveCategory(Main.Items.IndexOf(this) - 1, this);
					}
				});

			MoveDownCommand = new ActionCommand(
				o =>
				{
					if(Main.Items.IndexOf(this) < Main.Items.Count-1)
					{
						Main.MoveCategory(Main.Items.IndexOf(this) + 1, this);
					}
				});

			DragEnterCommand = new ActionCommand(
				o =>
				{
					if (o is DragEventArgs e)
					{
						if (e.Data.GetDataPresent(DataFormats.FileDrop))
						{
							e.Effects = DragDropEffects.Copy;
						}
					}
				});

			DropCommand = new ActionCommand(
				o =>
				{
					if (o is DragEventArgs e)
					{
						if (e.Data.GetDataPresent(DataFormats.FileDrop))
						{
							e.Handled = true;
							var files = (string[])e.Data.GetData(DataFormats.FileDrop);

							//自カテゴリに内容を追加
							foreach (var f in files)
							{
								if (System.IO.Path.GetExtension(f) == ".surfaceproj")
								{
									Main.MainWindow.LoadFile(f);
									return; //打ち切り
								}
								AddElement(f);
							}
						}
					}
				});
		}

		public ElementViewModel AddElement(string fullPath)
		{
			var elem = new ElementViewModel(this, fullPath);
			items.Add(elem);
			//初期選択
			if (SelectedItem == null)
				RequestSelect(elem, true);
			return elem;
		}

		public void RequestSelect(ElementViewModel item, bool isSelect)
		{
			if(isSelect)
			{
				//選択を変更
				SelectedItem = item;

				//選択の場合、他の選択を解除するので通知
				foreach(var i in items)
				{
					i.NotifySelectionUpdate();
				}
			}
			else if(SelectedItem == item)
			{
				SelectedItem = null;
				item.NotifySelectionUpdate();
			}
		}

		//エレメントの移動
		public void MoveElement(int targetIndex, ElementViewModel item)
		{
			items.Remove(item);
			items.Insert(targetIndex, item);
		}

		//カテゴリの削除
		public void RemoveElement(ElementViewModel item)
		{
			items.Remove(item);
		}
	}

	internal class ElementViewModel : NotificationObject
	{
		private string fullPath;
		private int? offsetX;
		private int? offsetY;
		private string label;
		private string offsetString;
		private bool isShowGrid;

		public MainViewModel Main => Category.Main;
		public CategoryViewModel Category { get; set; }

		//画像ファイルパス
		public string FullPath
		{
			get => fullPath;
			set
			{
				fullPath = value;
				NotifyChanged();
			}
		}

		//ファイル名のみ
		public string FileName
		{
			get => System.IO.Path.GetFileName(FullPath);
		}

		//ラベル(表示名)
		public string Label
		{
			get => label;
			set
			{
				label = value;
				NotifyChanged();
				NotifyChanged(nameof(Header));
			}
		}

		//表示用のラベル
		public string Header
		{
			get
			{
				return string.IsNullOrEmpty(label) ? FileName : Label;
			}
		}

		//数値オフセット
		public string OffsetString
		{
			get => offsetString;
			set
			{
				offsetString = value;
				NotifyChanged();

				//解析、オフセットに適用
				var sp = value.Split(new char[] {','}, 2);
				bool isValid = false;
				if(sp.Length == 2)
				{
					int ox, oy;
					if (int.TryParse(sp[0], out ox) && int.TryParse(sp[1], out oy))
					{
						offsetX = ox;
						offsetY = oy;
						isValid = true;
					}
				}

				if(!isValid)
				{
					offsetX = null;
					offsetY = null;
				}

				NotifyChanged(nameof(OffsetX));
				NotifyChanged(nameof(OffsetY));
			}
		}

		//グリッドビューに表示するか
		public bool IsShowGrid
		{
			get => isShowGrid;
			set
			{
				isShowGrid = value;
				NotifyChanged();
				Main.NotifyShowGridChanged();
			}
		}

		/*
		public ImageSource Source
		{
			get
			{
				var imageSourceConv = new ImageSourceConverter();
				var source = (ImageSource)imageSourceConv.ConvertFrom(FullPath);
				return source;
			}
		}
		*/

		//オフセットX
		public int OffsetX
		{
			get
			{
				return offsetX ?? Category.OffsetX;
			}
		}

		//オフセットY
		public int OffsetY
		{
			get
			{
				return offsetY ?? Category.OffsetY;
			}
		}

		//選択中かどうか
		public bool IsSelected
		{
			get => Category.SelectedItem == this;
			set
			{
				Category.RequestSelect(this, value);
			}
		}

		public ActionCommand RemoveCommand { get; }
		public ActionCommand MoveUpCommand { get; }
		public ActionCommand MoveDownCommand { get; }

		public ElementViewModel(CategoryViewModel category, string fullPath)
		{
			Category = category;
			FullPath = fullPath;
			isShowGrid = true;

			RemoveCommand = new ActionCommand(
				o =>
				{
					Category.RemoveElement(this);
				});

			MoveUpCommand = new ActionCommand(
				o =>
				{
					if (Category.Items.IndexOf(this) > 0)
					{
						Category.MoveElement(Category.Items.IndexOf(this) - 1, this);
					}
				});

			MoveDownCommand = new ActionCommand(
				o =>
				{
					if (Category.Items.IndexOf(this) < Category.Items.Count - 1)
					{
						Category.MoveElement(Category.Items.IndexOf(this) + 1, this);
					}
				});
		}

		public ElementViewModel(CategoryViewModel category, JObject data)
		{
			Category = category;
			isShowGrid = true;

			//復元
			FullPath = data[nameof(FullPath)].ToString();	//required
			Label = data[nameof(Label)].ToString() ?? string.Empty;
			OffsetString = data[nameof(OffsetString)]?.ToString() ?? string.Empty;
		}

		//シリアライズ
		public JObject Serialize()
		{
			var result = new JObject();
			result[nameof(FullPath)] = FullPath;
			result[nameof(Label)] = Label;
			result[nameof(OffsetString)] = OffsetString;
			return result;
		}

		//選択状態の変更を外部から通知
		public void NotifySelectionUpdate()
		{
			NotifyChanged(nameof(IsSelected));
		}

		//オフセット変更を外部から通知
		public void NotifyOffsetUpdate()
		{
			NotifyChanged(nameof(OffsetX));
			NotifyChanged(nameof(OffsetY));
		}
	}
}
