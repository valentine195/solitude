using System;
using System.Collections;
using System.IO;
using System.Linq;
using SOLITUDE.Composition;
using SOLITUDE.Player;
using SOLITUDE.Player.UI;
using SOLITUDE.Vitals;
using TMPro;
using UnityEngine;

namespace SOLITUDE.Verification
{
    /// <summary>Opt-in native rendering qualification; excluded from shipping by the assembly define.</summary>
    public sealed class PlayerVitalsVerification : MonoBehaviour
    {
        [Serializable] private sealed class Capture
        {
            public string state, file, reason, text, detail;
            public int width, height;
            public float health, hunger, oxygen, canvasScale, preferredTextWidth, textRectWidth;
        }
        [Serializable] private sealed class Result
        {
            public bool success;
            public string error;
            public Capture[] captures;
        }
        private static string Argument(string name)
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,name);
            return i>=0&&i+1<args.Length?args[i+1]:null;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] private static void Boot()
        {
            if(UnityEngine.Application.isEditor||Argument("-verifyVitalsCapture")==null)return;
            var go=new GameObject("PlayerVitalsVerification");DontDestroyOnLoad(go);go.AddComponent<PlayerVitalsVerification>();
        }
        private IEnumerator Start()
        {
            // The normal verification scenario yields two startup frames. This opt-in
            // qualification owns the process instead, without changing production composition.
            var smoke=FindFirstObjectByType<global::SolitudeCompositionSmoke>();if(smoke!=null)Destroy(smoke.gameObject);
            var directory=Argument("-verifyVitalsCapture");Directory.CreateDirectory(directory);
            UnityEngine.Application.targetFrameRate=60;UnityEngine.Application.runInBackground=true;
            var captures=new System.Collections.Generic.List<Capture>();
            yield return null;yield return null;
            var root=FindFirstObjectByType<GameCompositionRoot>();var player=FindFirstObjectByType<PlayerVitalsController>();
            if(root==null||root.State!=RuntimeStartupState.Ready||player==null||!player.GameplayEligible)
            { Fail(directory,"Wakeup is not composed and ready");yield break; }
            foreach(int width in new[]{1920,1280})
            {
                int height=width==1920?1080:720;Screen.SetResolution(width,height,FullScreenMode.Windowed);
                yield return new WaitForSecondsRealtime(.3f);yield return null;
                if(Screen.width!=width||Screen.height!=height){Fail(directory,"Unsupported native viewport "+Screen.width+"x"+Screen.height);yield break;}
                foreach(string state in new[]{"full","delayed","recovering","hungry","boundary"})
                {
                    player.Configure(new VitalsRecoverySettings());player.ResetForAwakening();
                    if(state=="delayed"){player.Vitals.Health.Set(40);player.Damage(10);}
                    if(state=="recovering")player.Vitals.Health.Set(30);
                    if(state=="hungry"){player.Vitals.Health.Set(30);player.Vitals.Hunger.Set(10);}
                    if(state=="boundary")player.Vitals.Health.Set(50);
                    yield return null;yield return new WaitForEndOfFrame();
                    var status=FindFirstObjectByType<VitalRecoveryStatusView>().GetComponent<TMP_Text>();
                    string expected=state=="delayed"?"Recovery delayed":state=="recovering"?"Recovering":state=="hungry"?"HUNGRY":"";
                    if(status.text!=expected){Fail(directory,"Unexpected "+state+" text: "+status.text);yield break;}
                    var detail=status.transform.Find("RecoveryDetail").GetComponent<TMP_Text>();
                    if(detail.text!=(state=="hungry"?"Recovery paused":"")){Fail(directory,"Unexpected recovery detail: "+detail.text);yield break;}
                    var scaler=status.GetComponentInParent<UnityEngine.UI.CanvasScaler>();
                    var file=Path.Combine(directory,state+"-"+width+".png");
                    var frame=ScreenCapture.CaptureScreenshotAsTexture();
                    try { File.WriteAllBytes(file,frame.EncodeToPNG()); }
                    finally { Destroy(frame); }
                    captures.Add(new Capture{state=state,file=file,width=Screen.width,height=Screen.height,health=player.Vitals.Health.Current,hunger=player.Vitals.Hunger.Current,oxygen=player.Vitals.Oxygen.Current,reason=player.RecoveryStatus.ToString(),text=status.text,detail=detail.text,canvasScale=scaler.GetComponent<Canvas>().scaleFactor,preferredTextWidth=status.GetPreferredValues(status.text).x,textRectWidth=status.rectTransform.rect.width});
                    yield return null;yield return new WaitForSecondsRealtime(.1f);
                    if(!File.Exists(file)){Fail(directory,"Screenshot not written: "+file);yield break;}
                }
            }
            File.WriteAllText(Path.Combine(directory,"result.json"),JsonUtility.ToJson(new Result{success=true,captures=captures.ToArray()},true));
            UnityEngine.Application.Quit(0);
        }
        private static void Fail(string directory,string error)
        {
            File.WriteAllText(Path.Combine(directory,"result.json"),JsonUtility.ToJson(new Result{success=false,error=error},true));
            Debug.LogError(error);UnityEngine.Application.Quit(1);
        }
    }
}
