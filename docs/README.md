# 🖨️ Motor DSL de Generación de Documentos Térmicos

Sistema de generación y procesamiento de documentos basado en un motor DSL (Domain Specific Language), diseñado para interpretar plantillas JSON, evaluar bindings y renderizar salidas estructuradas con foco en **impresión térmica ESC/POS por Bluetooth** desde aplicaciones **.NET MAUI**.

Construido con **.NET 10**, orientado a arquitectura extensible (renderers `IRenderer` y transports `IThermalPrinterTransport`), versionado semántico y ejecución determinística. Se distribuye en **7 paquetes NuGet** (`MotorDsl.Core`, `Parser`, `Rendering`, `Extensions`, `Printing.Abstractions`, `Bluetooth`, `Maui`).

---

## 📌 Descripción

El Motor DSL es una librería y conjunto de componentes que permiten:

- Definir documentos mediante un lenguaje DSL estructurado (JSON)  
- Interpretar nodos y expresiones  
- Evaluar bindings y condiciones de forma dinámica  
- Renderizar documentos a texto plano, ESC/POS, PDF y raster/preview  
- Imprimir por Bluetooth (Classic SPP en Android) desde apps .NET MAUI  
- Extender funcionalidades vía renderers (`IRenderer`) y transports (`IThermalPrinterTransport`)  
- Versionar y mantener compatibilidad entre cambios  

El repositorio incluye documentación técnica, especificaciones del DSL, reglas de validación, pruebas y guías de extensibilidad.

---

## 🎯 Objetivos del producto

- Definir un lenguaje DSL flexible y extensible  
- Permitir generación de documentos parametrizados  
- Separar contenido, lógica y presentación  
- Facilitar reutilización de plantillas  
- Garantizar trazabilidad y versionado  
- Soportar evolución del DSL sin romper compatibilidad  

---

## 🧱 Stack tecnológico

**Core**

- .NET 10  
- C#  
- Arquitectura modular / orientada a componentes  

**Procesamiento**

- Parser DSL JSON → AST  
- Evaluación de bindings y condiciones  
- Layout adaptable por perfil de dispositivo  
- Renderizado de documentos (texto, ESC/POS, PDF, raster)  

**Impresión / MAUI**

- Impresión térmica ESC/POS  
- Transport Bluetooth Classic SPP (Android) vía `MotorDsl.Bluetooth`  
- Controles y renderers MAUI (`MotorDsl.Maui`)  

**DevOps**

- Git  
- Pipeline CI/CD  
- Versionado SemVer  
- Publicación de paquetes (NuGet o equivalente)  

---

## 🏗️ Arquitectura (resumen)

El sistema se organiza en capas y componentes desacoplados:

- Núcleo del motor (interpretación DSL)  
- Evaluador de bindings y condiciones  
- Renderizadores (`IRenderer`)  
- Transports de impresión (`IThermalPrinterTransport`)  
- Extensibilidad vía DI fluent (`AddMotorDslEngine` / `AddRenderer`)  
- Capa de validación  
- Contratos de entrada/salida  

📄 Ver detalle:

```text
/docs/05_arquitectura_tecnica/arquitectura-solucion_v1.1.md
````

---

## 📂 Estructura del repositorio

```text
/docs
  00_contexto
  01_necesidades_negocio
  02_especificacion_funcional
  03_ux-ui
  04_prompts_ai
  05_arquitectura_tecnica
  06_backlog-tecnico
  07_plan-sprint
  08_calidad_y_pruebas
  09_devops
  10_developer_guide
  11_examples

