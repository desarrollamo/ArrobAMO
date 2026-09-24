# ArrobAMO 0.5.1 · Windows x64

## Actualizaciones
- Indicadores MIC y CAM clicables: permiten o bloquean nuevas solicitudes de micrófono y cámara del navegador; clic derecho ofrece permisos por sitio y ajustes de Windows. La preferencia persiste fuera del modo privado.
- Corrección del aviso de voz: menciona ArrobAMO y el botón MIC, no Chrome.
- Estado de conectividad: prueba TCP de Internet, enlace Wi-Fi y Ethernet, y presencia de Bluetooth PAN (red Bluetooth), con detalles y menú contextual.
- /ayudAMO, /help y arrobamo://ayudamo abren ayuda local con instrucciones de voz, permisos, red y scripts.
- Scripts de ejemplo seguros en la carpeta de perfil, sin ejecución automática. Menú contextual de páginas con acceso a AvatarAMO, ayuda y scripts.
- Acceso directo de escritorio a la versión instalada.

## Límites
- MIC y CAM son permisos del navegador, no interruptores físicos del hardware ni permisos globales de Windows. Si una página ya está capturando, hay que cerrarla o recargarla; no se oculta el uso existente.
- Bluetooth PAN no representa el estado del radio ni de dispositivos Bluetooth emparejados. El menú contextual permite abrir directamente la configuración de Windows.
- Si la prueba TCP de Internet falla, aparece SIN VERIF.; no se afirma desconexión solo por un servidor inaccesible.
- La voz requiere el servidor local AvatarAMO disponible y autorización del micrófono por el sitio.
- Ninguna IA externa obtiene automáticamente cookies ni acceso de lectura a páginas privadas.

## Validación
Compilar el ejecutable, correr las pruebas automatizadas y comprobar el arranque con /ayudAMO antes de distribuirlo.
