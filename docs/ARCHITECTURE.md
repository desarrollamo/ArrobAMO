# Arquitectura inicial

## Principio

DesarrollAMOBrowser debe poder nacer en Windows y evolucionar hacia DesarrollAMO OS sin reescribir el producto completo.

## Capas

```text
UI
↓
Browser Core
↓
Web Engine Adapter
↓
Chromium Embedded Engine
↓
Windows
```

El Core no debe depender directamente de detalles de Windows salvo en módulos de plataforma.

## Reglas iniciales

- Evitar Electron como runtime principal.
- Mantener UI, Core y motor web desacoplados.
- Preferir código nativo para procesos críticos.
- Toda integración con IAMO debe ser explícita y autorizable.
- La telemetría local debe poder desactivarse.
- No enviar historial, cookies ni contenido de navegación a servicios externos por defecto.
- Fijar versiones de dependencias críticas para builds reproducibles.

## Plataforma inicial

- Windows 10/11 x64
- artefacto objetivo: `DesarrollAMOBrowser.exe`

## Futuro

El adaptador del motor permitirá sustituir componentes de Chromium gradualmente sin cambiar la API principal del Browser Core.
