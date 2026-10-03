using System;
using System.IO;
namespace SOLITUDE.SaveLoad
{
    internal static class VerificationFaults
    {
        internal static void Hit(string checkpoint)
        {
#if SOLITUDE_VERIFICATION
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-solitudeCrashAt");
            if(i<0||i+1>=args.Length||args[i+1]!=checkpoint)return;
            int location=Array.IndexOf(args,"-solitudeSaveDirectory");
            if(location<0)throw new InvalidOperationException("Fault injection requires an isolated save directory.");
            using(var file=new FileStream(Path.Combine(args[location+1],"checkpoint.txt"),FileMode.Create,FileAccess.Write))
            {var bytes=System.Text.Encoding.UTF8.GetBytes(checkpoint);file.Write(bytes,0,bytes.Length);file.Flush(true);}
            System.Diagnostics.Process.GetCurrentProcess().Kill();
#endif
        }
    }
}
