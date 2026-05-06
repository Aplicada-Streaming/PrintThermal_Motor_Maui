# Guía de Uso de la Librería — Motor DSL de Renderizado  
**Archivo:** guia-uso-libreria_v1.0.md  
**Versión:** 1.0  
**Fecha:** 2026-03-28  
**Autor:** Equipo Técnico  
**Estado:** Histórico  

> **⚠️ Documento histórico — versión 1.0.** Para la versión actual con los nuevos paquetes (`MotorDsl.Printing.Abstractions`, `MotorDsl.Bluetooth`, `MotorDsl.Maui`) ver [guia-uso-libreria_v1.1.md](./guia-uso-libreria_v1.1.md).

---

## 1. Propósito

Este documento describe cómo utilizar la librería del Motor DSL de renderizado desde una aplicación cliente.

La guía está orientada a desarrolladores que integran el motor en proyectos .NET (por ejemplo MAUI, APIs, servicios backend o herramientas de generación de documentos).

Incluye:

- Instalación y configuración  
- Inicialización del motor  
- Ejecución de renderizado  
- Uso de perfiles de dispositivo  
- Ejemplos prácticos  
- Buenas prácticas de integración  

---

## 2. Requisitos

- .NET 6 o superior  
- Contenedor de inyección de dependencias (opcional pero recomendado)  
- Conocimiento básico de C#  
- Referencia al paquete de la librería del motor  

---

## 3. Instalación

Agregar la librería al proyecto:

```bash id="install-001"
dotnet add package MotorDsl.RenderEngine
````

---

## 4. Configuración inicial

Registrar los servicios del motor en el contenedor de dependencias:

```csharp id="setup-di-001"
using MotorDsl;

var services = new ServiceCollection();

services.AddMotorDslEngine();

var serviceProvider = services.BuildServiceProvider();
```

---

## 5. Resolución del motor

Obtener una instancia del motor:

```csharp id="resolve-engine-001"
var engine = serviceProvider.GetRequiredService<IDocumentEngine>();
```

---

## 6. Uso básico del motor

### Ejemplo simple

```csharp id="basic-render-001"
var templateDsl = @"
container {
  text ""Hola {{Nombre}}""
}
";

var data = new
{
    Nombre = "Juan"
};

var profile = new DeviceProfile
{
    Name = "Default",
    Width = 80,
    RenderTarget = "text"
};

var result = engine.Render(templateDsl, data, profile);

Console.WriteLine(result.Output);
```

---

## 7. Uso con plantilla pre-parsed

```csharp id="parsed-template-001"
var parser = serviceProvider.GetRequiredService<IDslParser>();

var template = parser.Parse(templateDsl);

var result = engine.Render(template, data, profile);
```

---

## 8. Uso de perfiles de dispositivo

Los perfiles definen cómo se renderiza el documento.

### Ejemplo

```csharp id="device-profile-001"
var profile = new DeviceProfile
{
    Name = "POS Printer",
    Width = 48,
    RenderTarget = "escpos"
};
```

---

## 9. Renderizado a distintos targets

El motor puede generar diferentes tipos de salida según el renderer disponible:

### Ejemplo: salida tipo texto

```csharp id="text-output-001"
profile.RenderTarget = "text";
```

### Ejemplo: salida para UI

```csharp id="ui-output-001"
profile.RenderTarget = "ui";
```

### Ejemplo: salida para impresión ESC/POS

```csharp id="escpos-output-001"
profile.RenderTarget = "escpos";
```

---

## 10. Uso de datos dinámicos

Los datos pueden ser objetos anónimos, DTOs o modelos complejos.

```csharp id="data-example-001"
var data = new
{
    Cliente = new
    {
        Nombre = "Ana",
        Edad = 30
    }
};
```

En el DSL:

```text id="dsl-binding-001"
text "Cliente: {{Cliente.Nombre}}"
```

---

## 11. Manejo de resultados

El resultado del render contiene información útil:

```csharp id="result-usage-001"
if (result.Errors?.Any() == true)
{
    foreach (var error in result.Errors)
    {
        Console.WriteLine(error);
    }
}
else
{
    Console.WriteLine(result.Output);
}
```

---

## 12. Manejo de errores

Errores pueden ocurrir en:

* Parsing DSL
* Resolución de datos
* Evaluación de expresiones
* Renderizado

Siempre validar:

```csharp id="error-check-001"
if (result.Errors != null && result.Errors.Count > 0)
{
    // Manejo de errores
}
```

---

## 13. Integración en aplicaciones MAUI (ejemplo)

```csharp id="maui-integration-001"
public class DocumentService
{
    private readonly IDocumentEngine _engine;

    public DocumentService(IDocumentEngine engine)
    {
        _engine = engine;
    }

    public RenderResult RenderDocument(object data)
    {
        var template = "text \"Hola {{Nombre}}\"";

        var profile = new DeviceProfile
        {
            Name = "Mobile",
            Width = 50,
            RenderTarget = "ui"
        };

        return _engine.Render(template, data, profile);
    }
}
```

---

## 14. Buenas prácticas

* Mantener templates DSL simples y reutilizables
* Separar datos de presentación
* Definir perfiles de dispositivo claros
* Validar datos antes del render
* Cachear templates si se reutilizan frecuentemente
* Evitar lógica compleja dentro del DSL
* Versionar plantillas cuando evolucionan

---

## 15. Organización recomendada

Una estructura típica de integración:

```text id="structure-001"
Application
 ├── Templates
 ├── Models (Data)
 ├── Services
 ├── DeviceProfiles
 └── MotorDsl Integration
```

---

## 16. Performance

Recomendaciones:

* Reutilizar instancias del motor
* Evitar parsing repetido innecesario
* Cachear DocumentTemplate si el DSL no cambia
* Minimizar evaluaciones de expresiones complejas
* Utilizar perfiles adecuados al dispositivo

---

## 17. Extensibilidad desde el consumidor

El consumidor puede extender el motor mediante:

* Implementación de nuevos renderers
* Implementación de IDataResolver
* Nuevos DeviceProfiles
* Extensiones del parser DSL
* Plugins personalizados

Ver documento:

* extensibilidad-motor_v1.0.md

---

## 18. Troubleshooting

### Problemas comunes

**1. No se encuentra el renderer**

* Verificar registro en DI
* Verificar Target del DeviceProfile

**2. Expresiones no se evalúan**

* Validar sintaxis DSL
* Verificar paths de datos

**3. Output vacío**

* Revisar AST generado
* Verificar data binding

---

## 19. Relación con otros documentos

Este documento se complementa con:

* arquitectura-solucion_v1.0.md
* contratos-del-motor_v1.0.md
* modelo-datos-logico_v1.0.md
* flujo-ejecucion-motor_v1.0.md
* extensibilidad-motor_v1.0.md

---

## 20. Historial de versiones

| Versión | Fecha      | Autor          | Cambios             |
| ------- | ---------- | -------------- | ------------------- |
| 1.0     | 2026-03-28 | Equipo Técnico | Guía inicial de uso |

---