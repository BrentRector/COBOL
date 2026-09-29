using System; using System.Reflection; using CobolNet.Runtime; using CobolNet.Runtime.IO;
class T : CobolObject {
  public readonly string K = CobolFile.MintInstanceKey("OFILE");
  public T(){ CobolFile.Register(K, "hgc.dat", 3, false, false); __TrackInstanceFile(K); }
  public static string Probe;
}
class Spy : CobolObject { public Spy(){ GC.ReRegisterForFinalize(this);} ~Spy(){ 
  var f = typeof(RunUnit).GetField("_current", BindingFlags.NonPublic|BindingFlags.Static);
  var al = f.GetValue(null); var v = al.GetType().GetProperty("Value").GetValue(al);
  Console.WriteLine("finalizer-thread ambient: " + (v==null?"NULL":(ReferenceEquals(v,P.MainRU)?"SAME as main run unit":"a DIFFERENT (orphan) RunUnit")) + "; orphan pending=" + (v==null?"-":P.Q((RunUnit)v).ToString())); } }
static class P {
  public static RunUnit MainRU; public static object Q(RunUnit ru){ var fr = ru.Files; var q = typeof(FileRegistry).GetField("_pendingObjectClose", BindingFlags.NonPublic|BindingFlags.Instance).GetValue(fr); return q.GetType().GetProperty("Count").GetValue(q); }
  static void Make(){ var t = new T(); new Spy(); }
  static void Main(){
    CobolFile.Init(); var main = RunUnit.Current; MainRU = main;
    Make();
    GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
    Console.WriteLine("main-run-unit pending closes after GC = " + Q(main));
  }
}
