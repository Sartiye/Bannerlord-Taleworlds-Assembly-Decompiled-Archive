using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TaleWorlds.Diamond;

public class FunctionResultJsonConverter : JsonConverter
{
	private static readonly Dictionary<string, Type> _knownTypes;

	public override bool CanWrite => true;

	static FunctionResultJsonConverter()
	{
		_knownTypes = (from t in (from a in AppDomain.CurrentDomain.GetAssemblies()
				where !a.GlobalAssemblyCache
				select a).SelectMany((Assembly a) => a.GetTypes())
			where t.IsSubclassOf(typeof(FunctionResult))
			select t).ToDictionary((Type item) => item.FullName, (Type item) => item);
	}

	public override bool CanConvert(Type objectType)
	{
		return typeof(FunctionResult).IsAssignableFrom(objectType);
	}

	public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Invalid comparison between Unknown and I4
		if ((int)reader.TokenType == 11)
		{
			return null;
		}
		JObject val = JObject.Load(reader);
		string key = (string)val["_type"];
		if (_knownTypes.TryGetValue(key, out var value))
		{
			FunctionResult functionResult = (FunctionResult)Activator.CreateInstance(value);
			serializer.Populate(((JToken)val).CreateReader(), (object)functionResult);
			return functionResult;
		}
		return null;
	}

	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Expected O, but got Unknown
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Expected O, but got Unknown
		JProperty val = new JProperty("_type", (object)value.GetType().FullName);
		JObject val2 = new JObject();
		((JContainer)val2).Add((object)val);
		PropertyInfo[] properties = value.GetType().GetProperties();
		foreach (PropertyInfo propertyInfo in properties)
		{
			if (propertyInfo.CanRead)
			{
				object value2 = propertyInfo.GetValue(value);
				if (value2 != null)
				{
					val2.Add(propertyInfo.Name, JToken.FromObject(value2, serializer));
				}
			}
		}
		((JToken)val2).WriteTo(writer, Array.Empty<JsonConverter>());
	}
}
