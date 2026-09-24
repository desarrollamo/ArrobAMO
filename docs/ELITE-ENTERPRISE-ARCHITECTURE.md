# ArrobAMO Elite / Enterprise — arquitectura local

## Principio

El Browser Core debe seguir funcionando aunque IA, Sync, Elite, Marketplace o Enterprise fallen o estén desactivados.

Capas:

1. Browser Core
2. UI
3. Automation Runtime
4. AI Runtime
5. Sync
6. Elite Collaboration
7. Enterprise Administration
8. Marketplace / Extensions
9. Developer SDK

Las capas 6–9 no pueden convertirse en dependencias obligatorias del Browser Core.

## Planes

- Free: navegación esencial, historial, marcadores, descargas, privado, workspace personal.
- PRO: productividad individual avanzada.
- Elite: organizaciones locales, equipos, RBAC, workspaces compartidos, comentarios, actividad, aprobaciones, agentes supervisados y paquetes.
- Enterprise: administración central, políticas, auditoría resistente, dispositivos, marketplace privado y APIs.

La lógica se centraliza en AmoEntitlements; no debe dispersarse por la aplicación.

## Elite 1 implementado localmente

- entidad Organización;
- usuarios e invitados;
- equipos;
- 9 roles base;
- roles personalizados;
- permisos granulares;
- workspaces Personal/Compartido/Equipo/Organización;
- comentarios + menciones en modelo;
- actividad;
- historial de versiones en modelo;
- aprobaciones;
- entidad Agente con autonomía 0–4 y límites;
- kill switch persistente;
- persistencia JSON atómica;
- centro Equipo separado de la navegación;
- modo de prueba --elite.

## Seguridad actual

- Free no puede crear organización mediante el modelo.
- Los permisos no dependen del nombre visible del rol.
- Un invitado base tiene workspace.read pero no workspace.write.
- Los permisos personalizados se filtran contra el catálogo conocido.
- Los agentes nacen en nivel 0.
- El kill switch se persiste.

## NO configurado todavía

No afirmar como disponible:

- colaboración remota;
- presencia en tiempo real;
- backend multi-tenant;
- tenant isolation en servidor;
- SSO / SAML / OIDC;
- SCIM;
- passkeys empresariales;
- E2EE;
- Secret Manager;
- DLP;
- SIEM;
- Marketplace;
- extensiones sandboxed;
- SDK / CLI;
- API Enterprise;
- webhooks;
- MDM;
- auditoría inmutable;
- self-hosted.

La UI debe mostrar esas capacidades como no configuradas/pendientes hasta que existan y estén verificadas.

## Siguientes fases

### Elite 2
Agentes supervisados, approvals reales sobre acciones, execution log, límites y kill switch conectado al runtime.

### Elite 3
Formato .arrobamo, validación, permisos de paquetes, firma, Marketplace, extensiones y SDK.

### Enterprise 1
Admin Center, políticas, usuarios, dispositivos y audit log central.

### Enterprise 2+
Identidad empresarial, secretos, sync avanzado, APIs, despliegue y controles de seguridad.

## Regla de validación

Ningún módulo Enterprise se considera terminado por tener pantalla. Para cada función registrar:

- IMPLEMENTADA
- TESTEADA
- VERIFICADA EN PRODUCCIÓN
- EXPERIMENTAL
- LIMITADA
- PENDIENTE

