# Estrategia de branching

Esta estrategia implementa la decisión [ADR-001 — Estrategia de dos ramas con promoción por ambientes](architecture-decisions.md#adr-001--estrategia-de-dos-ramas-con-promoción-por-ambientes). Las ramas representan estados del código; DEV, QA y PROD se representan mediante GitHub Environments.

## Clasificación

Es una variante simplificada de Gitflow con prácticas de GitHub Flow:

- De Gitflow conserva `develop` como rama de integración, `main` como línea estable y el tratamiento especial de `hotfix/*`.
- De GitHub Flow adopta ramas temporales cortas, Pull Requests, revisión, CI y eliminación después del merge.
- No es GitHub Flow puro porque las ramas de trabajo se integran primero en `develop`, no directamente en `main`.
- No es Gitflow clásico porque no utiliza `release/*` de forma habitual ni ramas permanentes por cada fase de entrega.
- No es trunk-based porque existen dos ramas permanentes y una etapa de integración previa a `main`.

El nombre oficial dentro de esta propuesta es **estrategia de dos ramas con promoción por ambientes**. Esta denominación describe el comportamiento real sin afirmar adhesión estricta a otro modelo.

## Ramas permanentes

`develop` integra funcionalidades y correcciones normales. Todo merge ejecuta CI y, si resulta exitoso, despliega automáticamente a DEV. No representa una versión productiva.

`main` contiene código estable, funcional y elegible para promoción. Todo merge ejecuta CI y despliega automáticamente a QA; un operador puede promover después ese artefacto exacto a PROD. `main` no equivale al estado actual de producción: ese estado lo identifica el historial del Environment `prod`.

## Ramas temporales

| Tipo | Nace de | PR hacia | Uso |
|---|---|---|---|
| `feature/*` | `develop` | `develop` | Funcionalidad nueva |
| `fix/*` | `develop` | `develop` | Corrección normal |
| `hotfix/*` | `main` | `main` | Incidente urgente de producción |

Ejemplos: `feature/add-version-endpoint`, `fix/version-format` y `hotfix/critical-api-error`. Deben ser breves y eliminarse tras el merge.

Después de promover un hotfix a PROD es obligatorio abrir un PR `main → develop`. Esto devuelve la corrección a la línea de integración y evita que un release posterior la revierta. Si hay conflictos, se resuelven en ese PR y se conserva el historial; no se hace force-push.

### Correcciones encontradas en QA

Un defecto funcional encontrado en QA pertenece al candidato que ya está en `main`, pero todavía no es un hotfix porque no afecta a PROD. Para no incorporar funcionalidades posteriores que puedan existir en `develop`, se crea excepcionalmente `fix/qa-<descripcion>` desde `main`:

```text
main → fix/qa-* → PR main → CI → QA
                         └── después: PR main → develop
```

El artefacto QA fallido queda rechazado y no se recompila. La corrección produce un nuevo commit, un nuevo artefacto y una nueva validación QA. Si la falla era solamente de configuración o infraestructura, se corrige el ambiente y se reintenta el mismo artefacto sin cambios de código.

## Pull requests y protección recomendada

Flujos permitidos: `feature/* → develop`, `fix/* → develop`, `develop → main`, `fix/qa-* → main`, `hotfix/* → main` y, para resincronizar correcciones de QA o PROD, `main → develop`.

Crear rulesets manuales para `main` y `develop`:

- bloquear push directo y force-push;
- requerir pull request y al menos una aprobación;
- exigir el check de CI y resolución de conversaciones;
- exigir rama actualizada antes del merge cuando el ritmo del equipo lo permita;
- restringir borrado y limitar excepciones a administradores designados.

Para `main` conviene usar mayor número de revisores o CODEOWNERS. La protección de ramas controla cambios de código; la protección del Environment controla el despliegue. Son capas complementarias.

## Alternativas y evolución

El modelo anterior era `dev → qa → main`, con `main` asociada a PROD. La propuesta reemplaza ese esquema por `develop → main`: `develop` integra y despliega a DEV; `main` despliega a QA y origina promociones controladas a PROD.

No se mantiene una rama permanente `qa`: agregaría merges y divergencia para representar un estado que corresponde al GitHub Environment `qa`. Tampoco se interpreta `main` como el estado de PROD; ese estado pertenece al historial de deployments del Environment `prod`. Si en el futuro fuera necesario congelar versiones durante periodos prolongados, se evaluarían ramas temporales `release/*`.

El modelo de solo `main` se considera una posible evolución, no el punto de partida. Se adoptará únicamente cuando los tests, feature flags, observabilidad y mecanismos de rollback permitan garantizar que `main` permanezca siempre desplegable.
