using System;
using System.IO;
namespace SOLITUDE.SaveLoad
{
    public static class VerificationFaults
    {
#if SOLITUDE_VERIFICATION
        private static bool writeArmed;
        private static bool failRepairWrite;
        public static void ArmRepairWrite(bool failOnce = false) { writeArmed = true; failRepairWrite = failOnce; }
        public static void RepairBeforeCommit() => Hit("repair-before-commit");
        // Write scenarios target the smoke's explicit write, after normal startup saves.
        public static void ArmWrite() => writeArmed = true;
#endif
        internal static void Hit(string checkpoint)
        {
#if SOLITUDE_VERIFICATION
            var args=Environment.GetCommandLineArgs();
            if (failRepairWrite && checkpoint == "before-temp")
            { failRepairWrite = false; throw new IOException("Injected mechanical repair write failure."); }
            if (!writeArmed && Array.IndexOf(args, "-verifyMechanicalRepair") >= 0 && !checkpoint.StartsWith("repair-", StringComparison.Ordinal)) return;
            if (!writeArmed && (Array.IndexOf(args,"-faultWrite") >= 0 || Array.IndexOf(args,"-faultStartingLoadout") >= 0)
                && !checkpoint.StartsWith("loadout-", StringComparison.Ordinal)) return;
            int i=Array.IndexOf(args,"-solitudeCrashAt");
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
