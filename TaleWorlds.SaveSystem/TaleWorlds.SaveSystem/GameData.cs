using System;
using System.IO;
using TaleWorlds.Library;

namespace TaleWorlds.SaveSystem;

[Serializable]
public class GameData
{
	public byte[] Header { get; internal set; }

	public byte[] Strings { get; internal set; }

	public byte[][] ObjectData { get; internal set; }

	public byte[][] ContainerData { get; internal set; }

	public int TotalSize
	{
		get
		{
			int num = Header.Length;
			num += Strings.Length;
			for (int i = 0; i < ObjectData.Length; i++)
			{
				num += ObjectData[i].Length;
			}
			for (int j = 0; j < ContainerData.Length; j++)
			{
				num += ContainerData[j].Length;
			}
			return num;
		}
	}

	public GameData(byte[] header, byte[] strings, byte[][] objectData, byte[][] containerData)
	{
		Header = header;
		Strings = strings;
		ObjectData = objectData;
		ContainerData = containerData;
	}

	public GameData()
	{
	}

	public void Inspect()
	{
		Debug.Print($"Header Size: {Header.Length} Strings Size: {Strings.Length} Object Size: {ObjectData.Length} Container Size: {ContainerData.Length}");
		float num = (float)TotalSize / 1048576f;
		Debug.Print($"Total size: {num:##.00} MB");
	}

	public static GameData CreateFrom(byte[] readBytes)
	{
		TaleWorlds.Library.BinaryReader binaryReader = new TaleWorlds.Library.BinaryReader(readBytes);
		int length = binaryReader.ReadInt();
		byte[] header = binaryReader.ReadBytes(length);
		int length2 = binaryReader.ReadInt();
		byte[] strings = binaryReader.ReadBytes(length2);
		int num = binaryReader.ReadInt();
		byte[][] array = new byte[num][];
		for (int i = 0; i < num; i++)
		{
			int length3 = binaryReader.ReadInt();
			byte[] array2 = binaryReader.ReadBytes(length3);
			array[i] = array2;
		}
		int num2 = binaryReader.ReadInt();
		byte[][] array3 = new byte[num2][];
		for (int j = 0; j < num2; j++)
		{
			int length4 = binaryReader.ReadInt();
			byte[] array4 = binaryReader.ReadBytes(length4);
			array3[j] = array4;
		}
		return new GameData(header, strings, array, array3);
	}

	public byte[] GetData()
	{
		TaleWorlds.Library.BinaryWriter binaryWriter = new TaleWorlds.Library.BinaryWriter();
		binaryWriter.WriteInt(Header.Length);
		binaryWriter.WriteBytes(Header);
		binaryWriter.WriteInt(Strings.Length);
		binaryWriter.WriteBytes(Strings);
		binaryWriter.WriteInt(ObjectData.Length);
		byte[][] objectData = ObjectData;
		foreach (byte[] array in objectData)
		{
			binaryWriter.WriteInt(array.Length);
			binaryWriter.WriteBytes(array);
		}
		binaryWriter.WriteInt(ContainerData.Length);
		objectData = ContainerData;
		foreach (byte[] array2 in objectData)
		{
			binaryWriter.WriteInt(array2.Length);
			binaryWriter.WriteBytes(array2);
		}
		return binaryWriter.GetFinalData();
	}

	public static void Write(System.IO.BinaryWriter writer, GameData gameData)
	{
		writer.Write(gameData.Header.Length);
		writer.Write(gameData.Header);
		writer.Write(gameData.ObjectData.Length);
		byte[][] objectData = gameData.ObjectData;
		foreach (byte[] array in objectData)
		{
			writer.Write(array.Length);
			writer.Write(array);
		}
		writer.Write(gameData.ContainerData.Length);
		objectData = gameData.ContainerData;
		foreach (byte[] array2 in objectData)
		{
			writer.Write(array2.Length);
			writer.Write(array2);
		}
		writer.Write(gameData.Strings.Length);
		writer.Write(gameData.Strings);
	}

	public static GameData Read(System.IO.BinaryReader reader)
	{
		int count = reader.ReadInt32();
		byte[] header = reader.ReadBytes(count);
		int num = reader.ReadInt32();
		byte[][] array = new byte[num][];
		for (int i = 0; i < num; i++)
		{
			int count2 = reader.ReadInt32();
			array[i] = reader.ReadBytes(count2);
		}
		int num2 = reader.ReadInt32();
		byte[][] array2 = new byte[num2][];
		for (int j = 0; j < num2; j++)
		{
			int count3 = reader.ReadInt32();
			array2[j] = reader.ReadBytes(count3);
		}
		int count4 = reader.ReadInt32();
		byte[] strings = reader.ReadBytes(count4);
		return new GameData(header, strings, array, array2);
	}

	public bool IsEqualTo(GameData gameData)
	{
		bool num = CompareByteArrays(Header, gameData.Header, "Header");
		bool flag = CompareByteArrays(Strings, gameData.Strings, "Strings");
		bool flag2 = CompareByteArrays(ObjectData, gameData.ObjectData, "ObjectData");
		bool flag3 = CompareByteArrays(ContainerData, gameData.ContainerData, "ContainerData");
		return num && flag && flag2 && flag3;
	}

	private bool CompareByteArrays(byte[] arr1, byte[] arr2, string name)
	{
		if (arr1.Length != arr2.Length)
		{
			Debug.FailedAssert(name + " failed length comparison.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.SaveSystem\\GameData.cs", "CompareByteArrays", 192);
			return false;
		}
		for (int i = 0; i < arr1.Length; i++)
		{
			if (arr1[i] != arr2[i])
			{
				Debug.FailedAssert($"{name} failed byte comparison at index {i}.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.SaveSystem\\GameData.cs", "CompareByteArrays", 200);
				return false;
			}
		}
		return true;
	}

	private bool CompareByteArrays(byte[][] arr1, byte[][] arr2, string name)
	{
		if (arr1.Length != arr2.Length)
		{
			Debug.FailedAssert(name + " failed length comparison.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.SaveSystem\\GameData.cs", "CompareByteArrays", 211);
			return false;
		}
		for (int i = 0; i < arr1.Length; i++)
		{
			if (arr1[i].Length != arr2[i].Length)
			{
				Debug.FailedAssert(name + " failed length comparison.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.SaveSystem\\GameData.cs", "CompareByteArrays", 218);
				return false;
			}
			for (int j = 0; j < arr1[i].Length; j++)
			{
				if (arr1[i][j] != arr2[i][j])
				{
					Debug.FailedAssert($"{name} failed byte comparison at index {i}-{j}.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.SaveSystem\\GameData.cs", "CompareByteArrays", 226);
				}
			}
		}
		return true;
	}
}
