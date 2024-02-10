namespace ArgonautReverse.PSX
{
    public enum TriggerTypePSX : int
    {
        None = 0,

        Every = 1<<1,
        WhenHit = 1<<2,
        EndFall = 1<<3,
        EndJump = 1<<4,
        In = 1<<5,
        Anim = 1<<6,
        WhenNear = 1<<7,
        WhenFar = 1<<8,
        WhenHitWall = 1<<9,
    };
}
