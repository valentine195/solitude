using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
namespace SOLITUDE.Art.Editor
{
    public static class OpeningArtImport
    {
        public const string Root = "Assets/_Project/Art/SOLITUDE";
        [Serializable] private sealed class ArtSpec { public string[] production_palette_hex; public int pixels_per_unit; }
        private static ArtSpec Spec => JsonUtility.FromJson<ArtSpec>(File.ReadAllText(Root+"/ArtDirection/art-spec.json"));
        private static Color32[] Palette => Spec.production_palette_hex.Select(h => { ColorUtility.TryParseHtmlString("#"+h,out var c); return (Color32)c; }).ToArray();
        [MenuItem("SOLITUDE/Art/Import Opening Props")]
        public static void Props()
        {
            var src = Load("Tools/ArtSources/SOLITUDE/props-source-v01.png");
            string[] names = {"cryopod","locker_closed","locker_open","intercom","battery","personal_shelf","clipboard"};
            int[,] sizes = {{32,64},{32,48},{40,48},{32,32},{16,24},{32,32},{16,24}};
            for (int i=0;i<names.Length;i++)
            {
                int col=i%4; bool top=i<4;
                // Generated sheet has a taller upper row; these are source regions, not assumed native pixels.
                RectInt region=new RectInt(Mathf.RoundToInt(col*src.width/4f), top ? Mathf.RoundToInt(src.height*.42f) : 0,
                    Mathf.RoundToInt(src.width/4f), top ? Mathf.RoundToInt(src.height*.58f) : Mathf.RoundToInt(src.height*.40f));
                WriteNormalized(src,region,sizes[i,0],sizes[i,1], Root+"/Props/solitude_"+names[i]+"_normal_v01.png");
            }
            UnityEngine.Object.DestroyImmediate(src);
            AssetDatabase.SaveAssets();
        }
        [MenuItem("SOLITUDE/Art/Import Opening Caretaker")]
        public static void Player()
        {
            var src=Load("Tools/ArtSources/SOLITUDE/caretaker-source-v01.png");
            var directions=new[]{"south","north","west","east"};
            for(int row=0;row<4;row++) for(int col=0;col<3;col++)
            {
                var region=new RectInt(Mathf.RoundToInt(col*src.width/3f),Mathf.RoundToInt((3-row)*src.height/4f),Mathf.FloorToInt(src.width/3f),Mathf.FloorToInt(src.height/4f));
                WriteNormalized(src,region,32,48,Root+$"/Characters/solitude_caretaker_{directions[row]}_{col}_v01.png");
            }
            UnityEngine.Object.DestroyImmediate(src);
            BuildController();
        }
        [MenuItem("SOLITUDE/Art/Import Quiet Floor")]
        public static void Floor()
        {
            var src=Load("Tools/ArtSources/SOLITUDE/floor-source-v01.png");
            string path=Root+"/Tiles/solitude_floor_quiet_normal_v01.png";
            WriteNormalized(src,new RectInt(0,0,src.width,src.height),32,32,path,true);
            UnityEngine.Object.DestroyImmediate(src);
            string tilePath=Root+"/Tiles/solitude_floor_quiet_normal_v01.asset";
            var tile=AssetDatabase.LoadAssetAtPath<UnityEngine.Tilemaps.Tile>(tilePath);
            if(tile==null){tile=ScriptableObject.CreateInstance<UnityEngine.Tilemaps.Tile>();AssetDatabase.CreateAsset(tile,tilePath);}
            tile.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);tile.colliderType=UnityEngine.Tilemaps.Tile.ColliderType.None;EditorUtility.SetDirty(tile);AssetDatabase.SaveAssets();
        }
        static Texture2D Load(string path) { var t=new Texture2D(2,2,TextureFormat.RGBA32,false); if(!t.LoadImage(File.ReadAllBytes(path))) throw new InvalidOperationException(path); return t; }
        static void WriteNormalized(Texture2D source,RectInt region,int width,int height,string path,bool tile=false)
        {
            var p=source.GetPixels32(); int minX=source.width,minY=source.height,maxX=0,maxY=0;
            for(int y=region.yMin;y<Math.Min(region.yMax,source.height);y++) for(int x=region.xMin;x<Math.Min(region.xMax,source.width);x++)
                if(p[y*source.width+x].a>=192){minX=Math.Min(x,minX);maxX=Math.Max(x,maxX);minY=Math.Min(y,minY);maxY=Math.Max(y,maxY);}
            if(maxX<minX) throw new InvalidOperationException("Empty source region: "+path);
            int sw=maxX-minX+1,sh=maxY-minY+1;
            float scale=Math.Min((width-(tile?0:2f))/sw,(height-(tile?0:2f))/sh);
            int dw=Math.Max(1,Mathf.RoundToInt(sw*scale)),dh=Math.Max(1,Mathf.RoundToInt(sh*scale));
            int ox=(width-dw)/2,oy=0;
            var pixels=new Color32[width*height];var palette=Palette;
            for(int y=0;y<dh;y++) for(int x=0;x<dw;x++)
            {
                int sx=minX+Math.Min(sw-1,(int)((x+.5f)*sw/dw)),sy=minY+Math.Min(sh-1,(int)((y+.5f)*sh/dh));
                var c=p[sy*source.width+sx];if(c.a<192)continue;
                int best=0,dist=int.MaxValue;
                for(int k=0;k<palette.Length;k++){int dr=c.r-palette[k].r,dg=c.g-palette[k].g,db=c.b-palette[k].b;int d=dr*dr+dg*dg+db*db;if(d<dist){dist=d;best=k;}}
                pixels[(y+oy)*width+x+ox]=palette[best];
            }
            var dst=new Texture2D(width,height,TextureFormat.RGBA32,false);dst.SetPixels32(pixels);dst.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,dst.EncodeToPNG());UnityEngine.Object.DestroyImmediate(dst);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Single;
            imp.spritePixelsPerUnit=Spec.pixels_per_unit;imp.filterMode=FilterMode.Point;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.mipmapEnabled=false;
            imp.npotScale=TextureImporterNPOTScale.None;imp.alphaIsTransparency=true;
            var settings=new TextureImporterSettings();imp.ReadTextureSettings(settings);settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spritePivot=new Vector2(.5f,tile?.5f:0);settings.spriteMeshType=SpriteMeshType.FullRect;imp.SetTextureSettings(settings);imp.SaveAndReimport();
        }
        public static Sprite Sprite(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Props/solitude_"+name+"_normal_v01.png");
        static void BuildController()
        {
            string path=Root+"/Characters/OpeningCaretaker.controller";
            if(AssetDatabase.LoadAssetAtPath<AnimatorController>(path)!=null) throw new InvalidOperationException("Controller already exists; preserve it and review before regenerating.");
            var ac=AnimatorController.CreateAnimatorControllerAtPath(path);ac.AddParameter("MoveX",AnimatorControllerParameterType.Float);ac.AddParameter("MoveY",AnimatorControllerParameterType.Float);ac.AddParameter("IsMoving",AnimatorControllerParameterType.Bool);
            var sm=ac.layers[0].stateMachine;var dirs=new[]{"south","north","west","east"};
            for(int i=0;i<4;i++)
            {
                var sprites=Enumerable.Range(0,3).Select(n=>AssetDatabase.LoadAssetAtPath<Sprite>(Root+$"/Characters/solitude_caretaker_{dirs[i]}_{n}_v01.png")).ToArray();
                AnimatorState idle=sm.AddState("Idle_"+dirs[i]), walk=sm.AddState("Walk_"+dirs[i]);
                idle.motion=Clip("Idle_"+dirs[i],new[]{sprites[0],sprites[0]},new[]{0f,1f});
                walk.motion=Clip("Walk_"+dirs[i],new[]{sprites[0],sprites[1],sprites[0],sprites[2],sprites[0]},new[]{0f,.12f,.24f,.36f,.48f});
                if(i==0)sm.defaultState=idle;
                var stop=walk.AddTransition(idle);stop.hasExitTime=false;stop.duration=0;stop.AddCondition(AnimatorConditionMode.IfNot,0,"IsMoving");
                var move=sm.AddAnyStateTransition(walk);move.hasExitTime=false;move.duration=0;move.canTransitionToSelf=false;move.AddCondition(AnimatorConditionMode.If,0,"IsMoving");
                move.AddCondition(i==0||i==2?AnimatorConditionMode.Less:AnimatorConditionMode.Greater,i==0||i==2?-.1f:.1f,i<2?"MoveY":"MoveX");
            }
            EditorUtility.SetDirty(ac);AssetDatabase.SaveAssets();
        }
        static AnimationClip Clip(string name,Sprite[] sprites,float[] times)
        {
            var c=new AnimationClip{frameRate=12,name=name};var settings=AnimationUtility.GetAnimationClipSettings(c);settings.loopTime=true;AnimationUtility.SetAnimationClipSettings(c,settings);
            AnimationUtility.SetObjectReferenceCurve(c,new EditorCurveBinding{path="",type=typeof(SpriteRenderer),propertyName="m_Sprite"},sprites.Select((s,i)=>new ObjectReferenceKeyframe{time=times[i],value=s}).ToArray());
            AssetDatabase.CreateAsset(c,Root+"/Characters/"+name+".anim");return c;
        }
    }
}
