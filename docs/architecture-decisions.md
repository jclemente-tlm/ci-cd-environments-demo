# Decisiones de arquitectura y entrega

## ADR-001 — Estrategia de dos ramas con promoción por ambientes

- **Estado:** aceptada
- **Fecha:** 2026-08-24
- **Alcance:** estrategia de branching y promoción CI/CD

### Contexto

El modelo anterior utilizaba exactamente tres ramas permanentes: `dev`, `qa` y `main`. La rama `dev` representaba integración/DEV, `qa` representaba QA y `main` representaba producción. El flujo de promoción era `dev → qa → main`.

Este diseño hacía que las ramas representaran ambientes y obligaba a realizar merges adicionales para promover código. En particular, asociar `main` directamente con PROD mezclaba el estado del repositorio con el estado desplegado: un commit puede estar en `main` y validándose en QA sin ser todavía la versión activa en producción. El objetivo es eliminar esa complejidad sin perder la separación entre integración, código estable y despliegues productivos.

También se consideró trabajar únicamente con `main` mediante trunk-based development. Ese modelo reduce todavía más las ramas, pero requiere una madurez elevada en tests automatizados, feature flags, observabilidad, rollback y cambios pequeños y frecuentes. Adoptarlo ahora trasladaría demasiado riesgo a controles que todavía deben consolidarse.

### Decisión

La estrategia se denomina **estrategia de dos ramas con promoción por ambientes**. Se clasifica como una variante simplificada de Gitflow que adopta prácticas de GitHub Flow para el trabajo cotidiano con ramas temporales y Pull Requests.

No se utilizará el término “GitHub Flow” sin calificadores, porque GitHub Flow puro integra las ramas temporales directamente en una única rama principal y no mantiene `develop`. Tampoco se utilizará “Gitflow” sin indicar que es simplificado, porque no se adoptan normalmente sus ramas `release/*` ni toda su ceremonia de releases.

Se mantendrán dos ramas permanentes:

- `develop`: integración de funcionalidades y correcciones; despliega automáticamente a DEV.
- `main`: código estable, funcional y elegible para promoción; despliega automáticamente a QA y es el único origen permitido para PROD.

Los ambientes no se representarán mediante ramas. Se utilizarán tres GitHub Environments de deployment: `dev`, `qa` y `prod`. Un cuarto Environment, `qa-signoff`, funciona únicamente como puerta de gobernanza para el sign-off funcional y no representa infraestructura adicional.

```text
feature/*, fix/*
        ↓ PR + CI
develop ──> DEV
        ↓ PR de promoción + CI
main ──> QA ──> aprobación ──> PROD
```

`main` no representa el estado actualmente desplegado en producción. PROD corresponde a un deployment concreto identificado por versión, commit SHA, artefacto, actor y fecha. Por ello, `main` puede contener una versión en QA mientras PROD continúa ejecutando una versión anterior.

### Alineación con patrones de la industria

| Patrón | Elementos adoptados | Elementos no adoptados |
|---|---|---|
| GitHub Flow | Ramas temporales cortas, Pull Requests, revisión, CI y eliminación tras el merge | Integración directa de toda rama temporal hacia una única `main` |
| Gitflow | `develop` como integración, `main` como línea estable, `feature/*` y `hotfix/*` | Uso habitual de `release/*` y una ceremonia completa de releases |
| Trunk-Based Development | Objetivo futuro de cambios pequeños, automatización y una rama siempre desplegable | Integración actual directa y continua de todos los cambios en un único trunk |

La adaptación es deliberada: las ramas modelan el estado del código y GitHub Environments modela los destinos y controles de deployment. No se afirma conformidad estricta con un framework externo.

Referencias de los patrones comparados:

