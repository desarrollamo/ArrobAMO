using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using ArrobAMO;
class BrowserUiSecuritySelfTest {
 static int pass,fail;
 static void Check(bool x,string name){Console.WriteLine((x?"PASS ":"FAIL ")+name);if(x)pass++;else fail++;}
 static T Get<T>(object o,string name) {return (T)o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);}
 static object Call(object o,string name,params object[] args){return o.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,args);}
 [STAThread]static void Main(){
  Application.EnableVisualStyles();
  var app=new BrowserForm("arrobamo://inicio",null,false,false,false,false,false,true);
  app.ShowInTaskbar=false;app.StartPosition=FormStartPosition.Manual;app.Location=new Point(-1800,-1400);
  app.Shown+=async delegate {try{await Exercise(app);}catch(Exception e){Console.WriteLine("ERROR "+e);fail++;}finally{Console.WriteLine("TOTAL PASS="+pass+" FAIL="+fail);Environment.ExitCode=fail==0?0:1;app.Close();}};
  Application.Run(app);
 }
 static async Task Exercise(BrowserForm app){
  await Task.Delay(2200);
  var tabs=Get<TabControl>(app,"tabs");
  var address=Get<TextBox>(app,"address");
  Check(tabs.TabCount==1,"Inicio: se abre exactamente una pestaña");
  Check(address.Text.Contains("inicio"),"Inicio: barra de direcciones coherente");
  var brand=Get<Control>(app,"brand");
  Check(brand.Controls.OfType<Label>().Count()>=9,"Barra de métricas y red creada");
  foreach(var name in new[]{"plus","back","forward","reload","home","bookmarkButton","profileButton","menuButton","settingsButton","downloadsButton","scriptsButton","loopButton","teamButton","securityButton","uiKitButton","aiButton"}){
   var btn=Get<Button>(app,name);
   Check(btn!=null&&btn.IsHandleCreated&&(btn.Enabled||name=="back"||name=="forward"),"Control cableado "+name);
  }
  foreach(var name in new[]{"internetLabel","wifiLabel","ethernetLabel","bluetoothLabel","microphoneLabel","cameraLabel"}){
   var label=Get<Label>(app,name);Check(label!=null&&label.Cursor==Cursors.Hand,"Clic derecho/clic "+name);
  }
  var ai=Get<ContextMenuStrip>(app,"aiMenu");
  Check(ai.Items.Cast<ToolStripItem>().Any(i=>i.Text=="ChatGPT"),"IA: ChatGPT accesible");
  Check(ai.Items.Cast<ToolStripItem>().Any(i=>i.Text=="Gemini"),"IA: Gemini accesible");
  Check(ai.Items.Cast<ToolStripItem>().Any(i=>i.Text=="Claude"),"IA: Claude accesible");
  Check(ai.Items.Cast<ToolStripItem>().Any(i=>i.Text=="Microsoft Copilot"),"IA: Copilot accesible");
  Check(ai.Items.Cast<ToolStripItem>().Any(i=>i.Text=="Perplexity"),"IA: Perplexity accesible");
  var scripts=Get<ContextMenuStrip>(app,"scriptsMenu");
  Check(scripts.Items.Cast<ToolStripItem>().Any(i=>i.Text.Contains("predeterminados")),"Menú scripts predeterminados");
  var main=Get<ContextMenuStrip>(app,"mainMenu");
  Check(main.Items.Cast<ToolStripItem>().Any(i=>i.Text.Contains("help")),"Menú de ayuda");
  Check(main.Items.Cast<ToolStripItem>().Any(i=>i.Text.Contains("conexiones")),"Menú de conexiones");
  var plus=Get<Button>(app,"plus");plus.PerformClick();await Task.Delay(1200);
  Check(tabs.TabCount==2,"Clic en + crea pestaña");
  Call(app,"Navigate","/ayudAMO");await Task.Delay(650);
  Check(address.Text.Contains("ayudamo"),"Barra /ayudAMO resuelta");
  Check(tabs.SelectedTab.Text.IndexOf("AyudAMO",StringComparison.OrdinalIgnoreCase)>=0,"AyudAMO renderizado");
  Call(app,"Navigate","/help");await Task.Delay(650);
  Check(address.Text.Contains("ayudamo"),"Alias /help funcional");
  var mic=Get<Label>(app,"microphoneLabel");var cam=Get<Label>(app,"cameraLabel");
  var security=Get<AmoSecurityManager>(app,"securityManager");
  Check(security.MicrophoneEnabled&&security.CameraEnabled,"Permisos privados iniciales habilitados");
  Call(app,"ToggleDevice",true);Call(app,"ToggleDevice",false);await Task.Delay(500);
  Check(!security.MicrophoneEnabled&&!security.CameraEnabled,"Acciones MIC/CAM bloquean nuevas solicitudes");
  Check(mic.Text.Contains("BLOQ")&&cam.Text.Contains("BLOQ"),"Estado visual MIC/CAM bloqueado");
  Call(app,"ToggleDevice",true);Call(app,"ToggleDevice",false);await Task.Delay(500);
  Check(security.MicrophoneEnabled&&security.CameraEnabled,"Acciones MIC/CAM habilitan");
  Check(mic.Text.Contains("PERM")&&cam.Text.Contains("PERM"),"Estado visual MIC/CAM permitido");
  Call(app,"Navigate","arrobamo://inicio");await Task.Delay(600);
  Check(address.Text.Contains("inicio"),"Inicio vuelve desde ayuda");
  var web=tabs.SelectedTab.Controls.OfType<WebView2>().First();
  Check(web.CoreWebView2!=null,"Motor WebView2 creado");
  string token=Get<string>(app,"internalMessageToken");
  await web.ExecuteScriptAsync("chrome.webview.postMessage('AMO|"+token+"|HELP')");await Task.Delay(700);
  Check(address.Text.Contains("ayudamo"),"AyudAMO: mensaje interno autorizado");
  Call(app,"Navigate","arrobamo://inicio");await Task.Delay(800);

  // Origen aislado: una página arbitraria podría intentar mandar órdenes internas.
  Call(app,"Navigate","https://example.com/");await Task.Delay(1900);
  Check(web.Source!=null&&web.Source.Host=="example.com","Contexto de sitio público separado del inicio interno");
  await web.ExecuteScriptAsync("chrome.webview.postMessage('AMO|HELP')");await Task.Delay(900);
  Check(!address.Text.Contains("ayudamo"),"Seguridad: mensajes AMO de página sin confianza ignorados");
  Check(web.CanGoBack,"Volver habilitado luego de abrir una URL externa");
  web.GoBack();await Task.Delay(1100);
  string visible=(await web.ExecuteScriptAsync("document.getElementById('q') !== null")).Trim();
  Check(visible=="true","Volver recupera la búsqueda de Inicio");
  if(visible=="true"){
    await web.ExecuteScriptAsync("document.getElementById('q').value='https://example.org/';document.querySelector('form.search').requestSubmit()");
    await Task.Delay(1400);
    Check(web.Source!=null&&web.Source.Host=="example.org","Botón Ir funciona otra vez luego de Volver");
  }
  var device=Get<Label>(app,"deviceLabel");
  Check(device.Text.Contains(Environment.MachineName),"El estado muestra nombre del equipo real");
  var sidebar=Get<Panel>(app,"side");
  var kit=Get<Button>(app,"uiKitButton");
  int before=Application.OpenForms.Cast<Form>().Count(f=>f.Text.Contains("UI Kit"));
  kit.PerformClick();await Task.Delay(250);
  var kitForms=Application.OpenForms.Cast<Form>().Where(f=>f.Text.Contains("UI Kit")).ToArray();
  Check(kitForms.Length-before==1,"UI Kit de barra lateral abre exactamente una ventana");
  foreach(var f in kitForms)f.Close();
  Check(sidebar.Controls.OfType<Button>().Count(b=>b==kit)==1,"UI Kit no se duplica en la barra lateral");
  var internet=Get<Label>(app,"internetLabel");
  var invoke=typeof(BrowserForm).GetMethod("ConnectionMouseUp",BindingFlags.NonPublic|BindingFlags.Instance);
  invoke.Invoke(app,new object[]{internet,new MouseEventArgs(MouseButtons.Right,1,4,4,0)});
  await Task.Delay(180);
  SendKeys.SendWait("{ESC}");await Task.Delay(200);
  Check(!app.IsDisposed,"Menú de conexiones abre y se cierra sin cerrar el navegador");
  Console.WriteLine("FINAL_ADDRESS="+address.Text);
 }
}
