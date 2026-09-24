using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using ArrobAMO;
class BrowserExtrasSelfTest {
 static int failures;
 static void Assert(bool condition,string name){Console.WriteLine((condition?"PASS ":"FAIL ")+name);if(!condition)failures++;}
 static object Field(object o,string name){return o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);}
 static object Call(object o,string name,params object[] args){return o.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,args);}
 [STAThread] static int Main(){
  var file=Path.Combine(Path.GetTempPath(),"arrobamo-051-settings-"+Guid.NewGuid().ToString("N")+".ini");
  try {
   var settings=new AmoSettings();
   settings.MicrophoneEnabled=false; settings.CameraEnabled=false;
   settings.Save(file);
   var loaded=AmoSettings.Load(file);
   Assert(!loaded.MicrophoneEnabled && !loaded.CameraEnabled,"Se guardan los permisos MIC/CAM");
   var defaults=AmoSettings.Load(file+".absent");
   Assert(defaults.MicrophoneEnabled && defaults.CameraEnabled,"Permisos por defecto explícitos");
   using(var app=new BrowserForm("arrobamo://ayudamo",null,false,false,false,false,false,true)){
    string help=(string)Call(app,"GetHelpHtml");
    Assert(help.Contains("/ayudAMO") && help.Contains("/help"),"Ayuda interna incluye ambos alias");
    Assert(help.Contains("Micrófono") && help.Contains("Wi-Fi"),"Ayuda documenta permisos y red");
    var mic=(Label)Field(app,"microphoneLabel");
    var cam=(Label)Field(app,"cameraLabel");
    Assert(mic.Cursor==Cursors.Hand && cam.Cursor==Cursors.Hand,"MIC y CAM son clicables");
    var net=(Label)Field(app,"internetLabel");
    Assert(net.Cursor==Cursors.Hand,"Indicador de red tiene acciones");
   }
  } catch(Exception ex){Console.WriteLine("ERROR "+ex);failures++;}
  finally {if(File.Exists(file))File.Delete(file);}
  Console.WriteLine("FAILS="+failures);
  return failures==0?0:1;
 }
}
