using ArgonautReverse.OpenStratEngine;

namespace ArgonautReverse.PC
{
    public enum StratEntityFlags0PC : uint
	{
		ST_FLAGS0_NONE = 0,
		ST_FLAGS0_1	= (1<<0),
		ST_FLAGS0_DONE = (1<<1),
		ST_FLAGS0_DEAD = (1<<2),
		ST_FLAGS0_ANIM_PLAYING = (1<<3),
		ST_FLAGS0_HELD = (1<<4),
		ST_FLAGS0_SHADOW = (1<<5),
		ST_FLAGS0_HOLD_TRIGGERS = (1<<6),
		ST_FLAGS0_SPRITE = (1<<7),
		ST_FLAGS0_BLINKING = (1<<8),
		ST_FLAGS0_JUMPING = (1<<9),
		ST_FLAGS0_FALLING = (1<<10),
		ST_FLAGS0_PLAYER = (1<<11),
		ST_FLAGS0_ENDFALL = (1<<12),
		ST_FLAGS0_ENDJUMP = (1<<13),
		ST_FLAGS0_COLL_ALWAYS = (1<<14),
		ST_FLAGS0_COLL_TRACK = (1<<15),
		ST_FLAGS0_COLL_PLAYER = (1<<16),
		ST_FLAGS0_COLL_OBJECT = (1<<17),
		ST_FLAGS0_COLL_WALL = (1<<18),
		ST_FLAGS0_COLL_PUSHABLE = (1<<19),
		ST_FLAGS0_COLL_REBOUND = (1<<20),
		ST_FLAGS0_ANIMCTRLSPD = (1<<21),
		ST_FLAGS0_STOODON = (1<<22),
		ST_FLAGS0_STOODON_PLAYER = (1<<23),
		ST_FLAGS0_HIDE = (1<<24),
		ST_FLAGS0_EYESCLOSED = (1<<25),
		ST_FLAGS0_HIT = (1<<26),
		ST_FLAGS0_CAMERA = (1<<27),
		ST_FLAGS0_ATTACK = (1<<28),
		ST_FLAGS0_TRIGGERED = (1<<29),
		ST_FLAGS0_CANTBEHIT = (1<<30),
		ST_FLAGS0_PUSH = (1u<<31),
	}

	public enum StratEntityFlags1PC : uint
	{
		ST_FLAGS1_NONE = 0,
		ST_FLAGS1_SLIDE = (1<<0),
		ST_FLAGS1_ON_GROUND = (1<<1),
		ST_FLAGS1_COLL_RADIUS = (1<<2),
		ST_FLAGS1_COLL_COLUMN = (1<<3),
		ST_FLAGS1_MOVE_DOWN = (1<<4),
		ST_FLAGS1_COLL_MODEL = (1<<5),
		ST_FLAGS1_HIT_MODEL = (1<<6),
		ST_FLAGS1_TRIG_USED = (1<<7),
		ST_FLAGS1_NO_HANG = (1<<8),
		ST_FLAGS1_PICKUP = (1<<9),
		ST_FLAGS1_COLL_TUBE = (1<<10),
		ST_FLAGS1_ONSCREEN = (1<<11),
		ST_FLAGS1_HANG = (1<<12),
		ST_FLAGS1_AUTOBLINK = (1<<13),
		ST_FLAGS1_CALCDIST = (1<<14),
		ST_FLAGS1_TOOFAR = (1<<15),
		ST_FLAGS1_TOOFAR_SUSPEND = (1<<16),
		ST_FLAGS1_RUNAT60 = (1<<17),
		ST_FLAGS1_INVIEWZONE = (1<<18),
		ST_FLAGS1_FLATSHADE = (1<<19),
		ST_FLAGS1_MIXEDSHADE = (1<<20),
		ST_FLAGS1_WATERTEST = (1<<21),
		ST_FLAGS1_INWATER = (1<<22),//PSX Aladdin: DEPTHCUE_WITH_MAP
		ST_FLAGS1_MAINCAMERA = (1<<23),
		ST_FLAGS1_OBJECT_ABOVE = (1<<24),
		ST_FLAGS1_PLAYER_ABOVE = (1<<25),
		ST_FLAGS1_CLIMBING = (1<<26),
		ST_FLAGS1_8000000 = (1<<27),//PSX Aladdin: STICK2FLOOR
		ST_FLAGS1_SWIM = (1<<28),
		ST_FLAGS1_20000000 = (1<<29),//PSX Aladdin: OBJMOVED
		ST_FLAGS1_40000000 = (1<<30),//PSX Aladdin: SUSPEND_WITH_PARENT
		ST_FLAGS1_80000000 = (1u<<31),
	}

	public sealed class StratEntityPC
	{
		public StratEntityPC next;
		public StratEntityPC prev;
		public StratEntityPC Parent;
		//public int* InstrStream;
		public StratObjectPC model;
		public AnimationStructPC animation;
		public string Name;
		public int distanceToPlayer;

		//union
		//{
		public RotPos3I newRotPos;
		//RotPos3Fx RotPosFx;
		//};
		public RotPos3I OldRotPos;
		public Vector3I scale;
		public Matrix3x4I matrix0;
		public RotPos3I StartRotPos;
	
		//public CollisionPoints* collPoints;
		public short collExtent;
		public ushort collisionBoneCount;
		//public CollisionBone* collisionBones;
		public Vector3I collisionOffset;
		public int collRadius;
		public int gapField6;
		public short wField7;
		public short wField8;
		public short wField9;
		public short wField10;
		public Vector3I vec0;
		public StratEntityFlags0PC flags0;
		public StratEntityFlags1PC flags1;
		public MapStratPC map;
		//public LocalVarsStruct* LocalVars;
		//public TriggerStruct1* triggers;
		public short wField0;
		public short wField1;
		public int field1;
		public int triggerCount;
		//public int* StackPtr;
		public int Fade;
		public int animIndex0;
		public int animSpeed;
		public int animFrame;
		public int verticalVelocity;
		public WaypointPC wpFirst;
		public WaypointPC wpLast;
		public WaypointPC wpCurrent;
		public WaypointPC wpField;
		public short shadowSpriteIndex;
		public short shadowSize;
		public byte bField2;
		public byte blinkCount;
		public byte blinkCountdown;
		public byte blinkNum;
		//public byte field6[2];
		//public short gap13[2];
		//public int gap14[1];
	}
}
