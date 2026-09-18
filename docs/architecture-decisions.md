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
- `main`: registro protegido del código liberado; valida el código, pero no vuelve a construir ni desplegar el candidato.

Los ambientes no se representarán mediante ramas. Se utilizarán tres GitHub Environments de deployment: `dev`, `qa` y `prod`. El sign-off funcional se registra mediante una revisión requerida del PR release; no se modela como un Environment porque no es un destino de deployment.

```text
feature/*, fix/*
        ↓ PR + CI
develop ──build único──> DEV
        └──release/* ──PR abierto──> QA ──sign-off──> merge main
                                                        └──aprobación──> PROD
```

`main` no representa el estado actualmente desplegado en producción. PROD corresponde a un deployment concreto identificado por versión, commit SHA, digest, actor y fecha. Después del merge del release, `main` puede contener una versión autorizada que todavía espera la aprobación o ejecución de PROD.

### Alineación con patrones de la industria

| Patrón | Elementos adoptados | Elementos no adoptados |
|---|---|---|
| GitHub Flow | Ramas temporales cortas, Pull Requests, revisión, CI y eliminación tras el merge | Integración directa de toda rama temporal hacia una única `main` |
| Gitflow | `develop` como integración, `main` como línea estable, `feature/*`, `release/*` y `hotfix/*` | Ceremonia completa y todas sus convenciones de release |
| Trunk-Based Development | Objetivo futuro de cambios pequeños, automatización y una rama siempre desplegable | Integración actual directa y continua de todos los cambios en un único trunk |

La adaptación es deliberada: las ramas modelan el estado del código y GitHub Environments modela los destinos y controles de deployment. No se afirma conformidad estricta con un framework externo.

Referencias de los patrones comparados:

