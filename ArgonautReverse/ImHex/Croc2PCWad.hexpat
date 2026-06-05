import std.io;

struct Vector2<T>
{
    T X;
    T Y;
};

struct Vector3<T>
{
    T X;
    T Y;
    T Z;
};

struct Vector4<T>
{
    T X;
    T Y;
    T Z;
    T W;
};

struct Matrix4x4F
{
    float m[4*4];
};

enum ChunkType:u32
{
    Unknown = 0,
    //PC Chunks
    ID_PC_INFO      = (('I' << 24) + ('N' << 16) + ('F' << 8) + 'O'),
    ID_PC_VERSION   = (('V' << 24) + ('E' << 16) + ('R' << 8) + 'S'),
    ID_PC_MAP       = (('M' << 24) + ('A' << 16) + ('P' << 8) + ' '),
    ID_PC_TRACK     = (('T' << 24) + ('R' << 16) + ('A' << 8) + 'K'),
    ID_PC_TEXT      = (('T' << 24) + ('E' << 16) + ('X' << 8) + 'T'),
    ID_PC_LIGHT     = (('L' << 24) + ('G' << 16) + ('H' << 8) + 'T'),
    ID_PC_STRAT     = (('S' << 24) + ('T' << 16) + ('P' << 8) + 'C'),
    ID_PC_WADFLAGS  = (('W' << 24) + ('F' << 16) + ('P' << 8) + 'C'),
    
    ID_PC_SAMPLE    = (('S' << 24) + ('M' << 16) + ('P' << 8) + 'C'),
    ID_PC_LANG      = (('L' << 24) + ('G' << 16) + ('P' << 8) + 'C'),
    
    ID_PC_AMPC      = (('A' << 24) + ('M' << 16) + ('P' << 8) + 'C'),
    ID_PC_FONT      = (('F' << 24) + ('O' << 16) + ('N' << 8) + 'T'),
    ID_PC_SPRITE    = (('S' << 24) + ('P' << 16) + ('R' << 8) + 'T'),
    ID_PC_RIMG      = (('R' << 24) + ('I' << 16) + ('M' << 8) + 'G'),
    
    //Universal Chunks
    ID_END          =(('E' << 24) + ('N' << 16) + ('D' << 8) + ' '),
};

struct INFOChunk
{
    u32 value;//This should always be 1.
};
struct VersionChunk
{
    u32 value;//This should always be 1.
};

bitfield WadFlagPC
{
    bool WAD_FLAG_1:1;
    bool WAD_FLAG_2:1;
    bool WAD_FLAG_4:1;
    bool WAD_FLAG_8:1;
    bool WAD_FLAG_MAP_DEPTHCUED:1;
    bool WAD_FLAG_20:1;
    bool WAD_FLAG_40:1;
    bool WAD_FLAG_80:1;
    bool WAD_FLAG_HAS_CUTSCENES:1;
    bool WAD_FLAG_200:1;//9 
    bool WAD_FLAG_400:1;//10 
    bool WAD_FLAG_800:1;//11 
    bool WAD_FLAG_1000:1;//12 
    bool WAD_FLAG_2000:1;//13 
    bool WAD_FLAG_4000:1;//14 
    bool WAD_FLAG_8000:1;//15 
    bool WAD_FLAG_HAS_CHANGING_GEOMETRY:1;
    bool WAD_FLAG_20000:1;//17 
    bool WAD_FLAG_40000:1;//18 
    bool WAD_FLAG_80000:1;//19 
    bool WAD_FLAG_100000:1;//20 
    bool WAD_FLAG_CAMERAPOINTS:1;//21 
    bool WAD_FLAG_400000:1;//22 
    bool WAD_FLAG_800000:1;//23 
    bool WAD_FLAG_1000000:1;//24 
    bool WAD_FLAG_2000000:1;//25 
    bool WAD_FLAG_4000000:1;//26 
    bool WAD_FLAG_8000000:1;//27 
    bool WAD_FLAG_10000000:1;//28 
    bool WAD_FLAG_20000000:1;//29 
    bool WAD_FLAG_40000000:1;//30 
    bool WAD_FLAG_80000000:1;//31 
};
WadFlagPC wadFlags;
struct WadFlagsChunk
{
    WadFlagPC flags;
    wadFlags = flags;
};

