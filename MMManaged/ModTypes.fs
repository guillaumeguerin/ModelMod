﻿namespace ModelMod

open System.Runtime.InteropServices

open SharpDX.Direct3D9 

open Types

type SDXVertexElement = SharpDX.Direct3D9.VertexElement
type SDXVertexDeclUsage = SharpDX.Direct3D9.DeclarationUsage
type SDXVertexDeclType = SharpDX.Direct3D9.DeclarationType

module ModTypes =
    // ------------------------------------------------------------------------
    // These are the core mesh types

    type MeshType = GPUReplacement | CPUReplacement | Deletion | Reference 

    type VTNIndex = { V: int; T: int; N: int }

    type IndexedTri = {
        Verts: VTNIndex[] // 3 elements long, where each element contains int indexes into position, texture, etc.
    }

    // A vertex declaration may not be present.  If present, both the raw bytes 
    // and an unpacked list of elements are available.
    type VertexDeclarationData = byte[] * SDXVertexElement list

    type BinaryVertexData = {
        NumVerts: uint32
        Stride: uint32
        Data: byte[]
    }

    type Mesh = {
        Type : MeshType
        Triangles : IndexedTri[]
        Positions: Vec3F[]
        UVs: Vec2F[]
        Normals: Vec3F[]
        BlendIndices: Vec4X[]
        BlendWeights: Vec4F[]
        Declaration : VertexDeclarationData option
        BinaryVertexData: BinaryVertexData option
        AnnotatedVertexGroups: string list []
        AppliedPositionTransforms: string []
        AppliedUVTransforms: string[]
        TexturePath: string option
    }

    // ------------------------------------------------------------------------
    // These are types loaded by the moddb from yaml files

    // This is the root of all yaml-based types.  
    type IThing = interface end

    type IReference =
        inherit IThing
        abstract member Name : string
        abstract member Mesh : Mesh

    type GeomDeletion = { PrimCount: int; VertCount: int }

    type ModAttributes = {
        DeletedGeometry: GeomDeletion list
    }

    let EmptyModAttributes = { ModAttributes.DeletedGeometry = [] }

    type IMod =
        inherit IThing
        abstract member RefName: string option
        abstract member Ref: IReference option
        abstract member Name: string
        abstract member Mesh: Mesh option
        abstract member Attributes: ModAttributes

#nowarn "9"
// ----------------------------------------------------------------------------
// These are types that are passed back to native land  
module InteropTypes =
    // the use of multibyte could be a problem here if we need to marshal strings containing unicode characters (i18n paths for example),
    // but currently the unmanaged code doesn't need to know about paths other than the MM install dir, which it already knows.
    [<StructLayout(LayoutKind.Sequential, Pack=8, CharSet=CharSet.Ansi)>] 
    type ConfData = {
        [<MarshalAs(UnmanagedType.I1)>]
        RunModeFull: bool
        [<MarshalAs(UnmanagedType.ByValTStr, SizeConst=512)>]
        InputProfile: string
    }

    let DefaultConf = { 
        ConfData.RunModeFull = true;
        InputProfile = "";
    }

    [<StructLayout(LayoutKind.Sequential, Pack=8)>]
    type ModData = {
        modType: int 
        primType: int
        vertCount: int
        primCount: int
        indexCount: int
        refVertCount: int
        refPrimCount: int
        declSizeBytes: int
        vertSizeBytes: int
        indexElemSizeBytes: int
    }
    
    [<StructLayout(LayoutKind.Sequential, Pack=8)>]
    type SnapshotData = {
        primType: int32
        baseVertexIndex: int32
        minVertexIndex: uint32
        numVertices: uint32
        startIndex: uint32
        primCount: uint32 

        vertDecl:nativeint
        ib:nativeint
    }

    type GetModCountCB = delegate of unit -> int 
    type GetModDataCB = delegate of int -> ModData
    type FillModDataCB = 
        delegate of 
            modIndex:int *
            declData:nativeptr<byte> *
            declSize:int32 *
            vbData:nativeptr<byte> *
            vbSize:int32 *
            ibData:nativeptr<byte> *
            ibSize:int32 -> int
            
    type TakeSnapshotCB = 
        delegate of 
            device: nativeint *
            snapData: SnapshotData -> int
