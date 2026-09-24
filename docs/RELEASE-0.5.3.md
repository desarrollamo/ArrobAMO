# ArrobAMO 0.5.3 · Windows x64

## Correcciones
- Menú contextual de NET/Wi-Fi/Ethernet/Bluetooth PAN y logo: eliminado el desecho prematuro de ContextMenuStrip que provocaba ObjectDisposedException al seleccionar opciones.
- Eventos duplicados de la barra lateral: botones compartidos con el panel principal reciben un único handler (Scripts, Loop, Descargas, Equipo, Seguridad, Configuración, UI Kit, expandir sidebar).
- Búsqueda de Inicio después de Volver: mensajes de páginas internas usan un token efímero del proceso; se conserva el funcionamiento tras navegar a una web y regresar, sin permitir que una web externa mande órdenes al navegador.
- AyudAMO usa base blanca/negra y acentos de DesarrollAMO discretos; marco y repintado de botones corregidos.
- Perfil local: se abre la carpeta con Explorador de Windows, no una terminal.
- Importación muestra solo la funcionalidad efectivamente disponible (favoritos Chromium/Edge/Brave/Opera); no presenta contraseñas o historial como si pudieran importarse.
- Barra de conexiones incluye nombre del equipo; el panel muestra sistema operativo, arquitectura, CPU, RAM, GPU y adaptadores realmente detectados.
- Perfil y menú principal incluyen verificación **manual** de última versión pública en GitHub, con aviso explícito si no se puede verificar.

## Límites y privacidad
- Esta compilación utiliza Windows Forms/WebView2 y solo funciona en Windows; todavía no hay ejecutables nativos para Linux, macOS o Android. Una futura implementación multiplataforma necesitará una capa de sistema diferente.
- El nombre de equipo y sus características, incluso acompañados por capturas de pantalla, **no prueban que el empleado use un dispositivo empresarial**. Para acreditarlo se requiere gestión/atestación del dispositivo por la organización, no capturas solas.
- No se importan contraseñas, historial, autocompletado ni pestañas de otros navegadores. El único importador disponible es el de marcadores, por acción del usuario.
- Las pruebas automatizadas no sustituyen verificaciones físicas con cámara/micrófono ni pruebas de interfaz en todas las configuraciones de pantalla.

## Regresión
Ejecutar BrowserUiSecuritySelfTest con perfil privado y las suites BrowserExtrasSelfTest, AgentGovernanceSelfTest, EnterpriseSelfTest, SettingsAudit051. Confirmar arranque del ejecutable instalado, iconos y lectura real de GitHub.
