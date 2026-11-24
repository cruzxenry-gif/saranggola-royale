using System;
using System.IO;
using System.Text;
using Saranggola.Sim;
using UnityEngine;

public static class BalanceSim
{
    public static void Run()
    {
        var args=Environment.GetCommandLineArgs();
        int count=int.Parse(Get(args,"-matches","200"));
        int seed=int.Parse(Get(args,"-seed","1"));
        if (count<1) throw new ArgumentOutOfRangeException(nameof(count));
        string report=Get(args,"-report", Path.GetFullPath(Path.Combine(Application.dataPath,"..","..","balance_report.md")));
        string text=BalanceBench.Run(count,seed,"Unity batchmode "+Application.unityVersion,i=>Debug.Log($"Balance matches {i}/{count}"));
        File.WriteAllText(report,text,new UTF8Encoding(false));
        Debug.Log("Balance report at "+report);
    }

    private static string Get(string[] args,string flag,string fallback)
    {
        for(int i=0;i+1<args.Length;i++)if(args[i]==flag)return args[i+1];
        return fallback;
    }
}
