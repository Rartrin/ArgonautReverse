using ArgonautReverse.Engine.Mappings;
using ArgonautReverse.Universal.StratLang;

namespace ArgonautReverse.Engine.Versions
{
	//Croc 2 PS1 US Demo's DUMMY.DAT
	//The files in here were almost certainly built prior to the rest of the demo
	public static class CROC_2_DEMO_PS1_DUMMY
	{
		public static DatVersion DatVersion => CROC_2_DEMO_PS1_DUMMY_Dat.Instance;
		public static DirFormat? DirFormat => null;

		public static WadVersion WadVersion_Early => CROC_2_DEMO_PS1_DUMMY_Wad.Instance_Early;
		public static WadVersion WadVersion_Latest => CROC_2_DEMO_PS1_DUMMY_Wad.Instance_Latest;

		private sealed class CROC_2_DEMO_PS1_DUMMY_Dat:DatVersionPSX
		{
			public static readonly DatVersion Instance = new CROC_2_DEMO_PS1_DUMMY_Dat();

			public override string Title => "Croc 2 Demo PS1 (Dummy)";
			public override string FilenameDAT => "DUMMY.DAT";
			public override string? FilenameDIR => null;
			public override DirFormat? DirFormat => null;

			public override WadVersion GetWadVersion(string? wadName)
			{
				if(wadName!=null && CROC_2_DEMO_PS1_DUMMY_Wad.wadVersions.TryGetValue(wadName, out var wadVerion))
				{
					return wadVerion;
				}
				return WadVersion_Latest;
			}

			public override IReadOnlyCollection<WadVersion> WadVersions => CROC_2_DEMO_PS1_DUMMY_Wad.wadVersions.Values;
		}

		private sealed class CROC_2_DEMO_PS1_DUMMY_Wad:WadVersion
		{
			public static readonly Dictionary<string,WadVersion> wadVersions = new Dictionary<string,WadVersion>();

			public override DateTime BuildDate{get;}
			
			public override bool NEW_COLLISION => false;
			public override bool KEYFRAME_STUFF => false;

			private CROC_2_DEMO_PS1_DUMMY_Wad(int buildVersionOrder, params string[] wadNames)
			{
				if(buildVersionOrder<0 || 1000<=buildVersionOrder)
				{
					throw new Exception();
				}
				//Actual dates are unknown but it was prior to the demo which was 1999, 3, 4
				BuildDate = new DateTime(1999, 3, 3, 0, 0, 0, buildVersionOrder);

				foreach(var wadName in wadNames)
				{
					wadVersions.Add(wadName, this);
				}
			}

