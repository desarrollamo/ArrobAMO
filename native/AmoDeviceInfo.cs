using System;
using System.Drawing;
using System.Diagnostics;
using System.Net;
using System.Management;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace ArrobAMO {
 public sealed partial class BrowserForm {
  readonly Label deviceLabel=new Label();
  string ReadHardwareInformation() {
   string cpu="Sin datos",gpu="Sin datos",ram="Sin datos";
   try {using(var s=new ManagementObjectSearcher("SELECT Name FROM Win32_Processor"))
    foreach(ManagementObject x in s.Get()){cpu=Convert.ToString(x["Name"]);break;}}catch {}
   try {using(var s=new ManagementObjectSearcher("SELECT Name FROM Win32_VideoController"))
    foreach(ManagementObject x in s.Get()){gpu=Convert.ToString(x["Name"]);break;}}catch {}
   try {using(var s=new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem"))
    foreach(ManagementObject x in s.Get()){ram=(Convert.ToDouble(x["TotalPhysicalMemory"])/1073741824d).ToString("0.0")+" GiB";break;}}catch {}
   return "Equipo: "+Environment.MachineName+"\nSistema: "+Environment.OSVersion.VersionString+
     "\nArquitectura SO: "+(Environment.Is64BitOperatingSystem?"64 bits":"32 bits")+
     "\nCPU: "+cpu+"\nRAM: "+ram+"\nGPU: "+gpu+
     "\n\nEl nombre del equipo y una captura de pantalla NO acreditan identidad, ubicación ni pertenencia a una empresa.";
  }
  void SetupDeviceLabel(){
   AddNetworkLabel(deviceLabel,"PC: "+Environment.MachineName,1047,182);
   deviceLabel.AutoEllipsis=true;
   tooltips.SetToolTip(deviceLabel,"Datos de este dispositivo; clic para ver. No es una prueba de identidad del equipo.");
   deviceLabel.MouseUp+=delegate(object sender,MouseEventArgs e){if(e.Button==MouseButtons.Left)ShowDeviceInfo();else if(e.Button==MouseButtons.Right)ShowDeviceMenu();};
   deviceLabel.Visible=ClientSize.Width>=1350;
  }
  void ShowDeviceInfo(){MessageBox.Show(this,ReadHardwareInformation(),"Dispositivo · ArrobAMO",MessageBoxButtons.OK,MessageBoxIcon.Information);}
  void ShowDeviceMenu(){
   var menu=new ContextMenuStrip();AmoTheme.StyleMenu(menu);
   menu.Items.Add("Información del equipo",null,delegate {ShowDeviceInfo();});
   menu.Items.Add("Comprobar versión en GitHub",null,async delegate {await CheckGitHubVersion();});
   menu.Items.Add("Abrir repositorio oficial",null,delegate {OpenWindowsSettings("https://github.com/desarrollamo/ArrobAMO");});
   menu.Show(deviceLabel,new Point(0,20));
  }
  async Task CheckGitHubVersion(){
   string local=System.Windows.Forms.Application.ProductVersion;
   try{
    string latest=await Task.Run(delegate{
     ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
     var req=(HttpWebRequest)WebRequest.Create("https://api.github.com/repos/desarrollamo/ArrobAMO/releases/latest");
     req.UserAgent="ArrobAMO/"+local;req.Accept="application/vnd.github+json";req.Timeout=6000;req.ReadWriteTimeout=6000;
     using(var response=(HttpWebResponse)req.GetResponse())
     using(var reader=new System.IO.StreamReader(response.GetResponseStream())){
      string json=reader.ReadToEnd();
      Match m=Regex.Match(json,"\"tag_name\"\\s*:\\s*\"v?([0-9]+\\.[0-9]+\\.[0-9]+)\"");
      if(!m.Success)throw new Exception("GitHub no devolvió una versión válida.");
      return m.Groups[1].Value;
     }
    });
    Version current=new Version(local),remote=new Version(latest);
    string text="Versión instalada: "+current+"\nÚltima versión publicada en GitHub: "+remote+
      "\n"+(current<remote?"Hay una actualización publicada.":current==remote?"La versión instalada coincide con la publicación.":"Versión local posterior a la última publicación.")+
      "\n\nEsta comparación requiere Internet y no modifica el navegador.";
    MessageBox.Show(this,text,"Actualizaciones · ArrobAMO",MessageBoxButtons.OK,MessageBoxIcon.Information);
   }catch(Exception e){MessageBox.Show(this,"Versión instalada: "+local+
      "\nNo se pudo consultar GitHub: "+e.Message+"\nLa versión remota NO fue verificada.",
      "Actualizaciones · ArrobAMO",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
  }
 }
}
