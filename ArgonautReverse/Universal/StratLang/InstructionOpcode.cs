namespace ArgonautReverse.Universal.StratLang
{
	public enum InstructionOpcode
	{
		CommandError = 0,//CommandErrorInstruction
		Local = 1,//VarInstruction
		Global = 2,//VarInstruction
		WorldGlobal = 3,//VarInstruction
		AlienVar = 4,//VarInstruction
		LocalAddress = 5,//VarInstruction
		GlobalAddress = 6,//VarInstruction
		WorldGlobalAddress = 7,//VarInstruction
		AlienVarAddress = 8,//VarInstruction
		Print = 9,//PrintInstruction
		Number = 10,//NumberInstruction
		UMinus = 11,//BASIC_INSTRUCTION(1,1)
		Increase = 12,//BASIC_INSTRUCTION(1,0)
		Decrease = 13,//BASIC_INSTRUCTION(1,0)
		Add = 14,//BASIC_INSTRUCTION(2,1)
		Sub = 15,//BASIC_INSTRUCTION(2,1)
		Mul = 16,//BASIC_INSTRUCTION(2,1)
		Div = 17,//BASIC_INSTRUCTION(2,1)
		Equals = 18,//BASIC_INSTRUCTION(2,0)
		Compare = 19,//BASIC_INSTRUCTION(2,1)
		LessThan = 20,//BASIC_INSTRUCTION(2,1)
		GreaterThan = 21,//BASIC_INSTRUCTION(2,1)
		SetModel = 22,//BASIC_INSTRUCTION(1,0)
		Scale = 23,//BASIC_INSTRUCTION(1,0)
		ScaleX = 24,//BASIC_INSTRUCTION(1,0)
		ScaleY = 25,//BASIC_INSTRUCTION(1,0)
		ScaleZ = 26,//BASIC_INSTRUCTION(1,0)
		Shadow = 27,//FlagInstruction
		ShadowSize = 28,//BASIC_INSTRUCTION(1,0)
		ShadowType = 29,//BASIC_INSTRUCTION(1,0)
		Hide = 30,//FlagInstruction
		Flash = 31,//FlagInstruction//Unimplemented
		Trans = 32,//BASIC_INSTRUCTION(1,0)//Unimplemented
		MoveUp = 33,//BASIC_INSTRUCTION(1,0)
		MoveDown = 34,//BASIC_INSTRUCTION(1,0)
		MoveForward = 35,//BASIC_INSTRUCTION(1,0)
		MoveBackward = 36,//BASIC_INSTRUCTION(1,0)
		MoveLeft = 37,//BASIC_INSTRUCTION(1,0)
		MoveRight = 38,//BASIC_INSTRUCTION(1,0)
		TurnRight = 39,//BASIC_INSTRUCTION(1,0)
		TurnLeft = 40,//BASIC_INSTRUCTION(1,0)
		TiltLeft = 41,//BASIC_INSTRUCTION(1,0)
		TiltRight = 42,//BASIC_INSTRUCTION(1,0)
		TiltForward = 43,//BASIC_INSTRUCTION(1,0)
		TiltBackward = 44,//BASIC_INSTRUCTION(1,0)
		TurnToPlayerX = 45,//BASIC_INSTRUCTION(1,0)
		TurnToPlayerY = 46,//BASIC_INSTRUCTION(1,0)
		TurnToPlayerXY = 47,//BASIC_INSTRUCTION(1,0)
		TurnToX = 48,//BASIC_INSTRUCTION(4,0)
		TurnToY = 49,//BASIC_INSTRUCTION(4,0)
		TurnToXY = 50,//BASIC_INSTRUCTION(4,0)
		Wobble = 51,//BASIC_INSTRUCTION(1,0)
		ReSetPos = 52,//BASIC_INSTRUCTION(0,0)
		SetPos = 53,//BASIC_INSTRUCTION(0,0)
		Jump = 54,//JumpInstruction
		ObjectFall = 55,//BASIC_INSTRUCTION(0,0)
		Hang = 56,//FlagInstruction
		WPFirst = 57,//BASIC_INSTRUCTION(0,0)
		WPLast = 58,//BASIC_INSTRUCTION(0,0)
		WPNext = 59,//BASIC_INSTRUCTION(0,0)
		WPPrev = 60,//BASIC_INSTRUCTION(0,0)
		WPDel = 61,//BASIC_INSTRUCTION(0,0)//Unimplemented
		WPNew = 62,//BASIC_INSTRUCTION(0,0)//Unimplemented
		WPNearest = 63,//BASIC_INSTRUCTION(0,0)
		WPFurthest = 64,//BASIC_INSTRUCTION(0,0)
		WPTurnToX = 65,//BASIC_INSTRUCTION(1,0)
		WPTurnToY = 66,//BASIC_INSTRUCTION(1,0)
		WPTurnToXY = 67,//BASIC_INSTRUCTION(1,0)
		AnimPlay = 68,//BASIC_INSTRUCTION(1,0)
		AnimStop = 69,//BASIC_INSTRUCTION(0,0)
		AnimClear = 70,//BASIC_INSTRUCTION(0,0)
		AnimSetSpeed = 71,//BASIC_INSTRUCTION(1,0)//Unimplemented
		CollisionType = 72,//BASIC_INSTRUCTION(1,0)
		CollRadius = 73,//BASIC_INSTRUCTION(1,0)
		CollHeight = 74,//Probably BASIC_INSTRUCTION(1,0)//Unimplemented
		CollExtent = 75,//BASIC_INSTRUCTION(1,0)
		CollView = 76,//Probably BASIC_INSTRUCTION(1,0)//Unimplemented
		CollPoints = 77,//BASIC_INSTRUCTION(1,0)
		CollSetPoint = 78,//BASIC_INSTRUCTION(4,0)
		CreateTrigger = 79,//TriggerCreateInstruction
		KillTrigger = 80,//TriggerUpdateInstruction
		HoldTriggers = 81,//BASIC_INSTRUCTION(0,0)
		ReleaseTriggers = 82,//BASIC_INSTRUCTION(0,0)
		HoldTrigger = 83,//TriggerUpdateInstruction
		ReleaseTrigger = 84,//TriggerUpdateInstruction
		Wait = 85,//BASIC_INSTRUCTION(1,0)
		Hold = 86,//BASIC_INSTRUCTION(0,0)
		Release = 87,//UNIMPLEMENTED_INSTRUCTION(0,0,0)
		Remove = 88,//BASIC_INSTRUCTION(0,0)
		MapRemove = 89,//BASIC_INSTRUCTION(0,0)
		MapAdd = 90,//UNIMPLEMENTED_INSTRUCTION(0,0,0)
		MapReplace = 91,//UNIMPLEMENTED_INSTRUCTION(0,0,0)
		Activated = 92,//BASIC_INSTRUCTION(0,0)
		Collected = 93,//BASIC_INSTRUCTION(0,0)
		Spawn = 94,//SpawnInstruction
		SpawnFrom = 95,//SpawnFromInstruction
		Link = 96,//Probably BASIC_INSTRUCTION(0,0)//Unimplemented
		Unlink = 97,//Probably BASIC_INSTRUCTION(0,0)//Unimplemented
		SoundShift = 98,//BASIC_INSTRUCTION(2,0)
		SoundStop = 99,//BASIC_INSTRUCTION(1,0)

		CdPlay = 100,//BASIC_INSTRUCTION(1,0)
		MidiLoop = 101,//UNIMPLEMENTED_INSTRUCTION(0,0,0)
		MidiVolume = 102,//UNIMPLEMENTED_INSTRUCTION(0,0,0)
		CdFade = 103,//BASIC_INSTRUCTION(1,0)
		MidiStop = 104,//UNIMPLEMENTED_INSTRUCTION(0,0,0)
		MidiQueue = 105,//UNIMPLEMENTED_INSTRUCTION(0,0,0)
		IsLight = 106,//UNIMPLEMENTED_INSTRUCTION(0,0,0)
		LightCol = 107,//UNIMPLEMENTED_INSTRUCTION(0,0,0)
		LightFade = 108,//UNIMPLEMENTED_INSTRUCTION(0,0,0)
		LightAtten = 109,//UNIMPLEMENTED_INSTRUCTION(0,0,0)
		LightType = 110,//UNIMPLEMENTED_INSTRUCTION(0,0,0)
		CollisionOn = 111,//FlagInstruction
		CollisionOff = 112,//FlagInstruction
		CollisionOffAll = 113,//BASIC_INSTRUCTION(0,0)
		SoundPlay3 = 114,//BASIC_INSTRUCTION(3,0)
		SoundPlay4 = 115,//UNIMPLEMENTED_INSTRUCTION(0,0,0)//Unimplemented on PC
		SoundPlay3ASS = 116,//BASIC_INSTRUCTION(3,1)
		SoundPlay4ASS = 117,//UNIMPLEMENTED_INSTRUCTION(0,0,0)//Unimplemented on PC
		Int = 118,//BASIC_INSTRUCTION(1,1)
		Sin = 119,//BASIC_INSTRUCTION(1,1)
		Cos = 120,//BASIC_INSTRUCTION(1,1)
		Not = 121,//BASIC_INSTRUCTION(1,1)
		Pop = 122,//BASIC_INSTRUCTION(1,0)
		StkCmp = 123,//StackCompareInstruction//Implemented but never used by Croc 2
		Address = 124,//AddressInstruction
		Jsr = 125,//JumpSubroutineInstruction
		JsrImm = 126,//JumpSubroutineInstruction
		Return = 127,//ReturnInstruction
		Beq = 128,//BranchInstruction
		Bne = 129,//BranchInstruction
		BeqImm = 130,//BranchInstruction
		BneImm = 131,//BranchInstruction
		JumpImm = 132,//JumpInstruction
		EndStrat = 133,//EndStratInstruction
		IsPlayer = 134,//BASIC_INSTRUCTION(0,0)
		And = 135,//BASIC_INSTRUCTION(2,1)
		Or = 136,//BASIC_INSTRUCTION(2,1)
		Index_Jump = 137,//IndexJumpInstruction
		BitwiseAnd = 138,//BASIC_INSTRUCTION(2,1)
		Ext_Local = 139,//VarInstruction
		Ext_LocalAddress = 140,//VarInstruction
		Ext_Global = 141,//UNIMPLEMENTED_INSTRUCTION(1,0,0)
		Ext_GlobalAddress = 142,//UNIMPLEMENTED_INSTRUCTION(1,0,0)
		ObjectJump = 143,//BASIC_INSTRUCTION(1,0)
		Ext_AlienVar = 144,//VarInstruction
		Ext_AlienVarAddress = 145,//VarInstruction
		NotEqual = 146,//BASIC_INSTRUCTION(2,1)
		ShiftLeft = 147,//BASIC_INSTRUCTION(2,1)
		ShiftRight = 148,//BASIC_INSTRUCTION(2,1)
		AnimAdvance = 149,//BASIC_INSTRUCTION(1,0)
		GreaterEqual = 150,//BASIC_INSTRUCTION(2,1)
		LessEqual = 151,//BASIC_INSTRUCTION(2,1)
		Rnd = 152,//BASIC_INSTRUCTION(1,1)
		Blink = 153,//BlinkInstruction
		LoseHeart = 154,//BASIC_INSTRUCTION(0,0)
		ResetToCheckPoint = 155,//BASIC_INSTRUCTION(0,0)
		ForceCollision = 156,//BASIC_INSTRUCTION(0,0)
		TurnFromPlayerY = 157,//BASIC_INSTRUCTION(1,0)
		PlayerAttack = 158,//FlagInstruction
		Rumble = 159,//BASIC_INSTRUCTION(2,0)
		Vibrate = 160,//BASIC_INSTRUCTION(1,0)
		SuspendIfTooFar = 161,//FlagInstruction
		CollisionBone = 162,//BASIC_INSTRUCTION(2,0)
		UseBone = 163,//BASIC_INSTRUCTION(1,0)
		IsCamera = 164,//BASIC_INSTRUCTION(0,0)
		LookAtMe = 165,//BASIC_INSTRUCTION(0,0)
		LookAtMe2 = 166,//BASIC_INSTRUCTION(0,0)
		PushCamera = 167,//BASIC_INSTRUCTION(0,0)
		PopCamera = 168,//BASIC_INSTRUCTION(0,0)
		ResetCameraPos = 169,//BASIC_INSTRUCTION(0,0)
		GainHeart = 170,//BASIC_INSTRUCTION(0,0)
		GainHeartPot = 171,//BASIC_INSTRUCTION(0,0)
		AddInv = 172,//ItemChangeInstruction
		GainCrystal = 173,//BASIC_INSTRUCTION(1,0)
		Cutscene = 174,//BASIC_INSTRUCTION(1,0)
		Inventory = 175,//ItemCountInstruction
		DebugName = 176,//DebugNameInstruction
		PlayerDistanceCheck = 177,//FlagInstruction
		SoundPlay1 = 178,//BASIC_INSTRUCTION(1,0)
		SoundPlay1ASS = 179,//BASIC_INSTRUCTION(1,1)
		SoundAddress = 180,//SoundAddressInstruction
		OnGround = 181,//FlagInstruction
		ObjectFallSlow = 182,//BASIC_INSTRUCTION(0,0)
		Player_AlienVar = 183,//VarInstruction
		Player_AlienVarAddress = 184,//VarInstruction
		CollisionOffset = 185,//BASIC_INSTRUCTION(3,0)
		Abs = 186,//BASIC_INSTRUCTION(1,1)
		Pickup = 187,//FlagInstruction
		Min = 188,//BASIC_INSTRUCTION(2,1)
		Max = 189,//BASIC_INSTRUCTION(2,1)
		SpawnParticle = 190,//BASIC_INSTRUCTION(6,0)
		Sgn = 191,//BASIC_INSTRUCTION(1,1)
		SpawnAfter = 192,//SpawnInstruction
		Camera_AlienVar = 193,//VarInstruction
		Camera_AlienVarAddress = 194,//VarInstruction
		Target_AlienVar = 195,//VarInstruction
		Target_AlienVarAddress = 196,//VarInstruction
		Collide_AlienVar = 197,//UNIMPLEMENTED_INSTRUCTION(0,0,0)
		Collide_AlienVarAddress = 198,//UNIMPLEMENTED_INSTRUCTION(0,0,0)
		Target2_AlienVar = 199,//VarInstruction

		Target2_AlienVarAddress = 200,//VarInstruction
		DontLookAtMe = 201,//BASIC_INSTRUCTION(0,0)
		RunAt60 = 202,//Probably BASIC_INSTRUCTION(0,0)//Used in DUMMY, Unimplemented in PC and PSX
		MoveForwardq = 203,//BASIC_INSTRUCTION(1,0)
		MoveBackwardq = 204,//BASIC_INSTRUCTION(1,0)
		ScreenPrint = 205,//PrintInstruction
		SoundPlay2 = 206,//BASIC_INSTRUCTION(2,0)
		SoundPlay2ASS = 207,//BASIC_INSTRUCTION(2,1)
		SetWP = 208,//BASIC_INSTRUCTION(0,0)
		ResetWP = 209,//BASIC_INSTRUCTION(0,0)
		SoundVolume = 210,//BASIC_INSTRUCTION(2,0)
		Push = 211,//FlagInstruction
		String = 212,//UNIMPLEMENTED_INSTRUCTION(0,0,0)
		SetBossHearts = 213,//BASIC_INSTRUCTION(1,0)
		LoseBossHeart = 214,//BASIC_INSTRUCTION(0,0)
		SoundShiftRelative = 215,//BASIC_INSTRUCTION(2,0)
		Smin = 216,//BASIC_INSTRUCTION(2,1)
		IsBoss = 217,//BASIC_INSTRUCTION(0,0)
		TopSay = 218,//DialogSayInstruction
		Boss_AlienVar = 219,//VarInstruction
		Boss_AlienVarAddress = 220,//VarInstruction
		GetParentPos = 221,//BASIC_INSTRUCTION(0,0)
		AfterBoss = 222,//BASIC_INSTRUCTION(0,0)
		AfterPlayer = 223,//BASIC_INSTRUCTION(0,0)
		BeforePlayer = 224,//BASIC_INSTRUCTION(0,0)
		BeforeBoss = 225,//BASIC_INSTRUCTION(0,0)
		NoHang = 226,//FlagInstruction
		Zero = 227,//BASIC_INSTRUCTION(0,1)
		TopHead = 228,//BASIC_INSTRUCTION(1,0)
		TopDialog = 229,//DialogSetInstruction
		BottomSay = 230,//DialogSayInstruction
		BottomHead = 231,//BASIC_INSTRUCTION(1,0)
		BottomDialog = 232,//DialogSetInstruction
		GetPlayerPos = 233,//BASIC_INSTRUCTION(0,0)
		GetWPpos = 234,//BASIC_INSTRUCTION(0,0)
		GetBossPos = 235,//BASIC_INSTRUCTION(0,0)
		GetDoorPos = 236,//BASIC_INSTRUCTION(0,0)
		FadeOut = 237,//BASIC_INSTRUCTION(1,0)
		FadeIn = 238,//BASIC_INSTRUCTION(1,0)
		MoveUpq = 239,//BASIC_INSTRUCTION(1,0)
		MoveDownq = 240,//BASIC_INSTRUCTION(1,0)
		ForcePlayerDist = 241,//BASIC_INSTRUCTION(0,0)
		ShadeType = 242,//BASIC_INSTRUCTION(1,0)
		NOP = 243,//BASIC_INSTRUCTION(0,0)
		SetAnimSpeed = 244,//BASIC_INSTRUCTION(1,0)
		CheckLevelDoor = 245,//BASIC_INSTRUCTION(0,0)
		BottomHeadLeft = 246,//BASIC_INSTRUCTION(0,0)
		TopHeadLeft = 247,//BASIC_INSTRUCTION(0,0)
		GainJigsaw = 248,//BASIC_INSTRUCTION(0,0)
		GainGoldenGobbo = 249,//BASIC_INSTRUCTION(0,0)
		Gain100Crystal = 250,//BASIC_INSTRUCTION(0,0)
		ResetSpline = 251,//BASIC_INSTRUCTION(0,0)
		CheckPoint = 252,//BASIC_INSTRUCTION(0,0)
		WaterTest = 253,//FlagInstruction
		IsMainCamera = 254,//BASIC_INSTRUCTION(0,0)
		ResetDialog = 255,//BASIC_INSTRUCTION(0,0)
		EndLevel = 256,//BASIC_INSTRUCTION(0,0)
		Dialog_AlienVar = 257,//VarInstruction
		Dialog_AlienVarAddress = 258,//VarInstruction
		IsDialog = 259,//BASIC_INSTRUCTION(0,0)
		Distance = 260,//BASIC_INSTRUCTION(3,1)
		Binocs = 261,//BinocsInstruction
		TopCloseDialog = 262,//BASIC_INSTRUCTION(0,0)
		BottomCloseDialog = 263,//BASIC_INSTRUCTION(0,0)
		NextInventory = 264,//BASIC_INSTRUCTION(0,0)
		PrevInventory = 265,//BASIC_INSTRUCTION(0,0)
		OtherPiece = 266,//BASIC_INSTRUCTION(0,0)
		NormalPiece = 267,//BASIC_INSTRUCTION(0,0)
		Climb = 268,//FlagInstruction
		DelInv = 269,//ItemChangeInstruction
		GainReward = 270,//BASIC_INSTRUCTION(0,0)
		WorldVector = 271,//BASIC_INSTRUCTION(3,0)
		ObjectFallVerySlow = 272,//BASIC_INSTRUCTION(0,0)
		Slope2Controller = 273,//BASIC_INSTRUCTION(1,0)
		LevelComplete = 274,//BASIC_INSTRUCTION(3,1)
		SetLevelFlag = 275,//BASIC_INSTRUCTION(1,0)
		GetLevelFlag = 276,//BASIC_INSTRUCTION(3,1)
		CalcCarTilt = 277,//BASIC_INSTRUCTION(4,0)
		MoveLeftq = 278,//BASIC_INSTRUCTION(1,0)
		MoveRightq = 279,//BASIC_INSTRUCTION(1,0)
		BitwiseNot = 280,//BASIC_INSTRUCTION(1,1)
		BordersOn = 281,//BASIC_INSTRUCTION(0,0)
		BordersOff = 282,//BASIC_INSTRUCTION(0,0)
		SoundAdsr = 283,//BASIC_INSTRUCTION(5,0)
		SoundAdsrRelative = 284,//BASIC_INSTRUCTION(5,0)
		RotatePiece = 285,//BASIC_INSTRUCTION(0,0)
		SetAmbient = 286,//UNIMPLEMENTED_INSTRUCTION(0,0,0)
		ResetAmbient = 287,//UNIMPLEMENTED_INSTRUCTION(0,0,0)
		InvActive = 288,//BASIC_INSTRUCTION(0,0)
		InvInactive = 289,//BASIC_INSTRUCTION(0,0)
		SampleStatus = 290,//BASIC_INSTRUCTION(1,1)
		ResetToCheckPointnlh = 291,//BASIC_INSTRUCTION(0,0)
		ResetDoor = 292,//BASIC_INSTRUCTION(0,0)
		StoreDoor = 293,//BASIC_INSTRUCTION(0,0)
		Camera_modified = 294,//BASIC_INSTRUCTION(0,0)
		PushPlayer = 295,//BASIC_INSTRUCTION(0,0)
		PopPlayer = 296,//BASIC_INSTRUCTION(0,0)
		ReSetPostrn = 297,//BASIC_INSTRUCTION(0,0)
		GainItem = 298,//BASIC_INSTRUCTION(0,0)
		SetItem = 299,//BASIC_INSTRUCTION(1,0)

		SetTimer = 300,//BASIC_INSTRUCTION(1,0)
		TimerOff = 301,//BASIC_INSTRUCTION(0,0)
		DistanceNoY = 302,//BASIC_INSTRUCTION(2,1)
		Swim = 303,//FlagInstruction
		Lose100Crystals = 304,//UNIMPLEMENTED_INSTRUCTION(0,0,0)
		LoseReward = 305,//BASIC_INSTRUCTION(0,0)
		LoseGoldenGobbo = 306,//BASIC_INSTRUCTION(0,0)
		NextTribe = 307,//BASIC_INSTRUCTION(0,0)
		PrevTribe = 308,//BASIC_INSTRUCTION(0,0)
		SetTimerClock = 309,//BASIC_INSTRUCTION(0,0)
		SetTimerBomb = 310,//BASIC_INSTRUCTION(0,0)
		InitBurpingGame = 311,//BASIC_INSTRUCTION(0,0)
		CloseBurpingGame = 312,//BASIC_INSTRUCTION(0,0)
		Credit = 313,//CreditInstruction
		CloseCredits = 314,//BASIC_INSTRUCTION(0,0)
		ShowRewardCard = 315,//BASIC_INSTRUCTION(0,0)
		ShowHearts = 316,//BASIC_INSTRUCTION(0,0)
		Cwg = 317,//CwgInstruction
		FadeFunction_47E960 = 318,//FadeSetUnknownInstruction
		CameraFunction_47F1E0 = 319,//BASIC_INSTRUCTION(0,0)
		CameraFunction_47F040 = 320,//BASIC_INSTRUCTION(0,0)
		CameraFunction_47F0C0 = 321,//BASIC_INSTRUCTION(0,0)
		CameraFunction_47F490 = 322,//BASIC_INSTRUCTION(0,0)
	}


	/*
	//Attempt to group up similar instructions. Potentially combine some

	CommandError,
	NOP,

	Print,ScreenPrint,//PrintInstruction
	DebugName,//DebugNameInstruction

	Local,Global,WorldGlobal,AlienVar,LocalAddress,GlobalAddress,WorldGlobalAddress,AlienVarAddress,Ext_Local,Ext_LocalAddress,Ext_Global,Ext_GlobalAddress,Ext_AlienVar,Ext_AlienVarAddress,Player_AlienVar,Player_AlienVarAddress,Camera_AlienVar,Camera_AlienVarAddress,Target_AlienVar,Target_AlienVarAddress,Collide_AlienVar,Collide_AlienVarAddress,Target2_AlienVar,Target2_AlienVarAddress,Boss_AlienVar,Boss_AlienVarAddress,Dialog_AlienVar,Dialog_AlienVarAddress,//VarInstruction

	//Constants
	Zero,//BASIC_INSTRUCTION(0,1)
	Number = 10,//NumberInstruction
	String = 212,//UNIMPLEMENTED_INSTRUCTION(0,0,0)
	Address = 124,//AddressInstruction

	Add,Sub,Mul,DivMin,Max,Smin,//BASIC_INSTRUCTION(2,1)//Math Deciaml Binary-Operations
	And,Or,BitwiseAnd,ShiftLeft,ShiftRight,//BASIC_INSTRUCTION(2,1)//Math Bitwise operations
	Compare,LessThan,GreaterThan,NotEqual,GreaterEqual,LessEqual,//BASIC_INSTRUCTION(2,1)//Math Compare
	UMinus,Int,Sin,Cos,Not,BitwiseNot,Abs,Sgn,//BASIC_INSTRUCTION(1,1)//Math Unary-Functions
	Rnd,//BASIC_INSTRUCTION(1,1)
	
	Increase = 12,//BASIC_INSTRUCTION(1,0)
	Decrease = 13,//BASIC_INSTRUCTION(1,0)
	Equals = 18,//BASIC_INSTRUCTION(2,0)//This is set, not comparison

	Shadow,Hide,Hang,PlayerAttack,SuspendIfTooFar,PlayerDistanceCheck,OnGround,Pickup,Push,NoHang,WaterTest,Climb,Swim,//FlagInstruction
	Flash,//FlagInstruction//Unimplemented

	SetModel = 22,//BASIC_INSTRUCTION(1,0)
	Scale,ScaleX,ScaleY,ScaleZ,//BASIC_INSTRUCTION(1,0)
	
	ShadowSize = 28,//BASIC_INSTRUCTION(1,0)
	ShadowType = 29,//BASIC_INSTRUCTION(1,0)
	Trans,//Probably BASIC_INSTRUCTION(1,0)//Unimplemented
	MoveUp,MoveDown,MoveForward,MoveBackward,MoveLeft,MoveRight,MoveUpq,MoveDownq,MoveForwardq,MoveBackwardq,MoveLeftq,MoveRightq,//BASIC_INSTRUCTION(1,0)
	TurnRight,TurnLeft,//BASIC_INSTRUCTION(1,0)
	TiltLeft,TiltRight,TiltForward,TiltBackward,//BASIC_INSTRUCTION(1,0)
	TurnToPlayerX,TurnToPlayerY,TurnToPlayerXY,//BASIC_INSTRUCTION(1,0)
	TurnToX,TurnToY,TurnToXY,BASIC_INSTRUCTION(4,0)
	TurnFromPlayerY = 157,//BASIC_INSTRUCTION(1,0)

	Wobble = 51,//BASIC_INSTRUCTION(1,0)

	SetPos,//BASIC_INSTRUCTION(0,0)
	ReSetPos,//BASIC_INSTRUCTION(0,0)
	ReSetPostrn,//BASIC_INSTRUCTION(0,0)

	Jump,JumpImm,//JumpInstruction
	Jsr,JsrImm,//JumpSubroutineInstruction
	Return,//ReturnInstruction
	Beq,Bne,BeqImm,BneImm,//BranchInstruction
	Index_Jump = 137,//IndexJumpInstruction

	SoundShift = 98,//BASIC_INSTRUCTION(2,0)
	SoundStop = 99,//BASIC_INSTRUCTION(1,0)
	SoundPlay1,SoundPlay2,SoundPlay3,SoundPlay4,//BASIC_INSTRUCTION(1-4,0)//4 is unimplemented on PC
	SoundPlay1ASS,SoundPlay2ASS,SoundPlay3ASS,SoundPlay4ASS,//BASIC_INSTRUCTION(1-4,1)//4 is unimplemented on PC

	SoundAddress = 180,//SoundAddressInstruction
	SoundVolume = 210,//BASIC_INSTRUCTION(2,0)
	SoundShiftRelative = 215,//BASIC_INSTRUCTION(2,0)
	SoundAdsr = 283,//BASIC_INSTRUCTION(5,0)
	SoundAdsrRelative = 284,//BASIC_INSTRUCTION(5,0)

	TopSay,BottomSay,//DialogSayInstruction
	TopHead,BottomHead,//BASIC_INSTRUCTION(1,0)
	TopDialog,BottomDialog,//DialogSetInstruction
	TopHeadLeft,BottomHeadLeft,
	TopCloseDialog,BottomCloseDialog,//BASIC_INSTRUCTION(0,0)

	ObjectFall = 55,//BASIC_INSTRUCTION(0,0)

	WPFirst,WPLast,//BASIC_INSTRUCTION(0,0)
	WPNext,WPPrev,//BASIC_INSTRUCTION(0,0)
	WPDel,//BASIC_INSTRUCTION(0,0)//Unimplemented
	WPNew,//BASIC_INSTRUCTION(0,0)//Unimplemented
	WPNearest,WPFurthest,//BASIC_INSTRUCTION(0,0)
	WPTurnToX,WPTurnToY,WPTurnToXY,//BASIC_INSTRUCTION(1,0)

	AnimPlay = 68,//BASIC_INSTRUCTION(1,0)
	AnimStop = 69,//BASIC_INSTRUCTION(0,0)
	AnimClear = 70,//BASIC_INSTRUCTION(0,0)
	AnimSetSpeed = 71,//BASIC_INSTRUCTION(1,0)//Unimplemented
	AnimAdvance = 149,//BASIC_INSTRUCTION(1,0)
	SetAnimSpeed = 244,//BASIC_INSTRUCTION(1,0)

	CollisionType = 72,//BASIC_INSTRUCTION(1,0)
	CollRadius = 73,//BASIC_INSTRUCTION(1,0)
	CollHeight = 74,//Probably BASIC_INSTRUCTION(1,0)//Unimplemented
	CollExtent = 75,//BASIC_INSTRUCTION(1,0)
	CollView = 76,//Probably BASIC_INSTRUCTION(1,0)//Unimplemented
	CollPoints = 77,//BASIC_INSTRUCTION(1,0)
	CollSetPoint = 78,//BASIC_INSTRUCTION(4,0)
	CollisionOn,CollisionOff,//FlagInstruction
	CollisionOffAll = 113,//BASIC_INSTRUCTION(0,0)
	ForceCollision = 156,//BASIC_INSTRUCTION(0,0)
	CollisionBone = 162,//BASIC_INSTRUCTION(2,0)
	CollisionOffset = 185,//BASIC_INSTRUCTION(3,0)

	CreateTrigger = 79,//TriggerCreateInstruction
	HoldTriggers = 81,//BASIC_INSTRUCTION(0,0)
	ReleaseTriggers = 82,//BASIC_INSTRUCTION(0,0)
	KillTrigger,HoldTrigger,ReleaseTrigger,//TriggerUpdateInstruction

	Wait = 85,//BASIC_INSTRUCTION(1,0)
	Hold = 86,//BASIC_INSTRUCTION(0,0)
	Release = 87,//UNIMPLEMENTED_INSTRUCTION(0,0,0)
	Remove = 88,//BASIC_INSTRUCTION(0,0)
	MapRemove = 89,//BASIC_INSTRUCTION(0,0)
	MapAdd = 90,//UNIMPLEMENTED_INSTRUCTION(0,0,0)
	MapReplace = 91,//UNIMPLEMENTED_INSTRUCTION(0,0,0)
	Activated = 92,//BASIC_INSTRUCTION(0,0)
	Collected = 93,//BASIC_INSTRUCTION(0,0)

	Spawn,SpawnAfter,//SpawnInstruction
	SpawnFrom = 95,//SpawnFromInstruction

	IsLight,IsPlayer,IsCamera,IsBoss,IsMainCamera,IsDialog,//BASIC_INSTRUCTION(0,0)

	Link,Unlink,//Probably BASIC_INSTRUCTION(0,0)//Unimplemented

	CdPlay = 100,//BASIC_INSTRUCTION(1,0)
	CdFade = 103,//BASIC_INSTRUCTION(1,0)
	MidiLoop,//FlagInstruction//Unimplemented
	MidiVolume = 102,//UNIMPLEMENTED_INSTRUCTION(0,0,0)
	MidiStop = 104,//UNIMPLEMENTED_INSTRUCTION(0,0,0)
	MidiQueue = 105,//UNIMPLEMENTED_INSTRUCTION(0,0,0)
	LightCol = 107,//UNIMPLEMENTED_INSTRUCTION(0,0,0)
	LightFade = 108,//UNIMPLEMENTED_INSTRUCTION(0,0,0)
	LightAtten = 109,//UNIMPLEMENTED_INSTRUCTION(0,0,0)
	LightType = 110,//UNIMPLEMENTED_INSTRUCTION(0,0,0)
	
	Pop = 122,//BASIC_INSTRUCTION(1,0)
	EndStrat = 133,//EndStratInstruction
	ObjectJump = 143,//BASIC_INSTRUCTION(1,0)
	Blink = 153,//BlinkInstruction
	LoseHeart = 154,//BASIC_INSTRUCTION(0,0)
	ResetToCheckPoint = 155,//BASIC_INSTRUCTION(0,0)
	Rumble = 159,//BASIC_INSTRUCTION(2,0)
	Vibrate = 160,//BASIC_INSTRUCTION(1,0)
	UseBone = 163,//BASIC_INSTRUCTION(1,0)
	LookAtMe,LookAtMe2,//BASIC_INSTRUCTION(0,0)
	PushCamera,PopCamera,//BASIC_INSTRUCTION(0,0)
	ResetCameraPos = 169,//BASIC_INSTRUCTION(0,0)
	Cutscene = 174,//BASIC_INSTRUCTION(1,0)
	AddInv,DelInv,//ItemChangeInstruction
	Inventory = 175,//ItemCountInstruction
	InvActive,InvInactive,//BASIC_INSTRUCTION(0,0)
	NextInventory = 264,//BASIC_INSTRUCTION(0,0)
	PrevInventory = 265,//BASIC_INSTRUCTION(0,0)
	GainItem = 298,//BASIC_INSTRUCTION(0,0)
	SetItem = 299,//BASIC_INSTRUCTION(1,0)
	GainHeart = 170,//BASIC_INSTRUCTION(0,0)
	GainHeartPot = 171,//BASIC_INSTRUCTION(0,0)
	GainCrystal = 173,//BASIC_INSTRUCTION(1,0)

	ObjectFallSlow = 182,//BASIC_INSTRUCTION(0,0)
	SpawnParticle = 190,//BASIC_INSTRUCTION(6,0)

	DontLookAtMe = 201,//BASIC_INSTRUCTION(0,0)
	RunAt60 = 202,//Probably BASIC_INSTRUCTION(0,0)//Used in DUMMY, Unimplemented in PC and PSX
	SetWP = 208,//BASIC_INSTRUCTION(0,0)
	ResetWP = 209,//BASIC_INSTRUCTION(0,0)

	SetBossHearts = 213,//BASIC_INSTRUCTION(1,0)
	LoseBossHeart = 214,//BASIC_INSTRUCTION(0,0)

	AfterBoss,AfterPlayer,BeforePlayer,BeforeBoss,//BASIC_INSTRUCTION(0,0)
	
	GetParentPos,GetPlayerPos,GetWPpos,GetBossPos,//BASIC_INSTRUCTION(0,0)
	GetDoorPos = 236,//BASIC_INSTRUCTION(0,0)

	FadeOut = 237,//BASIC_INSTRUCTION(1,0)
	FadeIn = 238,//BASIC_INSTRUCTION(1,0)
	FadeFunction_47E960 = 318,//FadeSetUnknownInstruction

	ForcePlayerDist = 241,//BASIC_INSTRUCTION(0,0)
	ShadeType = 242,//BASIC_INSTRUCTION(1,0)
	CheckLevelDoor = 245,//BASIC_INSTRUCTION(0,0)
	
	GainJigsaw = 248,//BASIC_INSTRUCTION(0,0)
	GainGoldenGobbo,LoseGoldenGobbo,//BASIC_INSTRUCTION(0,0)
	Gain100Crystal,Lose100Crystals,//BASIC_INSTRUCTION(0,0)//Lose unimplemented but code exists
	GainReward,LoseReward,//BASIC_INSTRUCTION(0,0)

	ResetSpline = 251,//BASIC_INSTRUCTION(0,0)
	CheckPoint = 252,//BASIC_INSTRUCTION(0,0)
	ResetDialog = 255,//BASIC_INSTRUCTION(0,0)
	EndLevel = 256,//BASIC_INSTRUCTION(0,0)
	Distance = 260,//BASIC_INSTRUCTION(3,1)
	Binocs = 261,//BinocsInstruction

	OtherPiece = 266,//BASIC_INSTRUCTION(0,0)
	NormalPiece = 267,//BASIC_INSTRUCTION(0,0)
	RotatePiece = 285,//BASIC_INSTRUCTION(0,0)

	WorldVector = 271,//BASIC_INSTRUCTION(3,0)
	ObjectFallVerySlow = 272,//BASIC_INSTRUCTION(0,0)
	Slope2Controller = 273,//BASIC_INSTRUCTION(1,0)
	
	LevelComplete = 274,//BASIC_INSTRUCTION(3,1)
	SetLevelFlag = 275,//BASIC_INSTRUCTION(1,0)
	GetLevelFlag = 276,//BASIC_INSTRUCTION(3,1)

	CalcCarTilt = 277,//BASIC_INSTRUCTION(4,0)
	BordersOn,BordersOff,//BASIC_INSTRUCTION(0,0)
	SetAmbient = 286,//UNIMPLEMENTED_INSTRUCTION(0,0,0)
	ResetAmbient = 287,//UNIMPLEMENTED_INSTRUCTION(0,0,0)
	SampleStatus = 290,//BASIC_INSTRUCTION(1,1)
	ResetToCheckPointnlh = 291,//BASIC_INSTRUCTION(0,0)

	ResetDoor = 292,//BASIC_INSTRUCTION(0,0)
	StoreDoor = 293,//BASIC_INSTRUCTION(0,0)
	PushPlayer = 295,//BASIC_INSTRUCTION(0,0)
	PopPlayer = 296,//BASIC_INSTRUCTION(0,0)

	SetTimer = 300,//BASIC_INSTRUCTION(1,0)
	TimerOff = 301,//BASIC_INSTRUCTION(0,0)
	DistanceNoY = 302,//BASIC_INSTRUCTION(2,1)
	NextTribe = 307,//BASIC_INSTRUCTION(0,0)
	PrevTribe = 308,//BASIC_INSTRUCTION(0,0)
	SetTimerClock,SetTimerBomb,//BASIC_INSTRUCTION(0,0)

	InitBurpingGame,CloseBurpingGame,//BASIC_INSTRUCTION(0,0)

	Credit = 313,//CreditInstruction
	CloseCredits = 314,//BASIC_INSTRUCTION(0,0)
	ShowRewardCard = 315,//BASIC_INSTRUCTION(0,0)
	ShowHearts = 316,//BASIC_INSTRUCTION(0,0)
	Cwg = 317,//CwgInstruction
	
	Camera_modified = 294,//BASIC_INSTRUCTION(0,0)
	CameraFunction_47F1E0 = 319,//BASIC_INSTRUCTION(0,0)
	CameraFunction_47F040 = 320,//BASIC_INSTRUCTION(0,0)
	CameraFunction_47F0C0 = 321,//BASIC_INSTRUCTION(0,0)
	CameraFunction_47F490 = 322,//BASIC_INSTRUCTION(0,0)

	StkCmp = 123,//StackCompareInstruction//Implemented but never used by Croc 2
	 */
}