namespace ArgonautReverse.Universal.StratLang.Decompiler
{
	public sealed class Writer
	{
		private readonly StringWriter writer = new();

		private int indentLevel = 0;
		private bool needsIndents = false;

		public void AssertNoIndent()
		{
			if(indentLevel != 0)
			{
				throw new Exception("Should not have an indent");
			}
		}

		public void OpenLine()
		{
			indentLevel++;
			if(!needsIndents)
			{
				writer.WriteLine();
				needsIndents = true;
			}
		}
		public void OpenLine(string text)
		{
			CheckIndent();
			indentLevel++;
			writer.WriteLine(text);
			needsIndents = true;
		}
		public void CloseLine(string text)
		{
			indentLevel--;
			if(!needsIndents)
			{
				writer.WriteLine();
				needsIndents = true;
			}
			CheckIndent();
			writer.WriteLine(text);
			needsIndents = true;
		}

		public void Indent()
		{
			indentLevel++;
		}
		public void Unindent()
		{
			if(indentLevel == 0)
			{
				throw new Exception("Can not unindented, indentation is already at 0");
			}
			indentLevel--;
		}

		public void Open(string text)
		{
			CheckIndent();
			indentLevel++;
			writer.Write(text);
		}
		public void Close(string text)
		{
			indentLevel--;
			CheckIndent();
			writer.Write(text);
		}

		public void Write(string text)
		{
			CheckIndent();
			writer.Write(text);
		}
		public void Write(char text)
		{
			CheckIndent();
			writer.Write(text);
		}

		public void WriteInt(int i)
		{
			CheckIndent();
			writer.Write(i);
		}

		public void WriteLineIfNotEmpty()
		{
			if(!needsIndents)
			{
				WriteLine();
			}
		}

		public void WriteLineIfNotEmpty(char text)
		{
			if(!needsIndents)
			{
				WriteLine(text);
			}
		}

		public void WriteLine(string text)
		{
			CheckIndent();
			writer.WriteLine(text);
			needsIndents = true;
		}
		public void WriteLine(char text)
		{
			CheckIndent();
			writer.WriteLine(text);
			needsIndents = true;
		}

		public void WriteLine()
		{
			//Don't CheckIndents. If this is a blank line, we shouldn't add anymore
			writer.WriteLine();
			needsIndents = true;
		}

		private void CheckIndent()
		{
			if(needsIndents)
			{
				writer.Write(new string('\t', indentLevel));
				needsIndents = false;
			}
		}

		public string GetString() => writer.ToString();
	}
}