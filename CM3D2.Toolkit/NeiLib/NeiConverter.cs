using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NeiLib
{
	public class NeiConverter
	{
		private static readonly byte[] NEI_KEY =
		{
			0xAA,
			0xC9,
			0xD2,
			0x35,
			0x22,
			0x87,
			0x20,
			0xF2,
			0x40,
			0xC5,
			0x61,
			0x7C,
			0x01,
			0xDF,
			0x66,
			0x54
		};

		private static readonly byte[] NEI_MAGIC =
		{
			0x77,
			0x73,
			0x76,
			0xFF
		};

		private static readonly Encoding ShiftJisEncoding = Encoding.GetEncoding(932);

		private static char ValueSeparator { get; set; }

		private static List<List<string>> ParseCSV(Stream stream)
		{
			var buffer = new StringBuilder();
			var whitespaceBuffer = new StringBuilder();
			var isQuoted = false;
			var quoteLevel = 0;
			var result = new List<List<string>>();
			var line = new List<string>();

			var tr = new StreamReader(stream);

			int nextChar;
			while ((nextChar = tr.Read()) != -1)
			{
				var c = (char)nextChar;

				if (c == '\n' && !isQuoted)
				{
					if (buffer.Length != 0)
					{
						line.Add(buffer.ToString());
						buffer = new StringBuilder();
					}

					if (line.Count != 0)
						result.Add(line);

					line = new List<string>();
					whitespaceBuffer = new StringBuilder();
					continue;
				}

				var isWhitespace = char.IsWhiteSpace(c);
				var shouldSeparate = c == ValueSeparator && (!isQuoted || quoteLevel % 2 == 0);

				if (isWhitespace)
				{
					whitespaceBuffer.Append(c);
					continue;
				}

				if (whitespaceBuffer.Length != 0)
				{
					if (buffer.Length > 0 && !shouldSeparate)
						buffer.Append(whitespaceBuffer);
					whitespaceBuffer = new StringBuilder();
				}

				if (shouldSeparate)
				{
					line.Add(buffer.ToString());
					buffer = new StringBuilder();
					quoteLevel = 0;
					isQuoted = false;
				}
				else if (c == '"')
				{
					if (buffer.Length == 0 && quoteLevel == 0)
					{
						isQuoted = true;
						quoteLevel++;
						whitespaceBuffer = new StringBuilder();
						continue;
					}

					if (isQuoted)
						quoteLevel++;

					if (!isQuoted || quoteLevel % 2 == 1)
						buffer.Append(c);
				}
				else
				{
					if (isQuoted && quoteLevel != 0 && quoteLevel % 2 == 0)
					{
						line.Clear();
						tr.ReadLine();
						continue;
					}

					buffer.Append(c);
				}
			}

			if (buffer.Length != 0)
				line.Add(buffer.ToString());

			if (line.Count != 0)
				result.Add(line);

			return result;
		}
		public byte[] ToNei(Stream fileData)
		{
			List<List<string>> values;
			values = ParseCSV(fileData);

			var cols = values.Max(l => l.Count);
			var rows = values.Count;

			var encodedValues = new byte[rows * cols][];

			for (var rowIndex = 0; rowIndex < values.Count; rowIndex++)
			{
				var row = values[rowIndex];
				for (var colIndex = 0; colIndex < cols; colIndex++)
					encodedValues[colIndex + rowIndex * cols] = colIndex < row.Count
						? ShiftJisEncoding.GetBytes(row[colIndex])
						: new byte[0];
			}

			var ms = new MemoryStream();
			var bw = new BinaryWriter(ms);
			bw.Write(NEI_MAGIC);
			bw.Write(cols);
			bw.Write(rows);

			var totalLength = 0;
			foreach (var encodedValue in encodedValues)
			{
				var len = encodedValue.Length;
				if (len != 0)
					len++;

				bw.Write(encodedValue.Length == 0 ? 0 : totalLength);
				bw.Write(len);
				totalLength += len;
			}

			for (var i = 0; i < encodedValues.Length; i++)
			{
				var encodedValue = encodedValues[i];
				if (encodedValue.Length == 0)
					continue;

				bw.Write(encodedValue);
				if (i != encodedValue.Length - 1)
					bw.Write((byte)0x00);
			}

			var data = ms.ToArray();

			return Encryption.EncryptBytes(data, NEI_KEY);
		}
		private static string Escape(string value)
		{
			if (!value.Contains(ValueSeparator) && !value.Contains('\n'))
				return value;
			return $"\"{value.Replace("\"", "\"\"")}\"";
		}

		public static List<List<string>> ToCSVList(string filePath) => ParseCSV(ToCSV(Encryption.DecryptBytes(File.ReadAllBytes(filePath), NEI_KEY)));
		public static List<List<string>> ToCSVList(Stream stream) => ParseCSV(ToCSV(Encryption.DecryptBytes(ReadFully(stream), NEI_KEY)));
		public static Stream ToCSV(string filePath) => ToCSV(Encryption.DecryptBytes(File.ReadAllBytes(filePath), NEI_KEY));
		public static Stream ToCSV(Stream stream) => ToCSV(Encryption.DecryptBytes(ReadFully(stream), NEI_KEY));
		public static Stream ToCSV(byte[] neiData)
		{
			var ms = new MemoryStream(neiData);
			var br = new BinaryReader(ms);
			if (!br.ReadBytes(4).SequenceEqual(NEI_MAGIC))
			{
				Console.WriteLine($"The passed stream is not a valid NEI file");
				return null;
			}

			var cols = br.ReadUInt32();
			var rows = br.ReadUInt32();

			var strLengths = new int[cols * rows];

			for (var cell = 0; cell < cols * rows; cell++)
			{
				br.ReadInt32(); // Total length of all strings because why not
				strLengths[cell] = br.ReadInt32();
			}

			var values = new string[cols * rows];

			for (var cell = 0; cell < cols * rows; cell++)
			{
				var len = strLengths[cell];
				//values[cell] = NUty.SjisToUnicode(br.ReadBytes(len)).Substring(0, Math.Max(len - 1, 0));
				values[cell] = ShiftJisEncoding.GetString(br.ReadBytes(len), 0, Math.Max(len - 1, 0));

				Console.WriteLine($"got cell value of {values[cell]}");
			}

			MemoryStream memoryStream = new MemoryStream();
			var tw = new StreamWriter(memoryStream);

			for (var row = 0; row < rows; row++)
			{
				for (var col = 0; col < cols; col++)
				{
					tw.Write(Escape(values[row * cols + col]));
					if (col != cols - 1)
						tw.Write(ValueSeparator);
				}

				tw.WriteLine();
			}

			tw.Flush();

			memoryStream.Seek(0, SeekOrigin.Begin);
			return memoryStream;
		}
		private static byte[] ReadFully(Stream input)
		{
			byte[] buffer = new byte[16 * 1024];
			using (MemoryStream ms = new MemoryStream())
			{
				int read;
				while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
				{
					ms.Write(buffer, 0, read);
				}
				return ms.ToArray();
			}
		}
	}
}