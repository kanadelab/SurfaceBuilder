using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SurfaceBuilder
{
	public class NotificationObject : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;
		protected void NotifyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}

	public class ActionCommand : ICommand
	{
		public event EventHandler CanExecuteChanged;
		private Action<object> executeAction;
		private Func<object, bool> canExecuteFunc;

		public ActionCommand(Action<object> execute, bool canExecute = true)
		{
			executeAction = execute;
			canExecuteFunc = (o) => canExecute;
		}

		public ActionCommand(Action<object> execute, Func<object, bool> canExecute)
		{
			executeAction = execute;
			canExecuteFunc = canExecute;
		}


		public bool CanExecute(object parameter)
		{
			if (canExecuteFunc != null)
			{
				return canExecuteFunc.Invoke(parameter);
			}
			return true;
		}

		public void Execute(object parameter)
		{
			executeAction?.Invoke(parameter);
		}

		public void NotifyCanExecuteChanged()
		{
			CanExecuteChanged?.Invoke(this, new EventArgs());
		}
	}

	public class JsonUtility
	{
		public static JToken DeserializeFromFile(string path)
		{
			using (var stream = new System.IO.StreamReader(path))
			{
				using (var textReader = new JsonTextReader(stream))
				{
					var jsonSerializer = new JsonSerializer();
					return (JToken)jsonSerializer.Deserialize(textReader);
				}
			}
		}

		public static T DeserializeFromFile<T>(string path)
		{
			using (var stream = new System.IO.StreamReader(path))
			{
				using (var textReader = new JsonTextReader(stream))
				{
					var jsonSerializer = new JsonSerializer();
					jsonSerializer.Formatting = Formatting.Indented;
					return jsonSerializer.Deserialize<T>(textReader);
				}
			}
		}

		public static void SerializeToFile(string path, object obj)
		{
			using (var writer = new System.IO.StreamWriter(path))
			{
				var jsonSerializer = new JsonSerializer();
				jsonSerializer.Formatting = Formatting.Indented;
				jsonSerializer.Serialize(writer, obj);
			}
		}

		public static bool SerializableObjectEquals(object a, object b)
		{
			return JObject.FromObject(a).ToString() == JObject.FromObject(b).ToString();
		}

		public static T CloneObject<T>(T obj) { return JObject.FromObject(obj).ToObject<T>(); }

	}
}