struct ENDChunkPC
{
};

struct MapStratPC
{
    Vector3<u16> rotation;
    Vector3<s32> position;
    s32 instructionStreamOffset;
    s32 paramCount;
    s32 ptrOffset;
    s32 LocalCount;
    s32 TriggerCount;
    u32 Collision;
    s32 CollisionBoneCount;
    s32 FirstWPIndex;
    s32 LastWP_Placeholder;
    u32 Flags;
    
};

struct MAPChunk
{

};

struct ModelVertexPC
{
    Vector3<float> Position;
    Vector3<float> Direction;
};

struct ModelTrianglePC
{
    u16 flags;
    u16 vertexIndices[3];
    u16 SpriteIndex;
    u16;//Padding
    Vector4<float> pos;
};

struct StratObjectPC_Header
{
    Vector3<float> vec[9];
    u16 verticeCount;
    u16 triangleCount;
    u32 vertices_Placeholder;
    u32 triangles_Placeholder;
    u16 wField1;
    u16 wField2;
    u32 array2_Placeholder;
};

struct ModelCollisionStruct0PC
{
    s16 wField0;
    s16 wField1;
    s32 field2;
};

struct Model_SubStruct1PC
{
    u8 bField0;
    u8 bField1;
    s16 wGapField0;
    ModelCollisionStruct0PC collisionArray[4];
    s16 Y;
    s16 X;
    s32 field4;
};

struct StratObject2PC_Header : StratObjectPC_Header
{
    u16 wField0;
    u16 boneCount;
    u32 boundVertCounts_Placeholder;//Always 0
};

struct StratObject2PC_Singlepass
{
    StratObject2PC_Header header;
    
    //StratObject2PC data
    u16 boneVertCounts[header.boneCount];
    //StratObjectPC data
    ModelVertexPC vertices[header.verticeCount];
    ModelTrianglePC triangles[header.triangleCount];
    
    Model_SubStruct1PC array2[header.wField1+header.wField2];
};

u32 StratObject2PC_Multipass_Index = 0;
struct StratObject2PC_Data_Multipass
{
    //StratObject2PC data
    u16 boneVertCounts[parent.headers[StratObject2PC_Multipass_Index].boneCount];
    //StratObjectPC data
    ModelVertexPC vertices[parent.headers[StratObject2PC_Multipass_Index].verticeCount];
    ModelTrianglePC triangles[parent.headers[StratObject2PC_Multipass_Index].triangleCount];
    Model_SubStruct1PC array2[parent.headers[StratObject2PC_Multipass_Index].wField1+parent.headers[StratObject2PC_Multipass_Index].wField2];

    StratObject2PC_Multipass_Index+=1;
};
struct StratObject2PC_Multipass<auto Count>
{
    StratObject2PC_Multipass_Index = 0;
    StratObject2PC_Header headers[Count];
    StratObject2PC_Data_Multipass data[Count];
};

struct AnimTriggerPC
{
    u16 Frame;
    u16 Trigger;
};

struct AnimationStruct1_PC
{
    s16 wField0;
    s16 wField1;
    s16 wField2;
    s16 wField3;
};

struct FirstMorphBone
{
    Vector4<s16> morphs[parent.MorphCount];
    Matrix4x4F bones[parent.BoneCount];
};

struct MorphBone
{
    s16 morphs[parent.MorphCount];
    if((parent.MorphCount & 1) != 0)
    {
        s16;//Likely padding;
    }
    Matrix4x4F bones[parent.BoneCount];
};

