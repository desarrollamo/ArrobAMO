# DesarrollAMOBrowser

Navegador de **DesarrollAMO** para Windows.

**Objetivo de salida estable:** `DesarrollAMOBrowser.exe`

## Estado

Proyecto inicializado. La primera meta es una versión usable en Windows basada en un motor Chromium embebido, con interfaz y comportamiento propios de DesarrollAMO.

## v0.1 · Windows

- Ejecutable nativo: `DesarrollAMOBrowser.exe`
- Navegación web real
- Pestañas
- Atrás / adelante / recargar
- Barra de direcciones
- Descargas
- Favoritos básicos
- Modo oscuro
- Identidad visual oficial DesarrollAMO
- Base preparada para telemetría local, perfiles e integración futura con IAMO

## Arquitectura objetivo

```text
DesarrollAMOBrowser.exe
├─ UI DesarrollAMO
├─ Core del navegador
├─ motor Chromium embebido
└─ Windows
```

La intención es evitar Electron como capa principal y mantener el Core reutilizable para una futura versión nativa de DesarrollAMO OS.

## Branding

La identidad visual se toma de:

- `desarrollamo/branding`
- versión fijada: `v1.3.0`
- wordmark: **DesarrollAMO.**
- claim: **Tecnología con alma.**

No depender de `main` del repositorio de branding para builds estables.

## Camino de evolución

- **v0.1:** Browser propio + Chromium en Windows
- **v0.2:** monitor por pestaña de CPU, RAM, GPU y red
- **v0.3:** privacidad, cookies, permisos y aislamiento
- **v0.4:** IAMO integrado y automatización autorizada
- **v0.5:** UI, seguridad, perfiles y Core DesarrollAMO propios
- **v1:** storage, permisos, telemetría y más red bajo DesarrollAMO Core
- **v2:** motor experimental DesarrollAMO para HTML/CSS simple
- **v3+:** reducción gradual de dependencias de Chromium

## Criterio de versión estable

Una versión sólo se considerará estable cuando:

1. compile para Windows x64;
2. genere `DesarrollAMOBrowser.exe`;
3. abra sitios HTTPS reales;
4. soporte navegación básica y múltiples pestañas;
5. cierre sin dejar procesos huérfanos;
6. pase una prueba básica de consumo de memoria y estabilidad;
7. tenga artefacto de release reproducible.
