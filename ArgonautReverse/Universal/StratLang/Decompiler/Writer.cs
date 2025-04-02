namespace ArgonautReverse.Universal.StratLang.Decompiler
{
	public sealed class Writer
	{
		private readonly List<string> output = new();

		private int indentLevel = 0;

		public void AssertNoIndent()
		{
			if(this.indentLevel != 0)
			{
				throw new Exception("Should not have an indent");
			}
		}

		public void Indent()
		{
			this.indentLevel++;
		}
		public void Unindent()
		{
			if(this.indentLevel == 0)
			{
				throw new Exception("Can not unindented, indentation is already at 0");
			}
			this.indentLevel--;
		}

		public void WriteLine(string text)
		{
			output.Add(new string('\t', indentLevel) + text);
		}

		public IReadOnlyList<string> GetLines() => output;
	}
}