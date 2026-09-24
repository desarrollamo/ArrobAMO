# ArrobAMO 0.5.3.1 · revisión de la versión 0.5.3

Corrige la comparación de versiones en «Comprobar GitHub». El ejecutable Windows muestra una versión de cuatro componentes (por ejemplo, 0.5.3.0), mientras GitHub publica etiquetas de tres componentes (v0.5.3). La comparación ahora completa con .0 la etiqueta remota de tres componentes, por lo que un usuario en la misma versión ya no recibe un falso aviso de versión local posterior.

Conserva todos los cambios y las limitaciones de [ArrobAMO 0.5.3](RELEASE-0.5.3.md): el producto sigue siendo exclusivo para Windows; otras plataformas no tienen cliente nativo; solo se importan marcadores; el nombre del equipo y una captura no son una acreditación de identidad corporativa.

## Descargar e instalar (Windows x64)

El archivo recomendado para usuarios es **[ArrobAMOInstaller.exe](https://github.com/desarrollamo/ArrobAMO/releases/download/v0.5.3.1/ArrobAMOInstaller.exe)**. Se descarga directamente, sin ZIP. Incluye ArrobAMO 0.5.3.1, las bibliotecas necesarias, el icono orbital y un desinstalador. Instala para el usuario actual, crea accesos directos en el Escritorio y menú Inicio y registra ArrobAMO en Aplicaciones instaladas.

**SHA-256 del instalador: `DD8FAE3FB79FEF7853500BC6FDFB750F15C14C2604022B3B8DF707C114E94903`.** El ejecutable no está firmado digitalmente todavía; verificar origen y suma antes de ejecutar.

Con el navegador cerrado, podés ejecutar una nueva versión sobre una instalación previa. El instalador conserva el perfil de `%LOCALAPPDATA%\ArrobAMO`; la desinstalación también conserva los datos del usuario. Antes de modificar archivos verifica que el navegador no esté en ejecución, y prepara el contenido en una carpeta temporal.

El instalador requiere Windows 10/11 de 64 bits y Microsoft WebView2 Runtime; si falta este último ofrece el enlace oficial de Microsoft. No se instala WebView2 automáticamente. [Instrucciones de instalación](INSTALAR-ARROBAMO.md). La verificación de actualizaciones desde ArrobAMO es manual, no automática.
