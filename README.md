# ArrobAMO

ArrobAMO es el navegador del ecosistema DesarrollAMO.

> Navegación + IA + automatización en un solo lugar.

Repositorio oficial: desarrollamo/ArrobAMO.

## Descargar ArrobAMO para Windows

**[⬇ Descargar ArrobAMOInstaller.exe — Windows 10/11 x64](https://github.com/desarrollamo/ArrobAMO/releases/latest/download/ArrobAMOInstaller.exe)**

Abrí el archivo EXE descargado, aceptá las condiciones y pulsá **Instalar**. El instalador crea accesos directos y registra la opción para desinstalar; no hace falta abrir un ZIP ni tener permisos de administrador. [Guía de instalación y requisitos](docs/INSTALAR-ARROBAMO.md).

* La versión de Windows requiere Microsoft WebView2 Runtime. Si no está instalado, el asistente muestra el enlace oficial de Microsoft.
* Las actualizaciones se instalan ejecutando una versión nueva del instalador con el navegador cerrado. Se conservan los datos del perfil local.
* El instalador aún no está firmado digitalmente; verificá el origen de la descarga y su SHA-256 en la publicación.
* Para pruebas manuales sigue disponible el [ZIP portátil](https://github.com/desarrollamo/ArrobAMO/releases/latest).

## Corrección 0.5.3.1

La comprobación de GitHub compara correctamente las versiones de tres y cuatro componentes. Ver [notas](docs/RELEASE-0.5.3.1.md).

## Novedades de la versión 0.5.3

[Notas de la 0.5.3](docs/RELEASE-0.5.3.md). Se corrigen cierres de menús, eventos duplicados, navegación y diseño; se añaden información del dispositivo y consulta de versión.

## Novedades de la versión 0.5.2

Consultar [notas 0.5.2](docs/RELEASE-0.5.2.md). Seguridad de mensajes, voz, ayuda, interfaz y nuevo icono orbital.

## Novedades de la versión 0.5.1

Ver [notas de 0.5.1](docs/RELEASE-0.5.1.md). MIC/CAM, conexiones, AyudAMO y scripts iniciales.

## Novedades de la versión 0.5.0

Ver [notas de versión](docs/RELEASE-0.5.0.md). Incluye AvatarAMO por voz, acceso interno, métricas y ajustes persistentes.

## Funciones heredadas de v0.3.2 (historial)

### Navegación

- WebView2 / Chromium.
- Pestañas con cierre por X, botón central y Ctrl+W.
- Nueva pestaña con Ctrl+T.
- Atrás, adelante, recargar y detener carga.
- Barra de direcciones y búsqueda.
- Indicador HTTPS / HTTP informativo.
- Historial local persistente.
- Marcadores locales persistentes.
- Menú principal.
- Inspeccionar / DevTools.
- URL inicial por línea de comandos.

### Popups y autenticación

Los window.open() ya no se convierten en navegaciones independientes. ArrobAMO crea un WebView hijo real dentro de una pestaña mediante NewWindowRequested.NewWindow y comparte el mismo perfil WebView2.

Prueba verificada: POPUP_OPENER_OK - ArrobAMO.

### IA

Conectar IA ofrece ChatGPT, Gemini, Claude, Microsoft Copilot, Perplexity y URL personalizada. Navegación normal y panel IA usan el mismo perfil local WebView2 para compartir cookies y sesiones.

ChatGPT fue verificado cargando dentro de ArrobAMO.

### Loop y Scripts

Loop:
- Activar / grabar.
- Detener y guardar.
- Ejecutar último Loop.
- Ejecutar minimizado.
- Repetir último Loop.
- Detener ejecución.

Scripts:
- Redactar.
- Importar.
- Exportar.
- Mis scripts.

Formato: *.arrobamo

Sintaxis manual:

    NAV https://ejemplo.com
    INPUT input[name=email] => texto
    CLICK button#enviar
    WAIT 1000

La grabación no guarda el valor de campos password.

Ejecución por línea de comandos:

    ArrobAMO.exe https://ejemplo.com --script=C:\ruta\tarea.arrobamo
    ArrobAMO.exe --record
    ArrobAMO.exe https://ejemplo.com --script=C:\ruta\tarea.arrobamo --minimized

### Sistema visual

La base visual está centralizada en native/AmoUI.cs:
- tokens de color;
- tipografía;
- bordes/radios;
- variantes de botón;
- badges;
- spinner orbital;
- barra de progreso;
- toasts;
- menús;
- UI Kit interna;
- sidebar colapsada / expandida;
- responsive básico del chrome.

Paleta principal: blanco, negro y grises. Verde, rojo, ámbar y azul se reservan para estados funcionales.

### Diagnóstico

CPU, RAM y GPU siguen visibles cuando hay espacio suficiente.

Las excepciones de UI se registran en %LOCALAPPDATA%\ArrobAMO\errores.log. En vez del cuadro JIT crudo de .NET, ArrobAMO muestra un mensaje breve en español.

## Instalador

ArrobAMO-Setup-v0.3.2-win-x64.exe

Pantalla final:
- Importar datos · próximamente
- Conectar IA
- Activar Loop
- Abrir ArrobAMO
- Ver tutorial rápido

Importar datos permanece deshabilitado hasta que la migración sea real y segura.

## Lo que todavía no forma parte de v0.3.2

- importación real de Chrome / Edge / Firefox / Brave / Opera;
- gestor visual completo de descargas;
- extensiones;
- perfil de usuario;
- configuración completa;
- skeletons en todas las vistas;
- sistema modal generalizado;
- terminal integrada;
- splash final con el logo definitivo;
- iconografía definitiva;
- motor propio fuera de Chromium.

No se presentan estas funciones como terminadas.

## Verificación v0.3.2

- POPUP_OPENER_OK - ArrobAMO
- ChatGPT: Chat, Work, Create & Code with AI - ArrobAMO
- SCRIPT_OK_AMO - ArrobAMO
- menú de Loop abierto sin reproducir ArgumentOutOfRangeException.

## Identidad

Producto: ArrobAMO
Marca madre: DesarrollAMO

Concepto: el navegador como núcleo operativo alrededor del cual orbitan sitios, herramientas, scripts, automatizaciones e inteligencias artificiales.

© DesarrollAMO. Tecnología con alma.