			public override InstructionOpcode MapOpcode(int value)
			{
				if(this != Instance_Latest)
				{
					//TODO Early dummy WAD opcode mapping.
					//Console.WriteLine("WARNING: Parsing script on earlier dummy WAD, may nto be compatible with regular dummy WAD mapping.");
				}
				if(value <= (int)InstructionOpcode.Spawn)
				{
					return (InstructionOpcode)value;
				}
				return value switch
				{
					//Same values
					10 => InstructionOpcode.Number,
					20 => InstructionOpcode.LessThan,
					22 => InstructionOpcode.SetModel,
					27 => InstructionOpcode.Shadow,
					29 => InstructionOpcode.ShadowType,
					68 => InstructionOpcode.AnimPlay,
					73 => InstructionOpcode.CollRadius,
					77 => InstructionOpcode.CollPoints,
					78 => InstructionOpcode.CollSetPoint,
					79 => InstructionOpcode.CreateTrigger,
					85 => InstructionOpcode.Wait,
					86 => InstructionOpcode.Hold,

					88 => InstructionOpcode.Remove,

					94 => InstructionOpcode.Spawn,
					95 => throw new NotImplementedException("Unknown OpCode 1"),
					96 => InstructionOpcode.SpawnFrom,
					#region Fuzzy Zone 1

					//4 additional values somewhere in here

					//Needs opcodes for known instructions:
					//Link (Unimplmented)
					//Unlink (Unimplmented)

					#endregion

					104 => InstructionOpcode.SoundShift,//Partical guess by location. Inputs match and no adjacent do.
					105 => InstructionOpcode.SoundStop,//Guessed by location

					117 => InstructionOpcode.CollisionOn,
					118 => InstructionOpcode.CollisionOff,//Guessed by location

					124 => InstructionOpcode.Int,//Guessed by location
					125 => InstructionOpcode.Sin,//Guessed by location
					126 => InstructionOpcode.Cos,
					127 => InstructionOpcode.Not,//Guessed by location

					130 => InstructionOpcode.Address,
					
					132 => InstructionOpcode.JsrImm,
					133 => InstructionOpcode.Return,
					134 => InstructionOpcode.Beq,//Guessed by location
					135 => InstructionOpcode.Bne,//Guessed by location
					136 => InstructionOpcode.BeqImm,
					
					138 => InstructionOpcode.JumpImm,
					139 => InstructionOpcode.EndStrat,
					140 => InstructionOpcode.IsPlayer,
					141 => InstructionOpcode.And,//Guessed by location
					142 => InstructionOpcode.Or,//Guessed by location
					143 => InstructionOpcode.Index_Jump,
					144 => InstructionOpcode.BitwiseAnd,//Partical Guess but seems right
					145 => InstructionOpcode.Ext_Local,//Guessed by location
					146 => InstructionOpcode.Ext_LocalAddress,//Guessed by location

					149 => InstructionOpcode.ObjectJump,//Partical Guess but seems right
					150 => InstructionOpcode.Ext_AlienVar,//Guessed by location

					152 => InstructionOpcode.NotEqual,//Rough Guess
					153 => InstructionOpcode.ShiftLeft,//Fits with expected inputs but can't comfirm
					154 => InstructionOpcode.ShiftRight,//Fits with expected inputs but can't comfirm

					#region Fuzzy Zone 2

					//7 additional values somewhere in here
					


					//There is a bunch of animation stuff around it where it is used and it takes in a fixed point type which matches.
					162 => InstructionOpcode.AnimAdvance,//This is an extremely rough guess.


					#endregion
					163 => InstructionOpcode.GreaterEqual,//Matches in areas
					164 => InstructionOpcode.LessEqual,//Guessed by location and almost matches patterns in areas
					165 => InstructionOpcode.Rnd,//Fits but can't confirm
					166 => InstructionOpcode.Blink,
					167 => InstructionOpcode.LoseHeart,//Guessed by location
					168 => InstructionOpcode.ResetToCheckPoint,
					169 => InstructionOpcode.ForceCollision,
					
					171 => InstructionOpcode.PlayerAttack,
					172 => InstructionOpcode.Rumble,//Guessed by location
					
					174 => InstructionOpcode.SuspendIfTooFar,//Guessed by location
					175 => InstructionOpcode.CollisionBone,
					176 => InstructionOpcode.UseBone,//Guessed by location
					177 => InstructionOpcode.IsCamera,//Guessed by location
					178 => InstructionOpcode.LookAtMe,

					#region Unconfirmed
					//179 => InstructionOpcode.LookAtMe2,//Guessed by location
					//180 => InstructionOpcode.PushCamera,//Guessed by location
					//181 => InstructionOpcode.PopCamera,//Guessed by location
					//182 => InstructionOpcode.ResetCameraPos,//Guessed by location
					//183 => InstructionOpcode.GainHeart,//Guessed by location
					//184 => InstructionOpcode.GainHeartPot,//Guessed by location
					//185 => InstructionOpcode.AddInv,//Guessed by location
					#endregion

					186 => InstructionOpcode.GainCrystal,//Guessed by location
					187 => InstructionOpcode.Cutscene,//Guessed by location

					189 => InstructionOpcode.DebugName,
					190 => InstructionOpcode.PlayerDistanceCheck,//Guessed by location
					191 => InstructionOpcode.SoundPlay1,//Guessed by location

					193 => InstructionOpcode.SoundAddress,
					194 => InstructionOpcode.OnGround,
					195 => InstructionOpcode.ObjectFallSlow,//Guessed by location
					196 => InstructionOpcode.Player_AlienVar,//Guessed by location
					197 => InstructionOpcode.Player_AlienVarAddress,//Guessed by location
					198 => InstructionOpcode.CollisionOffset,
					199 => InstructionOpcode.Abs,//Guessed by location
					200 => InstructionOpcode.Pickup,//Guessed by location
					201 => InstructionOpcode.Min,//Guessed by location
					202 => InstructionOpcode.Max,//Guessed by location, inputs fit
					203 => InstructionOpcode.SpawnParticle,
					204 => InstructionOpcode.Sgn,
					205 => InstructionOpcode.SpawnAfter,

					208 => InstructionOpcode.Target_AlienVar,//Guessed by location

					215 => InstructionOpcode.RunAt60,//Guessed by location
					216 => InstructionOpcode.MoveForwardq,//Guessed by location
					217 => InstructionOpcode.MoveBackwardq,//Guessed by location

					219 => InstructionOpcode.SoundPlay2,//Guessed by location
					220 => InstructionOpcode.SoundPlay2ASS,//Guessed by location
					221 => InstructionOpcode.SetWP,//Guessed by location
					222 => InstructionOpcode.ResetWP,//Guessed by location
					223 => InstructionOpcode.SoundVolume,//Guessed by location
					224 => InstructionOpcode.Push,//Guessed by location
					225 => InstructionOpcode.String,

					#region Unknown Zone (Guessed using spacing above)
					226 => InstructionOpcode.SetBossHearts,//Showed up in same place as IsBoss
					227 => InstructionOpcode.LoseBossHeart,//Showed up in same place as IsBoss

					229 => InstructionOpcode.Smin,//Guessed by location
					230 => InstructionOpcode.IsBoss,//Guessed by location
					#endregion

					//TODO: THIS IS BAD. This shows up in some cases like referencing spawn processes in dummy wad, likely an offset.
					//0x10000 => InstructionOpcode.NOP,

					//>=# and <=# => (InstructionOpcode)(value-13),//stDebugName to stString
					_ => throw new NotImplementedException($"Opcode {value} either does not exist or has not been implemented.")
				};
			}

			public override TriggerType MapTriggerType(int value) => MapperCroc2.TriggerTypeMapper(value);

			public static readonly WadVersion Instance_Early = new CROC_2_DEMO_PS1_DUMMY_Wad(100,
				"00BD7800",//Dino Fight
				"01864000",//Early snow hub
				"01BEF000",//Early Sledding/Snowman roll
				"01CBF000"//Widgies
			);
			//Version used by most
			public static readonly WadVersion Instance_Latest = new CROC_2_DEMO_PS1_DUMMY_Wad(999, "__Latest__");
		}
	}
}