struct AnimationStructPC_Singlepass
{
    //Header
    u32 triggerCount;
    u32 Triggers_Placeholder;
    u32 FrameCount;
    u32 Flag;//Should be 0 or 1
    u32 MorphCount;
    u32 Morphs_Placeholder;
    u32 BoneCount;
    u32 Bones_Placeholder;
    //Data
    AnimTriggerPC Tiggers[triggerCount];
    if(Flag == 0)
    {
        AnimationStruct1_PC PositionDeltas[FrameCount];
    }
    if(MorphCount!=0 && FrameCount!=0)
    {
        u32 EmptyMorphsArray[FrameCount];//Should be all 0s
    }
    if(FrameCount!=0)
    {
        u32 EmptyBonesArray[FrameCount];//Should be all 0s
        if(MorphCount > 0)
        {
            FirstMorphBone firstMorphBone;
            MorphBone morphBones[FrameCount-1];
        }
        else
        {
            MorphBone morphBones[FrameCount];
        }
    }
};

struct CutsceneInfoPC
{
    s32 MorphCount;
    s32 BoneCount;
};

struct Cutscene
{
    char fileExtension[4];
    u32 objCount;
    u32 InfoList_Placeholder;
    u32 ObjList_Placeholder;
    CutsceneInfoPC infoList[objCount];
    StratObject2PC_Multipass<objCount> objList;
};

struct ScriptPC
{
    u32 length;
    u32 data[length];
};

struct STPCChunk
{
    u32 modelCount;
    StratObject2PC_Singlepass models[modelCount];
    u32 animationCount;
    AnimationStructPC_Singlepass animations[animationCount];
    if(wadFlags.WAD_FLAG_HAS_CUTSCENES)
    {
        u32 cutsceneCount;
        Cutscene cutscenes[cutsceneCount];
    }
    
    u32 scriptCount;
    ScriptPC Scripts[scriptCount];
};

struct UnimplementedChunk<auto Size>
{
    u8 data[Size];
};

ChunkType lastChunkType = ChunkType::Unknown;
struct Chunk
{
    ChunkType type;
    u32 length;
    std::print("Reading Chunk: {} {}", type, length);
    match(type)
    {
        (ChunkType::ID_PC_INFO):INFOChunk chunk;
        (ChunkType::ID_PC_VERSION):VersionChunk chunk;
        (ChunkType::ID_PC_MAP):UnimplementedChunk<length> chunk;
        (ChunkType::ID_PC_TRACK):UnimplementedChunk<length> chunk;
        (ChunkType::ID_PC_TEXT):UnimplementedChunk<length> chunk;
        (ChunkType::ID_PC_LIGHT):UnimplementedChunk<length> chunk;
        (ChunkType::ID_PC_STRAT):STPCChunk chunk;
        (ChunkType::ID_PC_WADFLAGS):WadFlagsChunk chunk;
        (ChunkType::ID_END):ENDChunkPC chunk;
        (_):
        {
            if(lastChunkType == ChunkType::ID_PC_MAP)
            {
                std::print("Fixing MAP length");
                $ -= 4;//Unread the type
                $ -= 4;//Unread the length
                $ -= 4;//Move back 4 more to fix the offset issue
                continue;//Discard this one.
            }
            else
            {
                UnimplementedChunk<length> chunk;
            }
        }
    }
    lastChunkType = type;
    if(type == ChunkType::ID_END)
        break;
};


struct WadFile
{
    u32 wadLengthFaulty;
    Chunk chunks[while(true)];
};

WadFile wad @ 0x0;

// Enter your pattern code here and click on the Play button
// below to execute your code. The results will then be visible
// in the Pattern Data view.
// 
// More information can be found in the documentation.
// 
// 
// Simple example:
// 
// 
// 
// struct Pattern {
//     u32 int;
//     float floating_point;
// };
// 
// Pattern my_pattern @ 0x00;
// std::print("0x{:08X}", my_pattern.int);