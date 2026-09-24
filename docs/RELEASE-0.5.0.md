# ArrobAMO 0.5.0 · 24/09/2026

Actualización del navegador de escritorio para Windows, compilado con WebView2.

## Incluye
- AvatarAMO accesible desde el menú, barra lateral, menú contextual y dirección `arrobamo://avataramo` (alias de la interfaz local).
- El símbolo orbital controla la escucha de AvatarAMO con clic; clic derecho abre opciones de voz. El estado se refleja por animación y color.
- Indicadores de CPU, RAM y GPU por tramos hasta 33,3 %, 66,6 % y 100 %, más estado aproximado de uso de cámara/micrófono según Windows.
- Ajustes de visualización y restauración de pestañas persistentes en el perfil local.
- Las sesiones de IA en WebView2 se mantienen en el perfil propio del navegador; no se copian credenciales de Chrome.

## Condiciones y límites
- Para comandos por voz debe estar disponible AvatarAMO local (`127.0.0.1:18771`) y concedido el permiso de micrófono.
- El clic inicial en el símbolo abre AvatarAMO si todavía no existe su pestaña; su escucha se inicia según la configuración de AvatarAMO.
- Los sitios de IA externos no se leen ni controlan automáticamente. No se usa la API de pago de ChatGPT.
- Los indicadores de cámara/micrófono proceden del registro de privacidad de Windows, pueden tener demora y no constituyen prueba de que ningún otro proceso acceda al dispositivo.
- La dirección interna `arrobamo://avataramo` es exclusiva de ArrobAMO; no es un servidor público.

## Validación
Compilar y probar el ejecutable y su integración local antes de distribuirlo. Conservar datos del perfil en `%LOCALAPPDATA%\ArrobAMO` durante la actualización.
