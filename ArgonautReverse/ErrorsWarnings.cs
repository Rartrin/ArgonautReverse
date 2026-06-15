namespace ArgonautReverse
{
	public sealed class UnsupportedParsing(string feature_name):NotImplementedException($"Sorry, {feature_name} parsing / exporting isn't supported (yet) on this game.");

	public sealed class UnsupportedSerialization(string feature_name):NotImplementedException($"Sorry, {feature_name} serializing isn't supported (yet) on this game.");

	public abstract class ReverseError(string explanation, int? absolute_file_offset = null):Exception
	{
		public string explanation = explanation;
		public int? absolute_file_offset = absolute_file_offset;

		public override string Message =>
			(absolute_file_offset.HasValue ? $"A reversing error has been encountered at offset {absolute_file_offset.Value:X}:\n" : "A reversing error has been encountered:\n")
			+ $"{explanation}\n" + "If you think this error isn't supposed to happen, you can ask me for help (contact details in the README).";
		public override string ToString() => Message;
	}

	public sealed class NegativeIndexError(int absolute_file_offset, string cause, int value, object? entire):ReverseError($"A negative {cause} index has been found: {value}. Whole {cause}: {entire}", absolute_file_offset)
	{
		public const string CAUSE_VERTEX = "vertex";
		public const string CAUSE_VERTEX_NORMAL = "vertex normal";
		public const string CAUSE_FACE = "face";
	}

	public sealed class VerticesNormalsGroupsMismatch(int n_vertices_groups, int n_normals_groups, int absolute_file_offset):ReverseError($"Different amounts of vertices groups ({n_vertices_groups}) and normals groups ({n_normals_groups}) found.", absolute_file_offset);

	public sealed class IncompatibleAnimationError(int n_model_vg, int n_anim_vg):ReverseError($"This model has {n_model_vg} vertex groups, but this animation is designed for models with {n_anim_vg} vertex groups, thus they are incompatible.");

	public sealed class ZeroRunLengthError(int absolute_file_offset):ReverseError("A zero run length has been found while decompressing.", absolute_file_offset);

	public abstract class ReverseWarning(int absolute_file_offset, string message):ReverseError(message, absolute_file_offset)
	{
		protected static void WarnInner(string message)
		{
			Console.Write("WARNING: " + message);
		}

		protected static string IgnoreWarningsMessage => "\nIf you think that the amounts are coherent, you can silence this warning with the --ignore-warnings commandline option.";
	}

	public sealed class TexturesWarning(int absolute_file_offset, int n_textures, int n_rows):ReverseWarning(absolute_file_offset, GetWarningMessage(n_textures, n_rows))
	{
		private static string GetWarningMessage(int n_textures, int n_rows) =>
			$"Too much textures ({n_textures}), or incorrect row count ({n_rows}).\n"+
			$"It is most probably caused by an inaccuracy in my reverse engineering of the textures format.";
		public static void Warn(int n_textures, int n_rows) => WarnInner(GetWarningMessage(n_textures, n_rows));
	}

	public sealed class Models3DWarning(int absolute_file_offset, int n_vertices, int n_faces):ReverseWarning(absolute_file_offset, GetWarningMessage(n_vertices, n_faces) + IgnoreWarningsMessage)
	{
		private static string GetWarningMessage(int n_vertices, int n_faces) =>
			$"Too many vertices or faces ({n_vertices} vertices, {n_faces} faces).\n"+
			"It is most probably caused by an inaccuracy in my reverse engineering of the models format.";
		public static void Warn(int n_vertices, int n_faces) => WarnInner(GetWarningMessage(n_vertices, n_faces));
	}

	public sealed class AnimationsWarning(int absolute_file_offset, int n_total_frames):ReverseWarning(absolute_file_offset, GetWarningMessage(n_total_frames))
	{
		private static string GetWarningMessage(int n_total_frames) =>
			$"Too much frames in animation (or no frame): {n_total_frames} frames.\n"+
			$"It is most probably caused by an inaccuracy in my reverse engineering of the textures format.";
		public static void Warn(int n_total_frames) => WarnInner(GetWarningMessage(n_total_frames));
	}
}