/src        (código fuente del motor DSL — incluye MotorDsl.Tests)
/samples    (5 apps .NET MAUI de ejemplo)
/scripts    (local | mobile | nuget | docker)
/nupkg      (.nupkg generados, gitignored)
```

> Las pruebas automatizadas viven en `src/MotorDsl.Tests` (no hay carpeta `/tests` separada).

---

## 🚀 Cómo ejecutar el motor

### Prerrequisitos

* .NET 10 SDK
* Workload MAUI: `dotnet workload install maui` (requerido para `MotorDsl.Maui` y los samples)
* Android SDK (para compilar/correr los proyectos MAUI en Android; `dotnet build` a secas no aplica a los proyectos MAUI sin el workload)
* Entorno de desarrollo (Visual Studio / VS Code)

---

### 1. Clonar repositorio

```bash
git clone <repo-url>
cd PrintThermal_Motor_Maui
```

---

### 2. Restaurar dependencias

```bash
dotnet restore
```

---

### 3. Compilar solución

```bash
dotnet build
```

---

### 4. Ejecutar pruebas

```bash
dotnet test
```

**Cobertura mínima requerida:** 70%

---

## 🧪 Ejecución conceptual del motor

El motor procesa una entrada DSL siguiendo este flujo:

```text
DSL → Parseo → Validación → Evaluación → Layout → Render → Output
```

Componentes involucrados:

* Parser DSL
* Validator
* Evaluador de nodos
* Layout Engine
* Renderizador

---

## 🔌 Conceptos principales

### DSL

Lenguaje estructurado que define:

* Estructura del documento
* Nodos lógicos
* Expresiones
* Reglas de composición

📄 Ver detalle:

```text
/docs/02_especificacion_funcional/especificacion-funcional_v1.0.md
```

---

### Nodos

Elementos básicos del DSL que representan:

* Texto
* Condiciones
* Repeticiones
* Expresiones
* Componentes compuestos

---

### Evaluador

Encargado de:

* Resolver expresiones
* Ejecutar lógica condicional
* Interpretar contexto de ejecución

---

### Renderizadores

Transforman el modelo interpretado en una salida concreta:

* Texto plano (`TextRenderer`, target `text`)
* ESC/POS (`EscPosRenderer`, target `escpos`, `byte[]`)
* PDF (`PdfRenderer`, target `pdf`, en `MotorDsl.Maui`)
* Raster / preview PNG (`RasterPreviewRenderer`, target `raster-preview`) y ESC/POS bitmap (`BitmapEscPosRenderer`, target `escpos-bitmap`)
* Otros formatos extensibles vía `IRenderer`

---

### Extensiones

Permiten ampliar el motor mediante:

* Renderizadores específicos (`IRenderer`, registrados con `AddRenderer<T>()`)
* Transports de impresión personalizados (`IThermalPrinterTransport`: BLE, WiFi/TCP, USB)
* Perfiles de dispositivo y plantillas vía DI fluent (`AddProfiles` / `AddTemplates`)

📄 Ver detalle:

```text
/docs/05_arquitectura_tecnica/extensibilidad-motor_v1.0.md   (histórico)
/docs/05_arquitectura_tecnica/extensibilidad-motor_v1.1.md   (vigente)
```

---

## 🔄 Flujo de desarrollo

1. Crear branch `feature/*`
2. Implementar cambios en motor / DSL
3. Agregar pruebas unitarias
4. Ejecutar validaciones
5. Crear Pull Request
6. Pipeline CI en verde
7. Merge a `main`
8. Generación de versión

---

## ✅ Definition of Done

Una funcionalidad se considera completa cuando:

* Código compila correctamente
* Tests pasan
* Cobertura ≥ 70%
* Validaciones DSL exitosas
* Documentación actualizada
* PR aprobado

📄 Ver detalle:

```text
/docs/08_calidad_y_pruebas/estrategia-calidad-motor_v1.0.md
```

---

## 🧭 Roadmap (alto nivel)

* v1.1 — mejoras en evaluador de expresiones
* v1.2 — nuevos renderizadores
* v1.3 — optimización de performance
* v2.0 — evolución del DSL (breaking changes)

---

## 🧪 Calidad y testing

El proyecto incluye:

* Casos de prueba referenciales
* Matriz de cobertura
* Validaciones del motor
* Estrategias de testing automatizado

📄 Ver documentación:

```text
/docs/08_calidad_y_pruebas/
```

---

## 🚀 DevOps

El proyecto cuenta con estrategia de:

* Pipeline CI/CD
* Versionado SemVer
* Entornos de despliegue
* Promoción entre ambientes

📄 Ver documentación:

```text
/docs/09_devops/
```

Incluye:

* pipeline-ci-cd_v1.0.md
* estrategia-versionado_v1.0.md
* entornos-deploy_v1.0.md

---

## 🤝 Contribución

1. Fork del repositorio
2. Crear branch descriptivo
3. Implementar cambios
4. Agregar pruebas
5. Asegurar validaciones en verde
6. Crear Pull Request

---

## 📜 Licencia

MIT.

---

## 📞 Contacto

Fernando Rafael Filipuzzi — [Aplicada Streaming 2026](https://github.com/Aplicada-Streaming)

---
