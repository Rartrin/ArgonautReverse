namespace ArgonautReverse.PSX
{
    [Flags]
    public enum SPSXFlagsPSX : uint
    {
        HAS_AMBIENT_TRACKS = 1 << 0,//SF_HASAMBIENT
        AMBIENTSEP = 1 << 1,//TODO: What is this?
        HAS_COMMON_SFX_AND_DIALOGUES_BGMS = 1 << 2, //SF_HASSTREAMS // Stored in SPSX, generally found in several levels
        HAS_LEVEL_SFX = 1 << 3, // Stored in END, generally level-specific//TODO: Missing in Aladdin
        HAS_AMBIENT_TRACKS_ = 1 << 4,//TODO: Missing in Aladdin
    }
}