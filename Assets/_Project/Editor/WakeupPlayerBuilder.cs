using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace SOLITUDE.Editor
{
    public static class WakeupPlayerBuilder
    {
        public static void BuildVerification()=>Build(true);
        public static void BuildShipping()=>Build(false);
        private static void Build(bool verification)
        {
            PhaseFiveFixtureBuilder.RestoreBuildScope();
            var manifest=WakeupValidation.LoadManifest();WakeupValidation.ValidateForCI();
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-buildOutput");
            if(index<0)throw new InvalidOperationException("Provide an explicit isolated build output.");string output=args[index+1];
            PlayerSettings.companyName="SOLITUDE Verification";PlayerSettings.productName=verification?"SolitudeVerification":"SolitudeShippingCheck";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone,verification?"com.solitude.verification.phase5":"com.solitude.verification.shippingcheck");
            UnityEditor.OSXStandalone.UserBuildSettings.architecture=UnityEditor.Build.OSArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,(ScriptingImplementation)Enum.Parse(typeof(ScriptingImplementation),manifest.backend));
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {scenes=manifest.scenes,target=BuildTarget.StandaloneOSX,locationPathName=output,options=verification?BuildOptions.Development:BuildOptions.None,extraScriptingDefines=verification?new[]{"SOLITUDE_VERIFICATION"}:Array.Empty<string>()});
            if(report.summary.result!=BuildResult.Succeeded)throw new BuildFailedException("Wakeup build failed: "+report.summary.result);
            foreach(var file in Directory.GetFiles(output,"*.dll",SearchOption.AllDirectories))
            {
                string name=Path.GetFileName(file);
                if(name.StartsWith("SOLITUDE.Tests")||name.StartsWith("nunit")||(!verification&&name.StartsWith("SOLITUDE.Verification")))throw new BuildFailedException("Shipping isolation failed: "+name);
            }
            Debug.Log("WAKEUP_BUILD_PASS "+(verification?"verification":"shipping")+" ARM64 "+report.summary.totalSize);
        }
    }
}
