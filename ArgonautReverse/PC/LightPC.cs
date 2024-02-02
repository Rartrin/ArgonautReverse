namespace ArgonautReverse.PC
{
    public sealed class LightPC 
	{
		public Color4F LightColor;		/* Color of light */
		public Vector3F WorldPosition;
		public Vector3F WorldDirection;

		public Vector3F LocalPosition;
		public Vector3F LocalDirection;

		public float maybeV_squared;
		public float maybeU_squared;
		public float maybeV;
		public float maybeU;
		public float UNKNOWNFIELD1;
		public int maybeDepth;
		public int type;
	}
}
