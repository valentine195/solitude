using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
public static class PureTestRunner
{
    public static int Main()
    {
        int passed=0,failed=0;
        foreach(var type in Assembly.GetExecutingAssembly().GetTypes().Where(t=>t.Namespace=="SOLITUDE.Tests"))
        foreach(var method in type.GetMethods().Where(m=>m.IsDefined(typeof(TestAttribute))||m.IsDefined(typeof(TestCaseAttribute))))
        {
            var cases=method.GetCustomAttributes<TestCaseAttribute>().Select(c=>c.Arguments).ToArray();
            if(cases.Length==0)cases=new[]{Array.Empty<object>()};
            foreach(var args in cases)
            {
                object fixture=Activator.CreateInstance(type);
                try
                {
                    foreach(var setup in type.GetMethods().Where(m=>m.IsDefined(typeof(SetUpAttribute))))setup.Invoke(fixture,null);
                    method.Invoke(fixture,args);passed++;
                }
                catch(Exception error){failed++;Console.Error.WriteLine(type.Name+"."+method.Name+": "+(error.InnerException??error));}
                finally
                {
                    try {foreach(var teardown in type.GetMethods().Where(m=>m.IsDefined(typeof(TearDownAttribute))))teardown.Invoke(fixture,null);}
                    catch(Exception error){failed++;Console.Error.WriteLine("Teardown: "+error);}
                }
            }
        }
        Console.WriteLine($"Pure tests: {passed} passed, {failed} failed");return failed==0?0:1;
    }
}
