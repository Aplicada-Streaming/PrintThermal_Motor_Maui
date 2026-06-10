# guia-testing-extensibilidad_v1.0.md

## 1. Propósito

Definir una guía práctica para probar la extensibilidad del motor DSL, asegurando que:

- Nuevos renderizadores se integren correctamente.
- Nuevos tipos de nodos puedan incorporarse sin afectar el núcleo.
- Las extensiones respeten contratos y comportamientos esperados.
- El sistema mantenga compatibilidad hacia atrás.

---

## 2. Alcance

Aplica a:

- Plugins o extensiones del motor.
- Renderizadores personalizados (UI, ESC/POS, PDF, etc.).
- Nuevos evaluadores de expresiones.
- Nuevos nodos DSL.
- Hooks de ejecución del motor.

No aplica a:
- Testing funcional general de plantillas (cubierto en otras estrategias).
- Testing de infraestructura externa (API, red, etc.), salvo integración con extensiones.

---

## 3. Objetivos de Testing

- Validar que las extensiones cumplen contratos definidos.
- Asegurar interoperabilidad con el core del motor.
- Verificar que no se introducen regresiones.
- Comprobar que las extensiones son reemplazables y desacopladas.
- Garantizar comportamiento consistente bajo distintos escenarios.

---

## 4. Tipos de Testing para Extensibilidad

### 4.1 Contract Testing

Valida que las extensiones cumplen interfaces esperadas:

- Métodos obligatorios implementados.
- Firmas de métodos correctas.
- Tipos de retorno compatibles.

**Ejemplo:**
- Un renderizador debe implementar el contrato `MotorDsl.Core.Contracts.IRenderer`, que exige:
  - la propiedad `string Target { get; }` (el target que el renderer atiende, ej. `"text"`, `"escpos"`, `"pdf"`),
  - el método `RenderResult Render(LayoutedDocument document, DeviceProfile profile)`.

  (No existen los tipos `Document` ni `RenderContext`: el renderer recibe un `LayoutedDocument` y un `DeviceProfile`.)

---

### 4.2 Integration Testing

Valida la interacción entre:

- Motor core + extensión.
- DSL + nuevo nodo.
- Motor + renderizador externo.

**Objetivo:**
Asegurar que la extensión funciona dentro del pipeline completo.

---

### 4.3 Plugin Testing

Valida el ciclo de vida de una extensión:

- Carga dinámica.
- Registro en el motor.
- Inicialización.
- Ejecución.
- Desregistro (si aplica).

---

### 4.4 Regression Testing

Asegura que nuevas extensiones no rompen:

- Renderizadores existentes.
- Nodos existentes.
- Flujos de ejecución actuales.

---

### 4.5 Compatibility Testing

Verifica compatibilidad con:

- Versiones anteriores del DSL.
- Distintos perfiles de dispositivo.
- Diferentes configuraciones del motor.

---

## 5. Escenarios de Prueba para Extensibilidad

### 5.1 Registro de un Nuevo Renderizador

**Objetivo:**
Validar que el motor reconoce y utiliza un renderizador externo.

**Casos:**
- Registro exitoso.
- Renderizador no registrado.
- Renderizador duplicado.
- Renderizador con dependencia faltante.

---

### 5.2 Ejecución con Nodo Personalizado

**Objetivo:**
Validar que el motor procesa un nodo extendido.

**Casos:**
- Nodo reconocido correctamente.
- Nodo no soportado → fallback o error.
- Nodo con comportamiento personalizado.

---

### 5.3 Evaluador de Expresiones Extendido

**Objetivo:**
Validar soporte de nuevas funciones o operadores.

**Casos:**
- Evaluación correcta de nuevas funciones.
- Manejo de errores en expresiones inválidas.
- Compatibilidad con expresiones existentes.

---

### 5.4 Hooks y Middleware del Motor

**Objetivo:**
Validar puntos de extensión en el pipeline.

**Casos:**
- Hook pre-ejecución.
- Hook post-ejecución.
- Interceptación de nodos.
- Modificación de contexto.

---

### 5.5 Sustitución de Componentes

**Objetivo:**
Validar reemplazo de componentes internos.

**Casos:**
- Sustitución de renderer por uno custom.
- Sustitución de evaluador de expresiones.
- Configuración de fallback.

---

## 6. Estrategia de Pruebas

### 6.1 Enfoque por Capas

1. Unit Tests:
   - Validan la lógica interna de la extensión.

2. Integration Tests:
   - Validan interacción con el motor.

3. End-to-End Tests:
   - Validan flujo completo con plantillas reales.

---

### 6.2 Uso de Mocks y Stubs

- Simular contexto del motor.
- Simular datos de entrada.
- Simular renderizadores o dependencias externas.

---

### 6.3 Test Harness del Motor

Se recomienda disponer de un entorno de pruebas que permita:

- Registrar extensiones dinámicamente.
- Ejecutar plantillas DSL de prueba.
- Inspeccionar resultados intermedios.
- Capturar logs y trazas.

---

## 7. Criterios de Aceptación

Una extensión es considerada válida si:

- Implementa correctamente los contratos definidos.
- Se integra sin modificar el core del motor.
- No rompe funcionalidades existentes.
- Se comporta correctamente en distintos perfiles de dispositivo.
- Puede ser registrada y utilizada dinámicamente.

---

## 8. Casos de Error Comunes

- Interfaces mal implementadas.
- Dependencias no resueltas.
- Conflictos de nombres entre extensiones.
- Incompatibilidad de versiones DSL.
- Renderizadores que no respetan restricciones del dispositivo.
- Evaluadores que no manejan errores de forma segura.

---

## 9. Buenas Prácticas

- Diseñar extensiones desacopladas del core.
- Usar contratos claros (interfaces).
- Evitar dependencias directas entre extensiones.
- Versionar extensiones de forma independiente.
- Documentar comportamiento esperado.
- Incluir tests automatizados junto con cada extensión.

---

## 10. Métricas de Calidad

- Cobertura de tests de extensiones.
- Tasa de fallos en integración.
- Tiempo de ejecución de pruebas.
- Número de regresiones detectadas.
- Compatibilidad entre versiones.

---

## 11. Herramientas Sugeridas

- Framework de testing unitario (.NET: xUnit / NUnit).
- Mocking frameworks (Moq, NSubstitute).
- Logs estructurados para trazabilidad.
- Test runners automatizados en CI/CD.

---

## 12. Versionado

Este documento corresponde a:

- **guia-testing-extensibilidad_v1.0**

Las futuras versiones deberán:

- Incorporar nuevos tipos de extensiones.
- Ajustar escenarios de prueba según evolución del motor.
- Mantener compatibilidad con versiones anteriores del DSL y APIs.

---