- [GitHub Flow — GitHub Docs](https://docs.github.com/en/get-started/using-git/about-git#how-github-works)
- [Gitflow Workflow — Atlassian](https://www.atlassian.com/git/tutorials/comparing-workflows/gitflow-workflow)
- [Deploying code — GitHub Docs](https://docs.github.com/en/pull-requests/concepts/deploying-code)
- [GitHub Deployments API](https://docs.github.com/en/rest/deployments/deployments)
- [Protected branches and required deployments — GitHub Docs](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches/about-protected-branches)
- [GitHub Actions limits](https://docs.github.com/en/actions/reference/limits)
- [DORA DevOps capabilities — Google Cloud](https://docs.cloud.google.com/architecture/devops)

### Reglas derivadas

1. No se permite push directo a `develop` ni a `main`.
2. `feature/*` y `fix/*` se integran mediante PR hacia `develop`.
3. Después de seleccionar un SHA validado en DEV se crea `release/<versión>` y se abre PR hacia `main`; el PR permanece abierto durante QA.
4. `hotfix/*` nace de `main`, regresa mediante PR a `main` y, después del deployment, se sincroniza obligatoriamente con `develop`.
5. Todo PR requiere CI exitoso y las aprobaciones definidas en los rulesets.
6. Un merge a `develop` despliega automáticamente a DEV.
7. Un merge a `main` valida y registra código liberado, pero no genera otro artefacto.
8. Un aprobador del Environment `qa` autoriza instalar en QA el candidato fijado por el PR `release/* → main`.
9. El PR permanece abierto durante las pruebas funcionales; `QA sign-off` es una revisión requerida asociada a su HEAD, mientras deployment y smoke tests aportan checks para el mismo digest.
10. Un sign-off exitoso habilita el merge del candidato en `main`; PROD solo puede iniciarse después de ese merge.
11. DEV, QA y PROD deben recibir exactamente el mismo artefacto; CD no recompila.
12. La trazabilidad mínima incluye versión, SHA, digest, run de CI, rama de origen y timestamp.
13. Una aprobación manual no puede sustituir una validación QA fallida: PROD exige smoke evidence y sign-off funcional de QA para el mismo SHA y digest.
14. `release/*` es inmutable. Un cambio de código durante QA rechaza el release; la corrección produce desde `develop` un SHA y digest nuevos e invalida toda evidencia anterior.
15. Una rama temporal `release/*` fija el candidato, no representa un ambiente, no genera otra imagen por promoción y se elimina después del merge.
16. Un check obligatorio valida las rutas de PR, exige que releases y hotfixes contengan `main`, y restringe merges adicionales en hotfixes.

### Consecuencias positivas

- Se elimina la rama permanente `qa` y los merges usados únicamente para representar promociones.
- Se separa integración de código estable sin introducir una rama por ambiente.
- Los hotfixes solo necesitan sincronizarse entre dos ramas permanentes.
- GitHub Environments asume la protección, configuración e historia de deployments.
- Se mantiene una barrera clara antes de incorporar cambios a `main`.
- La estrategia permite evolucionar gradualmente sin adoptar trunk-based antes de contar con los controles necesarios.

### Costes y riesgos aceptados

- El merge o registro en `main` puede crear un SHA diferente; por ello la implementación empresarial debe verificar la equivalencia del árbol liberado y el `source_sha` del digest promovido.
- Resolver candidatos entre workflows exige conservar el artefacto y sus metadatos durante toda la validación; un candidato expirado no puede promoverse.
- `develop` puede experimentar inestabilidad temporal; PR, CI y DEV deben detectarla antes de promover a `main`.
- Mantener dos ramas implica sincronización explícita de hotfixes.

### Alternativas descartadas

#### Modelo anterior: `dev`, `qa` y `main` (`main` = PROD)

Descartado porque confunde ramas con ambientes, agrega los merges `dev → qa → main`, aumenta la divergencia y complica hotfixes. La rama `qa` no aporta un control que el GitHub Environment `qa` no pueda ofrecer mejor, y `main` debe representar código estable y promovible, no actuar como marcador del deployment actual de PROD.

#### GitHub Flow o trunk-based con solo `main`

No se descarta como evolución futura, pero no se adopta inicialmente. Requiere que `main` permanezca desplegable mediante automatización, feature flags, observabilidad, rollback y disciplina de integración maduros.

#### Rama permanente `release` o `qa`

No se justifica para el flujo actual. Se utilizan ramas temporales `release/*`, pero ninguna rama permanente representa QA o una fase de liberación. Si en el futuro existen múltiples versiones soportadas simultáneamente, deberá evaluarse una estrategia adicional de mantenimiento.

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

La organización necesita integrar varias funcionalidades en DEV antes de formar un candidato para QA. Además, los desarrolladores deben continuar preparando el siguiente release mientras QA ejecuta pruebas mayormente manuales sobre un candidato fijado por SHA y digest.

Por ello, `develop` no se conserva solamente por convención de Gitflow: representa una línea activa de integración. Un commit integrado produce un candidato que se valida progresivamente en DEV, QA y PROD. `main` registra el código aprobado y no origina una reconstrucción.

## ADR-002 — CI produce; CD promueve

- **Estado:** aceptada
- **Fecha:** 2026-08-24

### Decisión

CI restaura dependencias, compila, prueba, empaqueta mediante `dotnet publish` y almacena un artefacto inmutable. CD selecciona, descarga, configura, despliega y verifica ese artefacto sin recompilarlo.

La palabra *publish* de .NET significa preparar los archivos desplegables; no significa desplegar a un ambiente. Para evitar ambigüedad, los pasos se denominan **Package deployable application** y **Upload immutable build artifact**.

La separación se expresa mediante tres workflows: **CI and DEV** produce el candidato; **Release QA** se ejecuta para el PR release y termina después de deployment y smoke tests; **Release PROD** se ejecuta después del merge. La espera funcional vive en el PR y se registra como revisión requerida. Los deployments reutilizan acciones compuestas y conservan automáticamente run, SHA, versión, rama y digest.

### Consecuencias

- Los deployments pueden reintentarse sin recompilar.
- DEV, QA y PROD reciben el mismo binario.
- PROD solo acepta un SHA con evidencia QA exitosa y no expirada.
- Los permisos de deployment pueden evolucionar independientemente de CI.
- GitHub Artifacts funciona como registro simplificado para la PoC; una implementación productiva podrá reemplazarlo por GHCR, ECR, Artifactory o Nexus sin cambiar el modelo de promoción.
