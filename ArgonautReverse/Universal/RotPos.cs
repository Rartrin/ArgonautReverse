using ArgonautReverse.IO;

namespace ArgonautReverse.Universal
{
    public struct RotPos3I : IReadable<RotPos3I>, IWritable
    {
        public Vector3I Rotation;
        public Vector3I Position;

        public RotPos3I(Vector3I rotation, Vector3I position)
        {
            Rotation = rotation;
            Position = position;
        }

        public static RotPos3I Parse(WadReader reader)
        {
            var rotation = reader.Read<Vector3I>();
            var position = reader.Read<Vector3I>();
            return new RotPos3I(rotation, position);
        }

        public readonly void Write(WadWriter writer)
        {
            writer.Write(Rotation);
            writer.Write(Position);
        }
    }

    public struct RotPos3F : IReadable<RotPos3F>, IWritable
    {
        public Vector3F Rotation;
        public Vector3F Position;

        public RotPos3F(Vector3F rotation, Vector3F position)
        {
            Rotation = rotation;
            Position = position;
        }

        public static RotPos3F Parse(WadReader reader)
        {
            var rotation = reader.Read<Vector3F>();
            var position = reader.Read<Vector3F>();
            return new RotPos3F(rotation, position);
        }

        public readonly void Write(WadWriter writer)
        {
            writer.Write(Rotation);
            writer.Write(Position);
        }
    }
}
