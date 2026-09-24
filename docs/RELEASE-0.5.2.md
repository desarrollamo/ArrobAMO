# ArrobAMO 0.5.2 · Windows x64

## Corregido y mejorado
- Seguridad: los mensajes `AMO|` procedentes de sitios externos o de sus marcos quedan rechazados. Los comandos privilegiados solo se procesan si la pestaña actual es Inicio o AyudAMO y el documento es interno.
- AvatarAMO: el logo orbital reutiliza la sesión de AvatarAMO del panel lateral, si existe, en lugar de abrir otra pestaña.
- Configuración: más margen vertical en Guardar, persistencia del estado de MIC/CAM también al editar las preferencias generales.
- Scripts: catálogo con vista previa, botones consistentes con ArrobAMO y confirmación antes de ejecutar; los ejemplos predeterminados no se ejecutan automáticamente como último Loop.
- Indicadores MIC/CAM: diferenciación visible entre permisos concedidos/bloqueados y uso detectado por Windows.
- Menú y clic derecho: copiar voluntariamente un contexto mínimo (sitio/origen y título) para pegarlo en una IA elegida; no se copian cookies, formularios ni contenido privado.
- Ícono: recurso ejecutable y accesos directos con el símbolo orbital vigente y tamaños de 16 a 256 px; imagen de marca sincronizada.

## Alcance de la integración de IA
Las IAs externas continúan como páginas web independientes. El contexto se copia solo por acción explícita. No existe intercambio autónomo de mensajes ni control de páginas de terceros por parte de ellas.

## Validaciones
Ejecutar las suites de gobernanza, organización, permisos, configuración y navegación/seguridad con perfil aislado. Probar arranque del ejecutable y paquete ZIP. No reutilizar el perfil privado del usuario durante las pruebas.