- [GitHub Flow — GitHub Docs](https://docs.github.com/en/get-started/using-github/github-flow)
- [Gitflow Workflow — Atlassian](https://www.atlassian.com/git/tutorials/comparing-workflows/gitflow-workflow)
- [Trunk Based Development](https://trunkbaseddevelopment.com/)

### Reglas derivadas

1. No se permite push directo a `develop` ni a `main`.
2. `feature/*` y `fix/*` se integran mediante PR hacia `develop`.
3. La promoción ordinaria ocurre mediante PR `develop → main`.
4. `hotfix/*` nace de `main`, regresa mediante PR a `main` y, después del deployment, se sincroniza obligatoriamente con `develop`.
5. Todo PR requiere CI exitoso y las aprobaciones definidas en los rulesets.
6. Un merge a `develop` despliega automáticamente a DEV.
7. Un merge a `main` despliega automáticamente a QA.
8. Un sign-off QA exitoso inicia la promoción a PROD sin copiar identificadores manualmente; el deployment requiere aprobación del GitHub Environment `prod` y un artefacto generado por CI desde `main`.
9. QA y PROD deben recibir exactamente el mismo artefacto; CD no recompila.
10. La trazabilidad mínima incluye versión, SHA, run de CI, rama de origen y timestamp. En una evolución productiva también incluirá el digest.
11. Una aprobación manual no puede sustituir una validación QA fallida: PROD exige smoke evidence y sign-off funcional de QA para el mismo SHA.
12. Un defecto encontrado en QA se corrige desde `main` mediante `fix/qa-*` y se sincroniza después hacia `develop`; `hotfix/*` se reserva para defectos presentes en PROD.
13. Un check obligatorio valida las rutas de PR y la ascendencia de `fix/qa-*` y `hotfix/*`.

### Consecuencias positivas

- Se elimina la rama permanente `qa` y los merges usados únicamente para representar promociones.
- Se separa integración de código estable sin introducir una rama por ambiente.
- Los hotfixes solo necesitan sincronizarse entre dos ramas permanentes.
- GitHub Environments asume la protección, configuración e historia de deployments.
- Se mantiene una barrera clara antes de incorporar cambios a `main`.
- La estrategia permite evolucionar gradualmente sin adoptar trunk-based antes de contar con los controles necesarios.

### Costes y riesgos aceptados

- El merge de `develop` a `main` puede crear un SHA diferente; DEV puede usar un artefacto distinto al candidato generado desde `main`.
- La garantía obligatoria es que QA y PROD compartan el artefacto de `main`, no que DEV, QA y PROD compartan un único binario.
- `develop` puede experimentar inestabilidad temporal; PR, CI y DEV deben detectarla antes de promover a `main`.
- Mantener dos ramas implica sincronización explícita de hotfixes.

### Alternativas descartadas

#### Modelo anterior: `dev`, `qa` y `main` (`main` = PROD)

Descartado porque confunde ramas con ambientes, agrega los merges `dev → qa → main`, aumenta la divergencia y complica hotfixes. La rama `qa` no aporta un control que el GitHub Environment `qa` no pueda ofrecer mejor, y `main` debe representar código estable y promovible, no actuar como marcador del deployment actual de PROD.

#### GitHub Flow o trunk-based con solo `main`

No se descarta como evolución futura, pero no se adopta inicialmente. Requiere que `main` permanezca desplegable mediante automatización, feature flags, observabilidad, rollback y disciplina de integración maduros.

#### Rama permanente `release` o `qa`

No se justifica para el flujo actual. Si en el futuro existen periodos largos de estabilización, múltiples versiones soportadas o ventanas rígidas, podrán utilizarse ramas temporales `release/*`.

### Criterios para revisar la decisión

La organización podrá evaluar un modelo de solo `main` cuando se cumplan de forma medible:

- pruebas unitarias, de integración y end-to-end confiables;
- feature flags con ciclo de vida y responsables definidos;
- despliegues pequeños y frecuentes;
- rollback o roll-forward automatizado y probado;
- observabilidad con alertas y métricas de salud;
- baja tasa de fallos de cambio y tiempo de recuperación controlado;
- capacidad de mantener `main` siempre desplegable.

También se revisará la decisión si aparecen necesidades reales de mantener releases paralelos o largos periodos de estabilización.

### Justificación operativa de `develop`

La organización necesita integrar varias funcionalidades en DEV antes de formar un candidato para QA. Además, los desarrolladores deben continuar preparando el siguiente release mientras QA ejecuta pruebas mayormente manuales sobre el candidato de `main`.

Por ello, `develop` no se conserva solamente por convención de Gitflow: representa una línea activa de integración y trabajo futuro, mientras `main` representa la línea estable bajo promoción. Los controles de CI se ejecutan tanto en los PR como después de cada merge; `develop` también produce un artefacto desplegable, pero este queda restringido a DEV y no es elegible para PROD.

## ADR-002 — CI produce; CD promueve

- **Estado:** aceptada
- **Fecha:** 2026-08-24

### Decisión

CI restaura dependencias, compila, prueba, empaqueta mediante `dotnet publish` y almacena un artefacto inmutable. CD selecciona, descarga, configura, despliega y verifica ese artefacto sin recompilarlo.

La palabra *publish* de .NET significa preparar los archivos desplegables; no significa desplegar a un ambiente. Para evitar ambigüedad, los pasos se denominan **Package deployable application** y **Upload immutable build artifact**.

La separación se expresa mediante jobs dentro de un solo **CI/CD Pipeline**: `Build and test` produce el artefacto; `Deploy DEV` o `Deploy QA` lo promueven sin recompilar; `Approve QA` y `Deploy PROD` aplican puertas independientes mediante GitHub Environments. Los deployments reutilizan una composite action, evitando workflows visibles adicionales y conservando automáticamente run, SHA, versión y rama.

### Consecuencias

- Los deployments pueden reintentarse sin recompilar.
- QA y PROD reciben el mismo binario.
- PROD solo acepta un SHA con evidencia QA exitosa y no expirada.
- Los permisos de deployment pueden evolucionar independientemente de CI.
- GitHub Artifacts funciona como registro simplificado para la PoC; una implementación productiva podrá reemplazarlo por GHCR, ECR, Artifactory o Nexus sin cambiar el modelo de promoción.
