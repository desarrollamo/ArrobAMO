using System;
using System.Drawing;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
namespace ArrobAMO {
 public sealed partial class BrowserForm {
  readonly Label internetLabel=new Label(),wifiLabel=new Label(),ethernetLabel=new Label(),bluetoothLabel=new Label();
  readonly Timer networkTimer=new Timer();
  bool networkRefreshing;
  string networkDetails="Sin medición todavía.";
  void SetupNetworkLabels() {
   AddNetworkLabel(internetLabel,"NET: ...",638,120);
   AddNetworkLabel(wifiLabel,"WIFI: ...",762,85);
   AddNetworkLabel(ethernetLabel,"ETH: ...",851,99);
   AddNetworkLabel(bluetoothLabel,"BT: ...",954,87);
   networkTimer.Interval=12000;
   networkTimer.Tick+=async delegate { await RefreshNetwork(); };
   foreach(var label in new[]{internetLabel,wifiLabel,ethernetLabel,bluetoothLabel}) {
    label.MouseUp+=ConnectionMouseUp;
    tooltips.SetToolTip(label,"Clic: estado y adaptadores. Clic derecho: ajustes de Windows.");
   }
   UpdateNetworkVisibility();
  }
  void AddNetworkLabel(Label label,string title,int left,int width) {
   label.Text=title;label.Left=left;label.Width=width;label.Top=11;label.Height=19;
   label.Font=AmoTheme.UI(8f,FontStyle.Bold);label.ForeColor=AmoTheme.TextMuted;
   label.Cursor=Cursors.Hand;brand.Controls.Add(label);
  }
  void UpdateNetworkVisibility(){
   bool full=ClientSize.Width>=1280;
   internetLabel.Visible=ClientSize.Width>=1100;
   wifiLabel.Visible=full;ethernetLabel.Visible=full;bluetoothLabel.Visible=full;
   deviceLabel.Visible=ClientSize.Width>=1350;
  }
  sealed class NetworkSnapshot {
   public bool Wifi,Eth,Bt,Any,Internet;
   public string Details="";
  }
  static NetworkSnapshot MeasureNetwork(){
   var s=new NetworkSnapshot();var lines=new StringBuilder();
   foreach(var nic in NetworkInterface.GetAllNetworkInterfaces()){
    if(nic.NetworkInterfaceType==NetworkInterfaceType.Loopback || nic.NetworkInterfaceType==NetworkInterfaceType.Tunnel)continue;
    bool up=nic.OperationalStatus==OperationalStatus.Up;
    bool bt=nic.Name.IndexOf("Bluetooth",StringComparison.OrdinalIgnoreCase)>=0 ||
      nic.Description.IndexOf("Bluetooth",StringComparison.OrdinalIgnoreCase)>=0;
    s.Any |= up;
    if(nic.NetworkInterfaceType==NetworkInterfaceType.Wireless80211)s.Wifi |= up;
    if(nic.NetworkInterfaceType==NetworkInterfaceType.Ethernet || nic.NetworkInterfaceType==NetworkInterfaceType.GigabitEthernet)s.Eth |= up;
    if(bt)s.Bt |= up;
    lines.AppendLine(nic.Name+": "+(up?"conectado":"sin enlace")+" ("+nic.NetworkInterfaceType+")");
   }
   s.Details=lines.ToString();
   if(s.Any)try {
    using(var socket=new TcpClient()){
     var pending=socket.BeginConnect("1.1.1.1",443,null,null);
     try {if(pending.AsyncWaitHandle.WaitOne(1500)){socket.EndConnect(pending);s.Internet=socket.Connected;}}
     finally {pending.AsyncWaitHandle.Close();}
    }
   }catch {}
   return s;
  }
  async Task RefreshNetwork(){
   if(networkRefreshing || IsDisposed)return;
   networkRefreshing=true;
   try {
    var s=await Task.Run(()=>MeasureNetwork());
    if(IsDisposed)return;
    internetLabel.Text=s.Internet?"NET: ONLINE":s.Any?"NET: SIN VERIF.":"NET: SIN RED";
    internetLabel.ForeColor=s.Internet?AmoTheme.Success:s.Any?AmoTheme.Warning:AmoTheme.Danger;
    wifiLabel.Text=s.Wifi?"WIFI: ON":"WIFI: OFF";
    ethernetLabel.Text=s.Eth?"ETH: ON":"ETH: OFF";
    bluetoothLabel.Text=s.Bt?"BT PAN: ON":"BT PAN: --";
    wifiLabel.ForeColor=s.Wifi?AmoTheme.Success:AmoTheme.TextMuted;
    ethernetLabel.ForeColor=s.Eth?AmoTheme.Success:AmoTheme.TextMuted;
    bluetoothLabel.ForeColor=s.Bt?AmoTheme.Success:AmoTheme.TextMuted;
    networkDetails="Internet: "+(s.Internet?"TCP confirmado":s.Any?"No se verificó Internet":"Sin red detectada")+
      "\nWi-Fi: "+(s.Wifi?"enlace activo":"sin enlace detectado")+
      "\nEthernet: "+(s.Eth?"enlace activo":"sin enlace detectado")+
      "\nBluetooth PAN: "+(s.Bt?"enlace activo":"sin enlace detectado; no indica si el radio está encendido")+
      "\n\nAdaptadores de Windows:\n"+s.Details+
      "\nEl estado de Internet se comprueba con una conexión TCP; no garantiza que todos los sitios respondan.";
   } catch(Exception e) {networkDetails="No se pudo consultar la red: "+e.Message;internetLabel.Text="NET: ?";}
   finally {networkRefreshing=false;}
  }
  void ShowConnectivity(){MessageBox.Show(this,networkDetails+"\n\n"+ReadHardwareInformation(),"Conexiones · ArrobAMO",MessageBoxButtons.OK,MessageBoxIcon.Information);}
  void ConnectionMouseUp(object sender,MouseEventArgs e){
   if(e.Button==MouseButtons.Left){ShowConnectivity();return;}
   if(e.Button!=MouseButtons.Right)return;
   var menu=new ContextMenuStrip();AmoTheme.StyleMenu(menu);
   menu.Items.Add("Ver estado y adaptadores",null,delegate {ShowConnectivity();});
   menu.Items.Add("Configuración Wi-Fi",null,delegate {OpenWindowsSettings("ms-settings:network-wifi");});
   menu.Items.Add("Configuración Ethernet",null,delegate {OpenWindowsSettings("ms-settings:network-ethernet");});
   menu.Items.Add("Configuración Bluetooth",null,delegate {OpenWindowsSettings("ms-settings:bluetooth");});
   menu.Items.Add("Actualizar ahora",null,async delegate {await RefreshNetwork();});
   menu.Show((Control)sender,new Point(0,20));
  }
  void OpenWindowsSettings(string uri){
   try {Process.Start(new ProcessStartInfo(uri){UseShellExecute=true});}
   catch {ShowToast("No se pudo abrir Configuración de Windows.",AmoTheme.Warning);}
  }
  async void DeviceMouseUp(object sender,MouseEventArgs e){
   bool mic=ReferenceEquals(sender,microphoneLabel);
   if(e.Button==MouseButtons.Right){
    var menu=new ContextMenuStrip();AmoTheme.StyleMenu(menu);
    menu.Items.Add(mic?"Habilitar / bloquear micrófono":"Habilitar / bloquear cámara",null,
      delegate {ToggleDevice(mic);});
    menu.Items.Add("Permisos por sitio",null,delegate {ShowSecurity();});
    menu.Items.Add(mic?"Ajustes de micrófono de Windows":"Ajustes de cámara de Windows",null,
      delegate {OpenWindowsSettings(mic?"ms-settings:privacy-microphone":"ms-settings:privacy-webcam");});
    menu.Show((Control)sender,new Point(0,20));
   }else if(e.Button==MouseButtons.Left){ToggleDevice(mic);}
   await Task.CompletedTask;
  }
  async void ToggleDevice(bool mic){
   bool enable=mic?!securityManager.MicrophoneEnabled:!securityManager.CameraEnabled;
   if(mic){
    securityManager.MicrophoneEnabled=enable;
    if(enable)securityManager.Remove("127.0.0.1|Microphone");
    if(!enable)await PauseAvatarIfListening();
   }else securityManager.CameraEnabled=enable;
   UpdateDeviceLabels();
   if(!privateMode) { settings.MicrophoneEnabled = securityManager.MicrophoneEnabled; settings.CameraEnabled = securityManager.CameraEnabled; settings.Save(SettingsFile); }
   ShowToast((mic?"Micrófono":"Cámara")+(enable?" habilitado para nuevas solicitudes.":" bloqueado para nuevas solicitudes. Si está en uso, cerrá o recargá esa pestaña."),
       enable?AmoTheme.Success:AmoTheme.Warning);
  }
  async Task PauseAvatarIfListening(){
   foreach(var web in tabs.TabPages.Cast<TabPage>().SelectMany(t=>t.Controls.OfType<WebView2>())){
    if(web.CoreWebView2==null||web.Source==null||web.Source.Host!="127.0.0.1"||web.Source.Port!=18771)continue;
    try {await web.ExecuteScriptAsync("if(document.getElementById('mic')?.getAttribute('aria-pressed')==='true')document.getElementById('mic').click()");}
    catch {}
   }
  }
  void UpdateDeviceLabels(){
   DrawDevice(cameraLabel,"CAM",securityManager.CameraEnabled,PrivacyInUse("webcam"));
   DrawDevice(microphoneLabel,"MIC",securityManager.MicrophoneEnabled,PrivacyInUse("microphone"));
  }
  void DrawDevice(Label label,string name,bool enabled,bool? inUse){
   label.Text=name+(enabled?" PERM":" BLOQ")+(inUse==true?" *":"");
   label.ForeColor=!enabled?(inUse==true?AmoTheme.Warning:AmoTheme.Danger):
                   inUse==true?AmoTheme.Success:AmoTheme.TextMuted;
   tooltips.SetToolTip(label,name+": "+(enabled?"permitido para nuevas solicitudes":"bloqueado para nuevas solicitudes")+
      ". Uso detectado por Windows: "+(inUse==true?"sí":inUse==false?"no":"sin información")+
      ". Clic para cambiar; el bloqueo no corta una captura ya activa.");
  }
  void SeedDefaultScripts(){
   if(privateMode)return;
   string dir=ScriptsDir;
   string readme=Path.Combine(dir,"LEEME-SCRIPTS-PREDETERMINADOS.txt");
   if(!File.Exists(readme))File.WriteAllText(readme,
      "Scripts de ejemplo de ArrobAMO. Se ejecutan SOLO si vos los elegís.\r\n"+
      "Grabá tus propios pasos con Automatizaciones > Activar Loop / grabar.\r\n"+
      "No modifiques ni reemplaces scripts existentes para agregar ejemplos.\r\n");
   string one=Path.Combine(dir,"00-EJEMPLO-Abrir-DesarrollAMO.arrobamo");
   if(!File.Exists(one))File.WriteAllText(one,"# Ejemplo seguro: abrir la web del proyecto (ejecutar solo manualmente)\r\nNAV https://desarrollamo.com.ar/\r\n");
   string two=Path.Combine(dir,"00-EJEMPLO-Abrir-Documentacion.arrobamo");
   if(!File.Exists(two))File.WriteAllText(two,"# Ejemplo seguro: abrir documentación pública (ejecutar solo manualmente)\r\nNAV https://developer.mozilla.org/es/\r\n");
  }
  async Task CopyPageContextForAI(){
   var web=Active();
   if(web==null || web.CoreWebView2==null || web.Source==null){
    ShowToast("No hay una página activa para preparar contexto.",AmoTheme.Warning);return;
   }
   string site=web.Source.GetLeftPart(UriPartial.Authority);
   string title=web.CoreWebView2.DocumentTitle ?? "";
   if(title.Length>180)title=title.Substring(0,180);
   string context="Contexto preparado manualmente desde ArrobAMO\r\nSitio: "+site+
     "\r\nTítulo: "+title+
     "\r\nNo se copiaron cookies, contraseñas, formularios, URL con parámetros ni contenido privado.";
   try {Clipboard.SetText(context);ShowToast("Contexto mínimo copiado. Pegalo solo en la IA que elijas.",AmoTheme.Success);}
   catch {ShowToast("No se pudo copiar el contexto.",AmoTheme.Warning);}
   await Task.CompletedTask;
  }
  string GetHelpHtml(){
   return @"<!doctype html><html lang='es'><head><meta charset='utf-8'><title>AyudAMO · ArrobAMO</title>
   <style>body{font:16px Segoe UI,sans-serif;background:#fff;color:#151515;max-width:880px;margin:32px auto;padding:20px;line-height:1.6}
   h1{font-size:32px}h2{margin-top:30px;color:#202020}section{background:#f8f8f8;border:1px solid #dedede;padding:16px 22px;border-radius:14px;margin:12px 0}
   code{color:#1e1e1e}button{background:#171717;color:white;border:1px solid #171717;border-radius:8px;padding:11px 16px;cursor:pointer;margin:6px}button:hover{border-color:#83d8f4;box-shadow:0 0 0 2px #f1b8dc}h1{border-bottom:3px solid #f0d578;padding-bottom:12px}
   small{color:#626262}</style></head><body><h1>AyudAMO · ArrobAMO 0.5.3.1</h1>
   <p>Ayuda local. Escribí <code>/ayudAMO</code>, <code>/help</code> o <code>arrobamo://ayudamo</code> en la barra de direcciones.</p>
   <section><h2>Micrófono y cámara</h2><p>Clic en MIC o CAM para permitir o bloquear NUEVAS solicitudes de sitios dentro de ArrobAMO.
   Clic derecho: permisos por sitio y ajustes de privacidad de Windows. Los controles no apagan físicamente los dispositivos.
   Si ya hay una captura activa, cerrá o recargá la pestaña para detenerla. El logo orbital controla la escucha de AvatarAMO.</p></section>
   <section><h2>Red y conexiones</h2><p>NET comprueba una conexión TCP; Wi-Fi y Ethernet muestran enlaces de adaptadores.
   BT PAN muestra solo conectividad de red Bluetooth, no el estado del radio ni los dispositivos emparejados.
   Clic sobre cualquier indicador: detalles. Clic derecho: abrir ajustes de Windows o actualizar.</p></section>
   <section><h2>Scripts y automatizaciones</h2><p>Hay ejemplos en la carpeta local Scripts, sin ejecución automática.
   Para grabar: Automatizaciones → Activar Loop / grabar. Para ejecutar, importá o elegí un script explícitamente.</p>
   <button onclick='chrome.webview.postMessage(&apos;AMO|SCRIPTS&apos;)'>Abrir scripts</button></section>
   <section><h2>IA y navegación</h2><p>Menú Conectar IA: ChatGPT, Gemini, Claude, Copilot, Perplexity y AvatarAMO local.
   El perfil de ArrobAMO es separado de Chrome. Para abrir AvatarAMO: <code>arrobamo://avataramo</code>.</p>
   <button onclick='chrome.webview.postMessage(&apos;AMO|IA&apos;)'>Conectar IA</button></section>
   <small>Los cambios en permisos del navegador no alteran la red, el firewall ni los permisos globales de Windows.</small></body></html>";
  }
 }
}
