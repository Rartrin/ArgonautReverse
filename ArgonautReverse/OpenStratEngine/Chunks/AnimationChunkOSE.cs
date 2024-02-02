using ArgonautReverse.IO;

namespace ArgonautReverse.OpenStratEngine.Chunks
{
	public sealed class AnimationChunkOSE:ChunkOSE
	{
		public override ChunkTypeOSE ChunkType => ChunkTypeOSE.Animations;

		public IReadOnlyList<AnimationOSE> Animations{get;}

		public AnimationChunkOSE(IReadOnlyList<AnimationOSE> animations)
		{
			Animations = animations;
		}

		protected override void WriteData(ChunkWriter writer)
		{
			writer.Write(Animations.Count);
			writer.WriteArrayMultipass(Animations);
		}
	}
}
